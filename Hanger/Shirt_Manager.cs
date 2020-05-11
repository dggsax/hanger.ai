using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Hanger.Properties;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using System.Resources;
using System.Globalization;
using System.Collections;
using System.Diagnostics;

namespace Hanger
{
    class ShirtManager
    {
        public Canvas shirtCanvas;

        private int currentShirt;

        private List<Shirt> shirts;

        public ShirtManager(Canvas shirtCanvas)
        {
            this.shirtCanvas = shirtCanvas;
            this.currentShirt = 0;
            this.shirts = initializeShirts();
        }

        public Shirt nextShirt() 
        {
            this.currentShirt = (this.currentShirt + 1) % shirts.Count();
            return shirts[currentShirt];
        }

        private List<Shirt> initializeShirts()
        {
            List<Shirt> shirt_resources = new List<Shirt>();
            ResourceSet resources = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach(DictionaryEntry entry in resources)
            {
                Bitmap resource = (Bitmap) entry.Value;
                BitmapSource source = this.LoadImageBitmapSource(resource);
                Shirt shirt = new Shirt(this.shirtCanvas, source);
                shirt_resources.Add(shirt);
            }

            //Bitmap MIT = Resources.MIT_Shirt;
            //BitmapSource MIT_bitmap = this.LoadImageBitmapSource(MIT);
            //Shirt MIT_shirt = new Shirt(this.shirtCanvas, MIT_bitmap);

            return shirt_resources;
        }

        private BitmapSource LoadImageBitmapSource(Bitmap image)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            BitmapSource imageBitmap = BitmapSource.Create(image.Width, image.Height, image.HorizontalResolution, image.VerticalResolution, PixelFormats.Bgra32, null, imageData.Scan0, image.Width * image.Height * 4, imageData.Stride);

            return imageBitmap;
        }
    }
}
