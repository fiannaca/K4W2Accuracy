using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GistModulesLib
{
    public static class ImageHelpers
    {
        /// <summary>
        /// Converts a BitmapSource (e.g. WriteableBitmap) to a System.Drawing.Bitmap. This function 
        /// requires a memory stream to be created and left open! Only use this function with care!
        /// </summary>
        /// <param name="bitmapsource">The BitmapSource object</param>
        /// <returns>A System.Drawing.Bitmap image</returns>
        public static System.Drawing.Bitmap ToBitmap(this BitmapSource bitmapsource, out MemoryStream stream)
        {
            stream = new MemoryStream();

            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapsource));
            enc.Save(stream);

            return new System.Drawing.Bitmap(stream);
        }

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <p name="image">The Emgu CV Image</p>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                NativeMethods.DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <p name="image">The Emgu CV Image</p>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap image)
        {
            IntPtr ptr = image.GetHbitmap(); //obtain the Hbitmap

            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            NativeMethods.DeleteObject(ptr); //release the HBitmap
            return bs;
        }

        /// <summary>
        /// Used for Disposing of HBitmap objects created in the ToBitmapSource methods
        /// </summary>
        private static class NativeMethods
        {
            [DllImport("gdi32")]
            public static extern int DeleteObject(IntPtr o);
        }
    }
}
