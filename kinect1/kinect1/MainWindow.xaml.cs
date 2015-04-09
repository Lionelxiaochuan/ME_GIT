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
                //TODO:
                ColorImageStream colorstream = this.kinectsensor.ColorStream;
                colorstream.Enable();
                this.colorImageBitmap = new WriteableBitmap(colorstream.FrameWidth, colorstream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this.colorImageBitmapRect = new Int32Rect(0, 0, colorstream.FrameWidth, colorstream.FrameHeight);
                this.colorImageBitmapStride = colorstream.FrameWidth * colorstream.FrameBytesPerPixel;
                ColorImage.Source = this.colorImageBitmap;
                kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
                kinectSensor.Start();

            }
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
