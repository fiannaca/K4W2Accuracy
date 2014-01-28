using GistModulesLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Win32;

namespace GistModulesLib
{
    [InheritedExport(ViewModelTypes.VisualModule, typeof(VisualModule))]
    public abstract class VisualModule : ModuleBase
    {
        /// <summary>
        /// Constructor for the NonVisualModule class
        /// </summary>
        /// <p name="title">The title of the module</p>
        public VisualModule(string title = "Default Title", bool isActive = true) : base(isActive)
        {
            Title = title;
        }

        /// <summary>
        /// The <see cref="OutputImage" /> property's name.
        /// </summary>
        public const string OutputImagePropertyName = "OutputImage";

        private ImageSource _outputImage = new WriteableBitmap(320, 240, 96, 96, PixelFormats.Bgr32, null);

        /// <summary>
        /// Sets and gets the OutputImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ImageSource OutputImage
        {
            get
            {
                return _outputImage;
            }

            set
            {
                if (_outputImage == value)
                {
                    return;
                }

                RaisePropertyChanging(OutputImagePropertyName);
                _outputImage = value;
                RaisePropertyChanged(OutputImagePropertyName);
            }
        }

        private RelayCommand _saveImageCommand;

        /// <summary>
        /// Gets the SaveImage command
        /// </summary>
        public RelayCommand SaveImage
        {
            get
            {
                return _saveImageCommand ?? (_saveImageCommand = new RelayCommand( () => SaveOutputImageToFile()));
            }
        }

        /// <summary>
        /// Saves the OutputImage to a file. This allows for taking screen captures from the output of 
        /// each individual module.
        /// </summary>
        protected void SaveOutputImageToFile()
        {
            ImageSource temp = OutputImage.Clone();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = Title.ToLower().Replace(" ", String.Empty) + "_capture";
            sfd.AddExtension = true;
            sfd.DefaultExt = "jpg";
            sfd.Filter = "JPEG Image|*.jpg";

            if (sfd.ShowDialog() == true)
            {
                _saveOutput(temp, sfd.FileName);
            }
        }

        /// <summary>
        /// Performs the save operation. This can be overloaded if the default method of saving doesn't work for 
        /// some reason with an given type which is cast to an ImageSource.
        /// </summary>
        /// <p name="output">ImageSource to save</p>
        /// <p name="filename">Filename to save it under</p>
        protected virtual void _saveOutput(ImageSource output, string filename)
        {
            DrawingVisual vis = new DrawingVisual();
            DrawingContext cont = vis.RenderOpen();
            cont.DrawImage(output, new System.Windows.Rect(0, 0, 320, 240));
            cont.Close();

            RenderTargetBitmap renderer = new RenderTargetBitmap(320, 240, 96, 96, PixelFormats.Default);
            renderer.Render(vis);

            Stream imgStream = new FileStream(filename, FileMode.Create);

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(renderer));
            encoder.Save(imgStream);
        }
    }
}
