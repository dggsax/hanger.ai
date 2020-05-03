using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Hanger
{
    /// <summary>
    /// Provides functionality for easily generating a bitmap for a color frame!
    /// </summary>
    public static class ColorExtensions
    {
        #region Members

        /// <summary>
        /// The bitmap source, where we write the bitmap too
        /// </summary>
        private static WriteableBitmap bitmap = null;

        /// <summary>
        /// Width of frames
        /// </summary>
        private static int frameWidth;

        /// <summary>
        /// Height of frames
        /// </summary>
        private static int frameHeight;

        /// <summary>
        /// Pixel values in RGB format
        /// </summary>
        private static byte[] rgbPixelValues = null;

        #endregion

        #region Public Methods

        public static BitmapSource ToBitmap(this ColorImageFrame frame)
        {
            if (bitmap == null)
            {
                frameWidth = frame.Width;
                frameHeight = frame.Height;
                rgbPixelValues = new byte[frameWidth * frameHeight * Constants.BYTES_PER_PIXEL];
                bitmap = new WriteableBitmap(frameWidth, frameHeight, Constants.DPI, Constants.DPI, Constants.FORMAT, null);
            }

            // copy the pixel data from the frame to the storage array;
            frame.CopyPixelDataTo(rgbPixelValues);

            // take the lock on the bitmpa, don't want it to change!
            bitmap.Lock();

            // I don't know what this does lmao but it's needed
            Marshal.Copy(rgbPixelValues, 0, bitmap.BackBuffer, rgbPixelValues.Length);

            // mark affected area for bitmap
            Int32Rect affectedRect = new Int32Rect(0, 0, frameWidth, frameHeight);
            bitmap.AddDirtyRect(affectedRect);

            bitmap.Unlock();

            return bitmap;


        }
        #endregion
    }
}
