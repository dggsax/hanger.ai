using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Hanger.Properties;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Resources;
using System.Globalization;
using System.Collections;

namespace Hanger
{
    class ShirtManager
    {
        public Canvas shirtCanvas;

        private int currentShirt;

        private List<Shirt> shirts;

        /// <summary>
        /// Create an instance of Shirt Manager
        /// </summary>
        /// <param name="shirtCanvas">Canvas that we are asking shirts to draw themselves to</param>
        public ShirtManager(Canvas shirtCanvas)
        {
            this.shirtCanvas = shirtCanvas;
            this.currentShirt = 0;
            this.shirts = initializeShirts();
        }

        /// <summary>
        /// Switch to the next shirt
        /// </summary>
        /// <returns></returns>
        public Shirt nextShirt() 
        {
            this.currentShirt = (this.currentShirt + 1) % shirts.Count();
            return shirts[currentShirt];
        }

        /// <summary>
        /// Switch to the previous shirt
        /// </summary>
        /// <returns>Instance of newly selected shirt</returns>
        public Shirt previousShirt()
        {
            if (this.currentShirt == 0)
            {
                this.currentShirt = this.shirts.Count() - 1;
            }
            else
            {
                this.currentShirt = (this.currentShirt + 1) % shirts.Count();
            }

            return shirts[currentShirt];
        }

        /// <summary>
        /// Loads shirts that are stored in the project resources
        /// </summary>
        /// <returns>An array of <see cref="Shirt"/> instances for the <see cref="ShirtManager"/> to keep track of</returns>
        private List<Shirt> initializeShirts()
        {
            List<Shirt> shirt_resources = new List<Shirt>();
            ResourceSet resources = Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            
            foreach(DictionaryEntry entry in resources)
            {
                // Convert entry object to Bitmap
                Bitmap resource = (Bitmap) entry.Value;

                // Convert bitmap to BitmapSource file that we can display on the image
                BitmapSource source = this.LoadImageBitmapSource(resource);

                // Create instance of the shirt object, making sure to pass in Canvas
                Shirt shirt = new Shirt(this.shirtCanvas, source);
                shirt_resources.Add(shirt);
            }

            return shirt_resources;
        }

        private BitmapSource LoadImageBitmapSource(Bitmap image)
        {
            BitmapData imageData = 
                image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height), 
                    ImageLockMode.ReadWrite, 
                    image.PixelFormat);

            BitmapSource imageBitmap = 
                BitmapSource.Create(
                    image.Width, 
                    image.Height, 
                    image.HorizontalResolution, 
                    image.VerticalResolution, 
                    PixelFormats.Bgra32, null, 
                    imageData.Scan0, 
                    image.Width * image.Height * 4, 
                    imageData.Stride);

            return imageBitmap;
        }
    }
}
