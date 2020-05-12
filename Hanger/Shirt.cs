using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Hanger.Utilities;
using Point = System.Windows.Point;

namespace Hanger
{
    /// <summary>
    /// Instance used to store bitmap data for a shirt and to render said shirt
    /// </summary>
    class Shirt
    {
        /// <summary>
        /// Image for shirt that is rendered in <see cref="MainWindow"/>
        /// </summary>
        private readonly Canvas ShirtCanvas;

        /// <summary>
        /// Visual Host for storing and displaying <see cref="shirtDrawing"/> onto the <see cref="ShirtCanvas"/>
        /// </summary>
        private VisualHost visualHost;

        /// <summary>
        /// Visual where we draw image at a specific location
        /// </summary>
        private DrawingVisual shirtDrawing;

        /// <summary>
        /// The previous destination that the shirt was rendered at
        /// </summary>
        private Point previousDestination = new Point(0, 0);

        /// <summary>
        /// Gets or sets Bitmap Source of image
        /// </summary>
        public BitmapSource ShirtBitmapSource { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shirt" /> class.
        /// </summary>
        /// <param name="shirtCanvas">reference to <see cref="Canvas"/> object to display image to</param>
        /// <param name="bitmapSource">render-able bitmap source for shirt image</param>
        public Shirt(Canvas shirtCanvas, BitmapSource bitmapSource)
        {
            // Set up drawing capabilities
            this.Init();
            this.ShirtBitmapSource = bitmapSource;
            this.ShirtCanvas = shirtCanvas;

            // Draw image at (0, 0)
            Point initialDestination = new Point(0, 0);
            this.PlaceImageToPoint(initialDestination);
        }

        /// <summary>
        /// Initialize drawing instances that we will use to append to the canvas
        /// </summary>
        public void Init()
        {
            this.shirtDrawing = new DrawingVisual();

            this.visualHost = new VisualHost { visual = this.shirtDrawing };
        }

        /// <summary>
        /// Given a destination, Place the image there with the image's default width and height
        /// </summary>
        /// <param name="destination">Point object where top left of image will go</param>
        public void PlaceImageToPoint(Point destination)
        {
            double width = this.ShirtBitmapSource.Width;
            double height = this.ShirtBitmapSource.Height;

            this.PlaceShirtToPoint(destination, width, height);
        }

        /// <summary>
        /// Place shirt to (x - width*(1/2), y - 10) of specified destination, with a specific height and width
        /// </summary>
        /// <param name="destination">The point we will be using to shift and place the shirt</param>
        /// <param name="width">The desired width of the image</param>
        /// <param name="height">The desired height of the image</param>
        public void PlaceShirtToPoint(Point destination, double width, double height)
        {
            // determine the difference between the newly requested destination and the previous destination
            double difference = Point.Subtract(this.previousDestination, destination).Length;

            if (difference < 1.5)
            {
                this.previousDestination = destination;

                return;
            }

            this.ShirtCanvas.Children.Clear();

            double x = destination.X - (width / 2);
            double y = destination.Y - 10;

            using (DrawingContext dc = this.shirtDrawing.RenderOpen())
            {
                dc.DrawImage(
                    this.ShirtBitmapSource,
                    new Rect(x, y, width, height));
            }

            this.ShirtCanvas.Children.Add(this.visualHost);

            // track previous point, could be better to use moving average but ⏳
            this.previousDestination = destination;
        }
    }
}
