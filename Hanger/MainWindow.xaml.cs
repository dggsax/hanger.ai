using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Hanger.Utilities;
using Microsoft.Kinect;

namespace Hanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// For drawing images of skeleton and color output
        /// </summary>
        private DrawingVisual drawingVisual;

        /// <summary>
        /// Visual Host for storing DrawingVisual Output
        /// </summary>
        private VisualHost visualHost;

        /// <summary>
        /// Array of skeletons that we copy from <see cref="SkeletonFrame"/>
        /// </summary>
        private Skeleton[] skeletons = new Skeleton[6];

        /// <summary>
        /// Kinect Sensor Being Used
        /// </summary>
        private KinectSensor sensor;

        private Shirt shirt;

        /// <summary>
        /// Color of the joints that are tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Color of an inferred joint (one that Kinect assumes is there)
        /// </summary>
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Color of an inferred bone
        /// </summary>
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Color of tracked bone
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Format for color frames, must be same as DEPTH_IMAGE_FORMAT
        /// </summary>
        private const ColorImageFormat COLOR_IMAGE_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Executes when ready to go
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.shirt = new Shirt(ChosenShirt);

            // for drawing color pixels I guess
            this.drawingVisual = new DrawingVisual();

            // instantiate drawing host (this will be used to display skeleton on SkeletonCanvas
            this.visualHost = new VisualHost { visual = this.drawingVisual };

            // Select Sensor to be used
            foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    // found a potential sensor that's connected, so now we use it
                    this.sensor = potentialSensor;
                    
                    break;
                }
            }
            
            // execute only if we successfully acquired a sensor
            if (this.sensor == null) return;

            // enable the necessary streams
            this.sensor.ColorStream.Enable();
            this.sensor.SkeletonStream.Enable();
            this.sensor.DepthStream.Enable();

            // add event handlers for the frames
            this.sensor.AllFramesReady += SensorOnAllFramesReady;

            try
            {
                this.sensor.Start();
            }
            catch (IOException)
            {
                this.sensor = null;
            }
            
            //if (null == this.sensor)
            //{
            //    this.StatusBarText.Text = "No Kinect ready";
            //}
        }

        private void SensorOnAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Process the color frame
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                // We have a color frame (this is always true unless camera feed is cut)
                if (colorFrame != null)
                {
                    KinectCamera.Source = colorFrame.ToBitmap();
                }
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                SkeletonCanvas.Children.Clear();

                if (skeletonFrame != null)
                {
                    // Clear previous frame of skeleton drawings
                    // RESET FOR ORIGINAL METHOD SkeletonCanvas.Children.Clear();

                    // Copy the skeletons from the frame into class property
                    skeletonFrame.CopySkeletonDataTo(this.skeletons);

                    using (DrawingContext dc = this.drawingVisual.RenderOpen())
                    {
                        // process each skeleton
                        foreach (Skeleton skeleton in this.skeletons)
                        {
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                // NOT WORKING
                                this.DrawBonesAndJoints(skeleton, dc);

                                this.DrawShirtToSkeleton(skeleton);
                            }
                        }

                        SkeletonCanvas.Children.Add(this.visualHost);
                    }
                }
            }
        }

        private void DrawShirtToSkeleton(Skeleton skeleton)
        {
            // determine shoulder position
            Joint centerShoulder = skeleton.Joints[JointType.ShoulderCenter];

            if (!centerShoulder.TrackingState.Equals(JointTrackingState.Tracked))
            {
                Debug.WriteLine("Left shoulder not tracked, not drawing shirt");

                return;
            }

            if (centerShoulder.TrackingState.Equals(JointTrackingState.Inferred))
            {
                Debug.WriteLine("Left shoulder position is inferred, not drawing shirt");

                return;
            }

            // generate point of shoulder on screen
            Point centerShoulderPoint = this.SkeletonPointToScreen(centerShoulder.Position);

            // determine width
            Joint leftShoulder = skeleton.Joints[JointType.ShoulderLeft];
            Point leftShoulderPoint = this.SkeletonPointToScreen(leftShoulder.Position);

            Joint rightShoulder = skeleton.Joints[JointType.ShoulderRight];
            Point rightShoulderPoint = this.SkeletonPointToScreen(rightShoulder.Position);

            double width = Point.Subtract(leftShoulderPoint, rightShoulderPoint).Length;

            // determine height
            Joint head = skeleton.Joints[JointType.Head];
            Point headPoint = this.SkeletonPointToScreen(head.Position);

            Joint hip = skeleton.Joints[JointType.HipCenter];
            Point hipPoint = this.SkeletonPointToScreen(hip.Position);

            double height = Point.Subtract(headPoint, hipPoint).Length;

            Debug.WriteLine(centerShoulderPoint.ToString());

            this.shirt.DrawImage(centerShoulderPoint, width * 1.75, height * 1.25);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If sensor exists, then stop it as we leave
            sensor?.Stop();
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        public double JointThickness = 3;

        private Point SkeletonPointToScreen(SkeletonPoint jointPosition)
        {
            //            ColorImagePoint colorImagePoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, COLOR_IMAGE_FORMAT);
            ColorImagePoint colorImagePoint =
                this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(jointPosition, COLOR_IMAGE_FORMAT);
            
            return new Point(colorImagePoint.X, colorImagePoint.Y);
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
    }
}
