using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hanger.Properties;
using Image = System.Windows.Controls.Image;

namespace Hanger
{
    /// <summary>
    /// Class to keep track of <see cref="Shirt"/> instances for the Hanger Interface
    /// </summary>
    class ShirtManager
    {
        /// <summary>
        /// WPF Canvas for displaying the shirt on the skeleton
        /// </summary>
        private readonly Canvas ShirtCanvas;

        /// <summary>
        /// WPF Image used to display the previous shirt on the left side of the UI
        /// </summary>
        private readonly Image LeftPreviewImage;

        /// <summary>
        /// WPF Image used to display the next shirt on the right side of the UI
        /// </summary>
        private readonly Image RightPreviewImage;

        /// <summary>
        /// Thread dispatcher (owned by WPF window) that we use to display the previewed shirts
        /// </summary>
        private readonly Dispatcher Dispatcher;

        /// <summary>
        /// Index for the currently selected shirt
        /// </summary>
        private int currentShirtIndex;

        /// <summary>
        /// List of Shirt instances loaded from Project Resources listing
        /// </summary>
        private List<Shirt> shirts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShirtManager"/> class.
        /// </summary>
        /// <param name="shirtCanvas">Canvas that we are asking shirts to draw themselves to</param>
        /// <param name="leftPreview">Canvas for left shirt</param>
        /// <param name="rightPreview">Canvas for right shirt</param>
        /// <param name="dispatcher">Instance of dispatcher owned by <see cref="MainWindow"/></param>
        public ShirtManager(Canvas shirtCanvas, Image leftPreview, Image rightPreview, Dispatcher dispatcher)
        {
            this.ShirtCanvas = shirtCanvas;
            this.LeftPreviewImage = leftPreview;
            this.RightPreviewImage = rightPreview;
            this.Dispatcher = dispatcher;
            this.currentShirtIndex = 0;

            // Once the above fields are set, load up the images from project resources
            this.shirts = this.InitializeShirts();
        }

        /// <summary>
        /// Switches to the next shirt and then updates preview images by calling <see cref="DisplayPreviewImages"/>
        /// </summary>
        /// <returns>Returns the new shirt that is selected</returns>
        public Shirt NextShirt() 
        {
            // make sure to rotate around size of shirts list
            this.currentShirtIndex = (this.currentShirtIndex + 1) % this.shirts.Count();

            this.DisplayPreviewImages();

            Shirt nextShirt = this.shirts[this.currentShirtIndex];
            
            return nextShirt;
        }

        /// <summary>
        /// Switch to the previous shirt
        /// </summary>
        /// <returns>Instance of newly selected shirt</returns>
        public Shirt PreviousShirt()
        {
            if (this.currentShirtIndex == 0)
            {
                this.currentShirtIndex = this.shirts.Count() - 1;
            }
            else
            {
                this.currentShirtIndex = this.currentShirtIndex - 1;
            }

            this.DisplayPreviewImages();

            Shirt previousShirt = this.shirts[this.currentShirtIndex];

            return previousShirt;
        }

        /// <summary>
        /// Given a bitmap, process the data into a render-able <see cref="BitmapSource"/>
        /// </summary>
        /// <param name="image">bitmap for image we want to make render-able</param>
        /// <returns>render-able image</returns>
        private static BitmapSource LoadImageBitmapSource(Bitmap image)
        {
            // Take the bitmap data and acquire a lock to it (no corrupt bits allowed here no sir!)
            BitmapData imageData =
                image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadWrite,
                    image.PixelFormat);

            // This is here to support the case where not all shirt images follow the same
            // pixel format, which leads to bitmapsource creation being a pain
            System.Windows.Media.PixelFormat pixelFormat = PixelFormats.Bgra32;
            if (image.PixelFormat.Equals(System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                pixelFormat = PixelFormats.Bgr24;
            }

            // Copy the bitmap source to a render-able instance
            BitmapSource imageBitmap =
                BitmapSource.Create(
                    image.Width,
                    image.Height,
                    image.HorizontalResolution,
                    image.VerticalResolution,
                    pixelFormat,
                    null,
                    imageData.Scan0,
                    image.Width * image.Height * 4,
                    imageData.Stride);

            return imageBitmap;
        }

        /// <summary>
        /// Based on the currentShirtIndex, will display the shirts before and after into their respective frames on the main window
        /// </summary>
        private void DisplayPreviewImages()
        {
            int previous = (this.currentShirtIndex - 1 < 0) ? this.shirts.Count() - 1 : this.currentShirtIndex - 1;
            int next = (this.currentShirtIndex + 1) % this.shirts.Count();

            this.Dispatcher.Invoke(() =>
            {
                this.LeftPreviewImage.Source = this.shirts[previous].ShirtBitmapSource;
                this.RightPreviewImage.Source = this.shirts[next].ShirtBitmapSource;
            });
        }

        /// <summary>
        /// Loads shirts that are stored in the project resources
        /// </summary>
        /// <returns>An array of <see cref="Shirt"/> instances for the <see cref="ShirtManager"/> to keep track of</returns>
        private List<Shirt> InitializeShirts()
        {
            List<Shirt> loadedShirts = new List<Shirt>();

            ResourceSet resources = Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            
            // TODO make check if the entry is of type Bitmap, cause projects could have resources that are not images
            foreach (DictionaryEntry entry in resources)
            {
                // Convert entry object to Bitmap
                Bitmap resource = (Bitmap)entry.Value;

                // Convert bitmap to BitmapSource file that we can display on the image
                BitmapSource source = LoadImageBitmapSource(resource);

                // Create instance of the shirt object, making sure to pass in Canvas
                Shirt shirt = new Shirt(this.ShirtCanvas, source);

                loadedShirts.Add(shirt);
            }

            return loadedShirts;
        }
    }
}
