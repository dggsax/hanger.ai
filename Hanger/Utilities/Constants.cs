using System.Windows.Media;

namespace Hanger
{
    public static class Constants
    {
        #region Constants

        /// <summary>
        /// DPI for Kinect Video Stream
        /// </summary>
        public static readonly double DPI = 96.0;

        /// <summary>
        /// Default format for pixels
        /// </summary>
        public static readonly PixelFormat FORMAT = PixelFormats.Bgr32;

        /// <summary>
        /// Bytes per pixel
        /// </summary>
        public static readonly int BYTES_PER_PIXEL = (FORMAT.BitsPerPixel + 7) / 8;

        #endregion
    }
}
