using System;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Media;
using System.Reflection;
using ZXing;
using ZXing.Common;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Linq;
using Emgu.CV.Util;
using System.Collections.ObjectModel;

namespace CaptureWebcam
{
    public partial class MainWindow : Window
    {
        VideoCapture capture;
        Timer timer;
        public ObservableCollection<string> QRList { get; set; } = new ObservableCollection<string>() { "Scanned QRs"};
        public MainWindow()
        {
            InitializeComponent();

            listofQR.ItemsSource = QRList;
            //the fps of the webcam
            int cameraFps = 30;

            //init the camera
            capture = new VideoCapture();
            
            //set the captured frame width and height (default 640x480)
            capture.Set(CapProp.FrameWidth, 1024);
            capture.Set(CapProp.FrameHeight, 768);            

            //create a timer that refreshes the webcam feed
            timer = new Timer()
            {
                Interval = 1000 / cameraFps,
                Enabled = true
            };
            timer.Elapsed += new ElapsedEventHandler(timer_Tick);
        }


        private async void timer_Tick(object sender, ElapsedEventArgs e)
        {

            this.Dispatcher.Invoke(() =>
            {
                var mat1 = capture.QueryFrame();
                var mat2 = new Mat();

                //flip the image horizontally
                CvInvoke.Flip(mat1, mat2, FlipType.Horizontal);

                //convert the mat to a bitmap
                var bmp = mat2.ToImage<Bgr, byte>().ToBitmap();

                var source = new BitmapLuminanceSource(bmp);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                var result = new MultiFormatReader().decode(bitmap);

                if(result!= null)
                {
                    
                    QRList.Add(result.Text);
                    if(QRList.Count > 50)
                    {
                        var b = QRList[0];
                        QRList.Clear();
                        QRList.Add(b);
                    }

                    var rPoints = result.ResultPoints;
                    //0 botright 1 topright 2 topleft 3 botleft
                    if(rPoints.Length > 0)
                    {
                        System.Drawing.Point[] points = new System.Drawing.Point[rPoints.Length+1];
                        for (var i = 0; i < rPoints.Length; i++)
                        {
                            var newX = (int)Math.Round(rPoints[i].X);
                            var newY = (int)Math.Round(rPoints[i].Y);
                            points[i].X = newX;
                            points[i].Y = newY;
                        }
                        points[rPoints.Length] = points[0];
                        
                        System.Drawing.Point textPoint = points[2];
                        textPoint.Offset(0, -15);

                        //This was me learning contours

                        //Mat binaryMat = new Mat();
                        //Mat grayMat2 = mat2;
                        //VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                        //Mat hierarchy = new Mat();

                        //CvInvoke.CvtColor(mat2, grayMat2, ColorConversion.Bgr2Gray, 1);

                        //CvInvoke.Threshold(grayMat2, binaryMat, 180, 255, ThresholdType.Binary);

                        //CvInvoke.CvtColor(mat2, binaryMat, ColorConversion.Bgr2Gray, 1);

                        //CvInvoke.FindContours(binaryMat, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                        //CvInvoke.DrawContours(mat2, contours, -1, new MCvScalar(255, 0, 0), 5);

                        //draw on the matrix the polylines and text
                        CvInvoke.PutText(mat2, "QR Code", textPoint,FontFace.HersheySimplex, 2, new MCvScalar(255,0,0), 4);
                        CvInvoke.Polylines(mat2, points, false, new MCvScalar(0, 0, 255), 2);
                    }
                }

                var bmpDrawn = mat2.ToImage<Bgr, byte>().ToBitmap();
                //copy the bitmap to a memorystream
                var ms = new MemoryStream();
                bmpDrawn.Save(ms, ImageFormat.Bmp);

                //display the image on the ui
                feedImage.Source = BitmapFrame.Create(ms);


            });
        }
    }
}
