using Hanger.Properties;
using Hanger.Utilities;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Hanger
{
    class Shirt
    {
        /// <summary>
        /// Image for shirt that is rendered in <see cref="MainWindow"/>
        /// </summary>
        public Canvas shirtCanvas;

        /// <summary>
        /// Visual Host for storing and displaying <see cref="shirtDrawing"/> onto the <see cref="shirtCanvas"/>
        /// </summary>
        private VisualHost visualHost;

        /// <summary>
        /// Generated Bitmap Source of image
        /// </summary>
        private BitmapSource shirtBitmapSource;

        /// <summary>
        /// Visual where we draw image at a specific location
        /// </summary>
        private DrawingVisual shirtDrawing;

        private Point previousPoint = new Point(0, 0);

        /// <summary>
        /// Constructor for Shirt instance that takes in a reference to <see cref="MainWindow.ChosenShirt"/> object
        /// </summary>
        /// <param name="shirtCanvas"></param>
        public Shirt(Canvas shirtCanvas)
        {
            // Set up drawing capabilities
            this.Init();

            // Load "MIT_Shirt.png"
            this.LoadShirt();

            // save reference to shirt image
            this.shirtCanvas = shirtCanvas;

            // Draw image at (0, 0)
            Point initialDestination = new Point(0, 0);

            this.DrawImage(initialDestination);
        }

        public void DrawImage(Point destination, double width, double height)
        {
            double difference = Point.Subtract(this.previousPoint, destination).Length;

            if (difference < 1.5)
            {
                this.previousPoint = destination;

                return;
            }

            shirtCanvas.Children.Clear();

            double x = destination.X - width / 2;
            double y = destination.Y - 10;

            using (DrawingContext dc = this.shirtDrawing.RenderOpen())
            {
                dc.DrawImage(this.shirtBitmapSource,
                             new Rect(x, y, width, height));
            }

            shirtCanvas.Children.Add(this.visualHost);

            this.previousPoint = destination;
        }

        public void DrawImage(Point destination)
        {
            double width = this.shirtBitmapSource.Width;
            double height = this.shirtBitmapSource.Height;

            this.DrawImage(destination, width, height);
        }

        /// <summary>
        /// Initialize drawing instances that we will use to append to the canvas
        /// </summary>
        public void Init()
        {
            this.shirtDrawing = new DrawingVisual();

            this.visualHost = new VisualHost { visual = shirtDrawing };
        }

        private void LoadShirt()
        {
            Bitmap image = Resources.MIT_Shirt;

            this.LoadImageBitmapSource(image);
        }

        private void LoadImageBitmapSource(Bitmap image)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            BitmapSource imageBitmap = BitmapSource.Create(image.Width, image.Height, image.HorizontalResolution, image.VerticalResolution, PixelFormats.Bgra32, null, imageData.Scan0, image.Width * image.Height * 4, imageData.Stride);

            this.shirtBitmapSource = imageBitmap;
        }
    }
}
