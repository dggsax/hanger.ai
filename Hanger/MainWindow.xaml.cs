using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Hanger.Utilities;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Kinect;

namespace Hanger
{
    /// <summary>
    /// Controller for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Thickness of joints drawn
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Format for color frames, must be same as DEPTH_IMAGE_FORMAT
        /// </summary>
        private const ColorImageFormat COLOR_IMAGE_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

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
        /// Whether we want to view debug stuff on the window
        /// </summary>
        private bool DEBUG = false;

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

        /// <summary>
        /// Object keeping track of shirts
        /// </summary>
        private ShirtManager shirtManager;

        /// <summary>
        /// Current instance of shirt
        /// </summary>
        private Shirt shirt;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Executes when ready to go
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.BeginSpeechRecognition();

            // set up shir manager and load first shirt onto skeleton
            this.shirtManager = new ShirtManager(ChosenShirt, PreviousShirt, NextShirt, this.Dispatcher);
            this.shirt = this.shirtManager.NextShirt();

            // for drawing skeleton bones and joints onto body
            this.drawingVisual = new DrawingVisual();
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
            if (this.sensor == null)
            {
                return;
            }

            // enable the necessary streams
            this.sensor.ColorStream.Enable();
            this.sensor.SkeletonStream.Enable();
            this.sensor.DepthStream.Enable();

            // add event handlers for the frames
            this.sensor.AllFramesReady += this.SensorOnAllFramesReady;

            try
            {
                this.sensor.Start();
            }
            catch (IOException)
            {
                this.sensor = null;
            }
        }

        /// <summary>
        /// Tells <see cref="SpeechRecognizer"/> to start listening!
        /// </summary>
        private async void BeginSpeechRecognition()
        {
            await this.RecognizeSpeechAsync();
        }

        /// <summary>
        /// When all the frames we need from the kinect sensor are ready, process them
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
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
                                if (this.DEBUG)
                                {
                                    this.DrawBonesAndJoints(skeleton, dc);
                                }

                                this.DrawShirtToSkeleton(skeleton);
                            }
                        }

                        SkeletonCanvas.Children.Add(this.visualHost);
                    }
                }
            }
        }

        /// <summary>
        /// Provided with a skeleton, draw the <see cref="shirt"/> to the specified skeleton
        /// </summary>
        /// <param name="skeleton">Skeleton receiving the shirt</param>
        private void DrawShirtToSkeleton(Skeleton skeleton)
        {
            // determine shoulder position
            Joint centerShoulder = skeleton.Joints[JointType.ShoulderCenter];

            if (!centerShoulder.TrackingState.Equals(JointTrackingState.Tracked))
            {
                Debug.WriteLine("Center shoulder not tracked, not drawing shirt");

                return;
            }

            if (centerShoulder.TrackingState.Equals(JointTrackingState.Inferred))
            {
                Debug.WriteLine("Center shoulder position is inferred, not drawing shirt");

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

            this.shirt.PlaceShirtToPoint(centerShoulderPoint, width * 1.75, height * 1.25);
        }

        /// <summary>
        /// Executed when window closes
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If sensor exists, then stop it as we leave
            this.sensor?.Stop();
        }

        /// <summary>
        /// Draw all the bones for a skeleton
        /// </summary>
        /// <param name="skeleton">Skeleton being drawn</param>
        /// <param name="drawingContext">Context being drawn to</param>
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

        /// <summary>
        /// Map a <see cref="SkeletonPoint"/> to <see cref="ColorImagePoint"/> that is consistent with the skeleton's position in the color video
        /// </summary>
        /// <param name="jointPosition">Position of joint in skeleton space</param>
        /// <returns>Position of joint in 2D image space</returns>
        private Point SkeletonPointToScreen(SkeletonPoint jointPosition)
        {
            ColorImagePoint colorImagePoint =
                this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(jointPosition, COLOR_IMAGE_FORMAT);

            return new Point(colorImagePoint.X, colorImagePoint.Y);
        }

        /// <summary>
        /// Draw a bone and it's joints
        /// </summary>
        /// <param name="skeleton">Skeleton object who's bone is being drawn</param>
        /// <param name="drawingContext">Context being drawn to</param>
        /// <param name="jointType0">Start joint</param>
        /// <param name="jointType1">End joint</param>
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

        /// <summary>
        /// Updates the value in the TextBlock with the text parameter
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        private void SetText(string text)
        {
            // we need to use the Dispatcher to update WPF UI elements since this method gets
            // called from the Async Task method which is a new thread, and only the main thread
            // gets direct access to the UI elements.
            this.Dispatcher.Invoke(() =>
            {
                RecognizedSpeech.Text = text;
            });
        }

        /// <summary>
        /// Check text to see if a specific command is uttered
        /// </summary>
        /// <param name="text">Text recognized by <see cref="SpeechRecognizer"/> to be analyzed for command syntax</param>
        private void ProcessCommand(string text)
        {
            if (text.ToLower().Contains("next"))
            {
                this.shirt = this.shirtManager.NextShirt();
            }
            else if (text.ToLower().Contains("back") || text.ToLower().Contains("previous"))
            {
                this.shirt = this.shirtManager.PreviousShirt();
            }
            else if (text.ToLower().Contains("debug"))
            {
                this.DEBUG = !this.DEBUG;
            }
        }

        /// <summary>
        /// Creates an asynchronous task that will continually listen to user speech
        /// </summary>
        /// <returns>Task object to be executed</returns>
        private async Task RecognizeSpeechAsync()
        {
            // Please don't abuse our API key ❤
            var config =
                SpeechConfig.FromSubscription(
                    "295bb692e6cf43bd88b8f009c1da9be6",
                    "eastus");

            var stopRecognition = new TaskCompletionSource<int>();

            using (var recognizer = new SpeechRecognizer(config))
            {
                // Ran whenever system is in the process of Recognizing voice inputs
                recognizer.Recognizing += (s, e) =>
                {
                    string result = (string)e.Result.Text.ToString();

                    if (this.DEBUG)
                    {
                        this.SetText(result);
                    }
                };

                // this might be the one we want to use when checking to see if the user said "next" or something"
                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string result = e.Result.Text;

                        if (this.DEBUG)
                        {
                            this.SetText(result);
                        }

                        this.ProcessCommand(result);
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Debug.WriteLine($"NOMATCH: Speech could not be recognized");
                    }
                };

                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                Task.WaitAny(new[] { stopRecognition.Task });

                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
    }
}
