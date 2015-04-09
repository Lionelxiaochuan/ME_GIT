using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Windows.Forms;

namespace kinect1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //property of class 
        private KinectSensor kinectsensor;
        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageBitmapStride;
        private Skeleton[] skeletonData;

        bool IsBackwardGestureActive = true;
        bool IsForwardGestureActive = true;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => DiscoverKinectSensor();
            this.Unloaded += (s, e) => this.kinectsensor = null;
        }

        public KinectSensor Kinect
        {
            get { return this.kinectsensor; }
            set
            {
                if (this.kinectsensor != value)
                {
                    if (this.kinectsensor != null)
                    {
                        UninitializeKinectSensor(this.kinectsensor);
                        this.kinectsensor = null; 
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this.kinectsensor = value;
                        InitializeKinectSensor(this.kinectsensor);
                    }
                }
            }
        }

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (this.kinectsensor != null)
            {
                //TODO: skeletonstream enable
                //using parameter
                SkeletonStream skeletonstream = kinectSensor.SkeletonStream;
                skeletonstream.Enable();
     
                kinectSensor.SkeletonFrameReady += kinectSensor_SkeletonFrameReady;
                //TODO: colorstream enable
                //using parameter 
                ColorImageStream colorstream = kinectsensor.ColorStream;
                colorstream.Enable();
               
                this.colorImageBitmap = new WriteableBitmap(colorstream.FrameWidth, colorstream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this.colorImageBitmapRect = new Int32Rect(0, 0, colorstream.FrameWidth, colorstream.FrameHeight);
                this.colorImageBitmapStride = colorstream.FrameWidth * colorstream.FrameBytesPerPixel;
                ColorImage.Source = this.colorImageBitmap;
                kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
                kinectSensor.Start();

            }
        }

        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonframe = e.OpenSkeletonFrame())
            {
                if (null != skeletonframe)
                {
                    this.skeletonData = new Skeleton[this.kinectsensor.SkeletonStream.FrameSkeletonArrayLength];
                    skeletonframe.CopySkeletonDataTo( skeletonData );
                    Skeleton skeleton = (from s in skeletonData where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                    if (null != skeleton)
                    {
                        SkeletonCanvas.Visibility = Visibility.Visible;
                        ProcessGesture(skeleton);
                    }
                    
                }

 
            }
        }

        private void ProcessGesture(Skeleton skeleton)
        {
            Joint lefthand = (from j in skeleton.Joints where j.JointType == JointType.HandLeft select j).FirstOrDefault();
            Joint righthand = (from j in skeleton.Joints where j.JointType == JointType.HandRight select j).FirstOrDefault();
            Joint head = (from j in skeleton.Joints where j.JointType == JointType.Head select j).FirstOrDefault();

            if (righthand.Position.X > head.Position.X + 0.45)
            {
                if (!this.IsBackwardGestureActive && !this.IsForwardGestureActive)
                {
                    this.IsForwardGestureActive = true;
                    SendKeys.SendWait("{Right}");
                }
            }
            else
            {
                this.IsForwardGestureActive = false;
            }

            if (lefthand.Position.X < head.Position.X - 0.45)
            {
                if (!this.IsBackwardGestureActive && !this.IsForwardGestureActive)
                {
                    this.IsBackwardGestureActive = true;
                    SendKeys.SendWait("{Left}");

                }
            }
            else
            {
                this.IsBackwardGestureActive = false;
            }

            SetEillpsePosition(EllipseHead,head,false);
            SetEillpsePosition(EllipseLefthand, lefthand, IsBackwardGestureActive);
            SetEillpsePosition(EllipseRighthand, righthand, IsForwardGestureActive);
        }

        private void SetEillpsePosition(Ellipse ellipse, Joint joint, bool isHighlighted)
        {
            ColorImagePoint colorImagePoint = kinectsensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.InfraredResolution640x480Fps30);
            if (isHighlighted)
            {
                ellipse.Width = 60;
                ellipse.Height = 60;
                ellipse.Fill = Brushes.Green;
            }
            else
            {
                ellipse.Width = 20;
                ellipse.Height = 20;
                ellipse.Fill = Brushes.Red;
            }
            Canvas.SetLeft(ellipse, colorImagePoint.X - ellipse.ActualWidth / 2);
            Canvas.SetTop(ellipse, colorImagePoint.Y - ellipse.ActualHeight / 2);
        }

        private void UninitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                this.kinectsensor.Stop();
                this.kinectsensor.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady);
                          
            }
        }

        private void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using(ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (null != frame)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo( pixelData );
                    this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, pixelData, this.colorImageBitmapStride, 0);
                }

            }
        }

        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.kinectsensor == null)
                        this.kinectsensor = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    if (this.kinectsensor == e.Sensor)
                    {
                        this.kinectsensor = null;
                        this.kinectsensor = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.kinectsensor == null)
                        {
                            //TODO:
                        }
                    }
                    break;
                  
            }
        }
    }

}
