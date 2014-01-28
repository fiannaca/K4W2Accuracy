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

namespace K4W2Accuracy.ViewModel
{
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
                                          }));
            }
        }

        private RelayCommand<MouseButtonEventArgs> _depthClickCommand;

        /// <summary>
        /// Gets the DepthClickCommand.
        /// </summary>
        public RelayCommand<MouseButtonEventArgs> DepthClickCommand
        {
            get
            {
                return _depthClickCommand
                    ?? (_depthClickCommand = new RelayCommand<MouseButtonEventArgs>(
                                          (e) =>
                                          {
                                              var pt = e.GetPosition(null);
                                              clicked = new Rect(pt.X - 15, pt.Y - 15, 10, 10);
                                              GetDepth = true;
                                          }));
            }
        }

        public void ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            WriteableBitmap img;

            bool success = KinectDisplayHelper.ProcessColorFrame(out img, e.FrameReference);

            if (success)
            {
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

        private Rect clicked = Rect.Empty;

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

                if (clicked != Rect.Empty)
                {
                    if (GetDepth)
                    {
                        List<int> depths = new List<int>();
                        for (int i = (int)clicked.Y; i < (int)clicked.Y + 40; ++i)
                        {
                            for (int j = (int)clicked.X; j < (int)clicked.X + 40; j++)
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
                            Distance = Math.Round(depths.Average(), 2).ToString() + " mm";
                            GetDepth = false;
                        }
                        else
                        {
                            Distance = "0.0 mm";
                            GetDepth = false;
                        }
                    }

                    img.DrawRectangle((int)clicked.X, (int)clicked.Y, (int)clicked.BottomRight.X, (int)clicked.BottomRight.Y, Color.FromRgb(0, 0, 255));
                }

                img.DrawRectangle(512 / 2 - 10, 424 / 2 - 10, 512 / 2 + 10, 424 / 2 + 10, Color.FromRgb(0, 255, 0));
                    
                OutputImage = img;
            }
        }

        public FrameDescription DepthFrameDesc { get; set; }

        /// <summary>
        /// The <see cref="Distance" /> property's name.
        /// </summary>
        public const string DistancePropertyName = "Distance";

        private string _distanceString = "0.0 mm";

        /// <summary>
        /// Sets and gets the Distance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Distance
        {
            get
            {
                return _distanceString;
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
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            
        }
    }
}