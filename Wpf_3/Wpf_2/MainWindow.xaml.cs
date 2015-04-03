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
using System.IO;

namespace Wpf_2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //私有Kinectsensor对象
        private KinectSensor kinect;

        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;
        private byte[] colorImagePixelData;

        private WriteableBitmap depthImageBitmap;
        private Int32Rect depthImageBitmapRect;
        private int depthImageStride;
        private byte[] depthImagePixelData;

        public KinectSensor Kinect
        {
            get { return this.kinect; }
            set
            {
                //如果带赋值的传感器和目前的不一样
                if (this.kinect != value)
                {
                    //如果当前的传感对象不为null
                    if (this.kinect != null)
                    {
                        UninitializeKinectSensor(this.kinect);
                        //uninitailize当前对象
                        this.kinect = null;
                    }
                    //如果传入的对象不为空，且状态为连接状态
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this.kinect = value;
                        InitializeKinectSensor(this.kinect);
                    }
                }
            }
        }

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                DepthImageStream depthStream = kinectSensor.DepthStream;
                depthStream.Enable();

                depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                DepthImage.Source = depthImageBitmap;
                kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    short[] depthPixelDate = new short[depthFrame.PixelDataLength];
                    depthFrame.CopyPixelDataTo(depthPixelDate);
                    depthImageBitmap.WritePixels(depthImageBitmapRect, depthPixelDate, depthImageStride, 0);
                }
            }
        }


        private void UninitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
                kinectSensor.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady);
            }
        }

        void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, pixelData, this.colorImageStride, 0);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => DiscoverKinectSensor();
            this.Unloaded += (s, e) => this.kinect = null;
        }

        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.kinect == null)
                        this.kinect = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    if (this.kinect == e.Sensor)
                    {
                        this.kinect = null;
                        this.kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.kinect == null)
                        {
                            //TODO:通知用于Kinect已拔出
                        }
                    }
                    break;
                //TODO:处理其他情况下的状态
            }
        }

        private void TakePictureButton_Click(object sender, RoutedEventArgs e)
        {
            String fileName = "snapshot.jpg";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (FileStream savedSnapshot = new FileStream(fileName, FileMode.CreateNew))
            {
                BitmapSource image = (BitmapSource)DepthImage.Source;
                JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
                jpgEncoder.QualityLevel = 70;
                jpgEncoder.Frames.Add(BitmapFrame.Create(image));
                jpgEncoder.Save(savedSnapshot);

                savedSnapshot.Flush();
                savedSnapshot.Close();
                savedSnapshot.Dispose();
            }
        }
    }
}
