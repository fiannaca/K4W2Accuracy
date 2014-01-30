using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GistModulesLib;
using K4W2Accuracy.Infrastructure;
using K4W2Accuracy.Model;
using Microsoft.Kinect;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rectangle = System.Drawing.Rectangle;
using System.IO;

namespace K4W2Accuracy.ViewModel
{
    public struct Observation
    {
        public double Observed;
        public double Actual;
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    [Export(ViewModelTypes.MainViewModel, typeof(MainViewModel))]
    public class MainViewModel : VisualModule, IKinectDepthSubscriber, IKinectColorSubscriber
    {
        private RelayCommand _startSystemCommand;

        /// <summary>
        /// Gets the StartSystem command
        /// </summary>
        public RelayCommand StartSystem
        {
            get
            {
                return _startSystemCommand
                    ?? (_startSystemCommand = new RelayCommand(
                                          () =>
                                          {
                                              //This is the only call which needs to be made, because
                                              // any other module which needs to initialize will register
                                              // and respond to the KinectStatusMessage sent by StartHelper()
                                              Helper.StartHelper();
                                          }));
            }
        }

        private RelayCommand _stopSystemCommand;

        /// <summary>
        /// Gets the StopSystem command
        /// </summary>
        public RelayCommand StopSystem
        {
            get
            {
                return _stopSystemCommand
                    ?? (_stopSystemCommand = new RelayCommand(
                                          () =>
                                          {
                                              //This is the only call which needs to be made, because
                                              // any other module which needs to shutdown will register
                                              // and respond to the KinectStatusMessage sent by ShutdownHelper()
                                              Helper.ShutdownHelper();

                                              if(Observations.Count > 0)
                                              {
                                                  using(FileStream file = new FileStream("Observations.csv", FileMode.OpenOrCreate))
                                                  {
                                                      StreamWriter writer = new StreamWriter(file);

                                                      foreach(var ob in Observations)
                                                      {
                                                          writer.WriteLine(ob.Observed + "," + ob.Actual + ",");
                                                      }

                                                      writer.Close();
                                                  }
                                              }
                                          }));
            }
        }

        private RelayCommand<MousePoint> _depthClickCommand;

        /// <summary>
        /// Gets the DepthClickCommand.
        /// </summary>
        public RelayCommand<MousePoint> DepthClickCommand
        {
            get
            {
                return _depthClickCommand
                    ?? (_depthClickCommand = new RelayCommand<MousePoint>(
                                          (pt) =>
                                          {
                                              DepthSelection = new Rectangle(pt.X - 5, pt.Y - 5, 10, 10);
                                              GetDepth = true;

                                              //Temp solution to color2depth mapping problem
                                              DepthSpacePoint dpt = new DepthSpacePoint { X = pt.X, Y = pt.Y };
                                              var cpt = Helper.PointMapper.MapDepthPointToColorSpace(
                                                                    dpt, 
                                                                    DepthFrameData[pt.Y * DepthFrameDesc.Width + pt.X]
                                                                );

                                              ColorSelection = new Rectangle((int)cpt.X - 15, (int)cpt.Y - 15, 30, 30);
                                          }));
            }
        }

        private RelayCommand<MousePoint> _colorClickCommand;

        /// <summary>
        /// Gets the ColorClickCommand.
        /// </summary>
        public RelayCommand<MousePoint> ColorClickCommand
        {
            get
            {
                return _colorClickCommand
                    ?? (_colorClickCommand = new RelayCommand<MousePoint>(
                                          (p) =>
                                          {
                                              var pt = new MousePoint { X = (int)(p.X * 3.75), Y = (int)(p.Y * 3.75) };
                                              ColorSelection = new Rectangle(pt.X - 15, pt.Y - 15, 30, 30);
                                          }));
            }
        }

        private const string EmptyTextError = "Enter the actual distance to collect an observation!";

        List<Observation> Observations = new List<Observation>();

        private RelayCommand _storeObservation;

        /// <summary>
        /// Gets the StoreObservation.
        /// </summary>
        public RelayCommand StoreObservation
        {
            get
            {
                return _storeObservation
                    ?? (_storeObservation = new RelayCommand(
                                          () =>
                                          {
                                              if (ActualDistance != "")
                                              {
                                                  Observations.Add(new Observation
                                                  {
                                                      Observed = Double.Parse(_distanceString),
                                                      Actual = Double.Parse(ActualDistance)
                                                  });

                                                  ActualDistance = "";

                                                  if(Observations.Count > 1)
                                                  {
                                                      Message(Observations.Count.ToString() + " observations collected...");
                                                  }
                                                  else
                                                  {
                                                      Message(Observations.Count.ToString() + " observation collected...");
                                                  }
                                              }
                                              else
                                              {
                                                  Message(EmptyTextError, true);
                                              }
                                            }));
            }
        }

        public void ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            WriteableBitmap img;

            if (KinectDisplayHelper.ProcessColorFrame(out img, e.FrameReference))
            {
                if(ColorSelection != Rectangle.Empty)
                {
                    img = BitmapFactory.ConvertToPbgra32Format(img);

                    var tmp = new Rectangle(ColorSelection.Location, ColorSelection.Size);
                    
                    for (int i = 0; i < 3; ++i)
                    {
                        img.DrawRectangle(
                            tmp.X, 
                            tmp.Y, 
                            tmp.Right, 
                            tmp.Bottom, 
                            Color.FromRgb(0, 255, 255)
                        );

                        tmp.Inflate(1, 1);
                    }
                }

                ColorImage = img;
            }

            return;
        }

        public FrameDescription ColorFrameDesc { get; set; }

        /// <summary>
        /// The <see cref="ColorImage" /> property's name.
        /// </summary>
        public const string ColorImagePropertyName = "ColorImage";

        private WriteableBitmap _colorImage = null;

        /// <summary>
        /// Sets and gets the ColorImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public WriteableBitmap ColorImage
        {
            get
            {
                return _colorImage;
            }

            set
            {
                if (_colorImage == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImagePropertyName);
                _colorImage = value;
                RaisePropertyChanged(ColorImagePropertyName);
            }
        }

        private Rectangle DepthSelection = Rectangle.Empty;

        private Rectangle ColorSelection = Rectangle.Empty;

        private Rectangle ColorToDepthSelection = Rectangle.Empty;

        private bool GetDepth = false;

        private ushort[] DepthFrameData { get; set; }

        public void DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            if (DepthFrameData == null)
            {
                DepthFrameDesc = Helper.Kinect.DepthFrameSource.FrameDescription;
                DepthFrameData = new ushort[DepthFrameDesc.LengthInPixels];
            }

            WriteableBitmap img;

            if (KinectDisplayHelper.ProcessDepthFrame(out img, e.FrameReference, DepthFrameData))
            {
                img = BitmapFactory.ConvertToPbgra32Format(img);

                if (DepthSelection != Rectangle.Empty)
                {
                    if (GetDepth)
                    {
                        List<int> depths = new List<int>();
                        for (int i = DepthSelection.Y; i < DepthSelection.Y + 40; ++i)
                        {
                            for (int j = DepthSelection.X; j < DepthSelection.X + 40; j++)
                            {
                                if (i > 0 && j > 0 && i < DepthFrameDesc.Height && j < DepthFrameDesc.Width)
                                {
                                    var val = DepthFrameData[i * DepthFrameDesc.Width + j];

                                    if(val > 0)
                                        depths.Add(val);
                                }
                            }
                        }

                        if (depths.Count > 0)
                        {
                            Distance = Math.Round(depths.Average(),0).ToString();
                            //GetDepth = false;
                        }
                        else
                        {
                            Distance = "0.0";
                            //GetDepth = false;
                        }
                    }

                    img.DrawRectangle(DepthSelection.X, DepthSelection.Y, DepthSelection.Right, DepthSelection.Bottom, Color.FromRgb(0, 0, 255));
                }

                //if (ColorSelection != Rectangle.Empty)
                //{
                //    if (ColorToDepthSelection != Rectangle.Empty)
                //    {
                //        img.DrawRectangle(
                //            ColorToDepthSelection.X,
                //            ColorToDepthSelection.Y,
                //            ColorToDepthSelection.Right,
                //            ColorToDepthSelection.Bottom,
                //            Color.FromRgb(0, 255, 255)
                //        );
                //    }
                //    else
                //    {
                //        DepthSpacePoint[] dpoints = new DepthSpacePoint[ColorFrameDesc.LengthInPixels];
                //        Helper.PointMapper.MapColorFrameToDepthSpace(DepthFrameData, dpoints);

                //    }
                //}

                img.DrawRectangle(512 / 2 - 10, 424 / 2 - 10, 512 / 2 + 10, 424 / 2 + 10, Color.FromRgb(0, 255, 0));
                    
                OutputImage = img;
            }
        }

        public FrameDescription DepthFrameDesc { get; set; }

        /// <summary>
        /// The <see cref="Distance" /> property's name.
        /// </summary>
        public const string DistancePropertyName = "Distance";

        private string _distanceString = "0.0";

        /// <summary>
        /// Sets and gets the Distance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Distance
        {
            get
            {
                return _distanceString + " mm";
            }

            set
            {
                if (_distanceString == value)
                {
                    return;
                }

                RaisePropertyChanging(DistancePropertyName);
                _distanceString = value;
                RaisePropertyChanged(DistancePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ActualDistance" /> property's name.
        /// </summary>
        public const string ActualDistancePropertyName = "ActualDistance";

        private string _actualDistance = "";

        /// <summary>
        /// Sets and gets the ActualDistance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ActualDistance
        {
            get
            {
                return _actualDistance;
            }

            set
            {
                if (_actualDistance == value)
                {
                    return;
                }

                RaisePropertyChanging(ActualDistancePropertyName);
                _actualDistance = value;
                RaisePropertyChanged(ActualDistancePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="StatusMessage" /> property's name.
        /// </summary>
        public const string StatusMessagePropertyName = "StatusMessage";

        private string _status = "";

        /// <summary>
        /// Sets and gets the StatusMessage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string StatusMessage
        {
            get
            {
                return _status;
            }

            set
            {
                if (_status == value)
                {
                    return;
                }

                RaisePropertyChanging(StatusMessagePropertyName);
                _status = value;
                RaisePropertyChanged(StatusMessagePropertyName);
            }
        }

        private readonly SolidColorBrush NormalBackground = new SolidColorBrush(Color.FromRgb(166, 247, 142));

        private readonly SolidColorBrush ErrorBackground = new SolidColorBrush(Color.FromRgb(255, 153, 153));

        /// <summary>
        /// The <see cref="MessageBackground" /> property's name.
        /// </summary>
        public const string MessageBackgroundPropertyName = "MessageBackground";

        private SolidColorBrush _background = null;

        /// <summary>
        /// Sets and gets the MessageBackground property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public SolidColorBrush MessageBackground
        {
            get
            {
                return _background;
            }

            set
            {
                if (_background == value)
                {
                    return;
                }

                RaisePropertyChanging(MessageBackgroundPropertyName);
                _background = value;
                RaisePropertyChanged(MessageBackgroundPropertyName);
            }
        }

        private readonly SolidColorBrush NormalBorder = new SolidColorBrush(Color.FromRgb(37, 128, 11));

        private readonly SolidColorBrush ErrorBorder = new SolidColorBrush(Color.FromRgb(178, 0, 0));

        /// <summary>
        /// The <see cref="MessageBorderColor" /> property's name.
        /// </summary>
        public const string MessageBorderColorPropertyName = "MessageBorderColor";

        private SolidColorBrush _border = null;

        /// <summary>
        /// Sets and gets the MessageBorderColor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public SolidColorBrush MessageBorderColor
        {
            get
            {
                return _border;
            }

            set
            {
                if (_border == value)
                {
                    return;
                }

                RaisePropertyChanging(MessageBorderColorPropertyName);
                _border = value;
                RaisePropertyChanged(MessageBorderColorPropertyName);
            }
        }

        private void Message(string message, bool isError = false)
        {
            if(isError)
            {
                MessageBackground = ErrorBackground;
                MessageBorderColor = ErrorBorder;
            }
            else
            {
                MessageBackground = NormalBackground;
                MessageBorderColor = NormalBorder;
            }

            if (StatusMessage == message)
                StatusMessage = "";

            StatusMessage = message;
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            _background = NormalBackground;
            _border = NormalBorder;
        }
    }
}