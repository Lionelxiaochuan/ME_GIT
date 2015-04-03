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
        private short[] depthImagePixelData;
        private short[] depthPixelDate;

        private DepthImageFrame lastDepthFrame;

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
                    this.CreateColorDepthImage(depthFrame, depthPixelDate);
                }
            }


            //if (lastDepthFrame != null)
            //{
            //    lastDepthFrame.Dispose();
            //    lastDepthFrame = null;
            //}
            //lastDepthFrame = e.OpenDepthImageFrame();
            //if (lastDepthFrame != null)
            //{
            //    depthPixelDate = new short[lastDepthFrame.PixelDataLength];
            //    lastDepthFrame.CopyPixelDataTo(depthPixelDate);
            //    depthImageBitmap.WritePixels(depthImageBitmapRect, depthPixelDate, depthImageStride, 0);
            //}
        }

        //private void DepthImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    Point p = e.GetPosition(DepthImage);
        //    if (depthPixelDate != null && depthPixelDate.Length > 0)
        //    {
        //        Int32 pixelIndex = (Int32)(p.X + ((Int32)p.Y * this.lastDepthFrame.Width));
        //        Int32 depth = this.depthPixelDate[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
        //        Int32 depthInches = (Int32)(depth * 0.0393700787);
        //        Int32 depthFt = depthInches / 12;
        //        depthInches = depthInches % 12;
        //        PixelDepth.Text = String.Format("{0}mm~{1}'{2}", depth, depthFt, depthInches);
        //    }
        //}

        private void CreateLighterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Int32 loThreashold = 0;
            Int32 hiThreshold = 3500;
            short[] enhPixelData = new short[depthFrame.Width * depthFrame.Height];
            for (int i = 0; i < pixelData.Length; i++)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (depth < loThreashold || depth > hiThreshold)
                {
                    enhPixelData[i] = 0xFF;
                }
                else
                {
                    enhPixelData[i] = (short)~pixelData[i];
                }

            }
            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Gray16, null, enhPixelData, depthFrame.Width * depthFrame.BytesPerPixel);
        }

        private void CreateBetterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Int32 gray;
            Int32 loThreashold = 0;
            Int32 bytePerPixel = 4;
            Int32 hiThreshold = 3500;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytePerPixel];
            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytePerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (depth < loThreashold || depth > hiThreshold)
                {
                    gray = 0xFF;
                }
                else
                {
                    gray = (255 * depth / 0xFFF);
                }
                enhPixelData[j] = (byte)gray;
                enhPixelData[j + 1] = (byte)gray;
                enhPixelData[j + 2] = (byte)gray;

            }
            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytePerPixel);
        }

        private void CreateColorDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Double hue;
            Int32 loThreshold = 1200;
            Int32 hiThreshold = 3500;
            Int32 bytesPerPixel = 4;
            byte[] rgb = new byte[3];
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];

            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth < loThreshold || depth > hiThreshold)
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
                else
                {
                    hue = ((360 * depth / 0xFFF) + loThreshold);
                    ConvertHslToRgb(hue, 100, 100, rgb);

                    enhPixelData[j] = rgb[2];  //Blue
                    enhPixelData[j + 1] = rgb[1];  //Green
                    enhPixelData[j + 2] = rgb[0];  //Red
                }
            }

            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytesPerPixel);
        }

        public void ConvertHslToRgb(Double hue, Double saturation, Double lightness, byte[] rgb)
        {
            Double red = 0.0;
            Double green = 0.0;
            Double blue = 0.0;
            hue = hue % 360.0;
            saturation = saturation / 100.0;
            lightness = lightness / 100.0;

            if (saturation == 0.0)
            {
                red = lightness;
                green = lightness;
                blue = lightness;
            }
            else
            {
                Double huePrime = hue / 60.0;
                Int32 x = (Int32)huePrime;
                Double xPrime = huePrime - (Double)x;
                Double L0 = lightness * (1.0 - saturation);
                Double L1 = lightness * (1.0 - (saturation * xPrime));
                Double L2 = lightness * (1.0 - (saturation * (1.0 - xPrime)));

                switch (x)
                {
                    case 0:
                        red = lightness;
                        green = L2;
                        blue = L0;
                        break;
                    case 1:
                        red = L1;
                        green = lightness;
                        blue = L0;
                        break;
                    case 2:
                        red = L0;
                        green = lightness;
                        blue = L2;
                        break;
                    case 3:
                        red = L0;
                        green = L1;
                        blue = lightness;
                        break;
                    case 4:
                        red = L2;
                        green = L0;
                        blue = lightness;
                        break;
                    case 5:
                        red = lightness;
                        green = L0;
                        blue = L1;
                        break;
                }
            }

            rgb[0] = (byte)(255.0 * red);
            rgb[1] = (byte)(255.0 * green);
            rgb[2] = (byte)(255.0 * blue);
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
