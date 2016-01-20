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
using System.Diagnostics;
using Microsoft.Kinect;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace KinectGraphs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectSensor kinect;
        private MultiSourceFrameReader frameReader;

        private double LastX = 0;
        private double LastY = 0;
        private double LastZ = 0;

        private TimeSpan LastRelativeTime = new TimeSpan();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new KinectDataViewModel();

            this.Loaded += new RoutedEventHandler(this.MainWindow_Loaded);
        }

        protected void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.kinect = KinectSensor.GetDefault();
            kinect.Open();

            if (kinect.IsOpen)
            {
                kinectStatusLabel.Content = "Kinect Ready!";
            }
            else
            {
                kinectStatusLabel.Content = "Kinect not Ready!";
            }

            frameReader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Infrared | FrameSourceTypes.Depth | FrameSourceTypes.Color);

            frameReader.MultiSourceFrameArrived += MainWindow_KinectFrameArrived;

        }

        private void MainWindow_KinectFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var frameRef = e.FrameReference.AcquireFrame();

            using (var frame = frameRef.BodyFrameReference.AcquireFrame())
            {
                
                if (frame != null)
                {
                    var bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    foreach (var body in bodies) { 
                        if (body.IsTracked)
                        {
                            Joint handJoint = body.Joints[JointType.HandRight];

                            var tstamp = frame.RelativeTime;

                            double timeDelta = ((double)(tstamp - LastRelativeTime).TotalMilliseconds) / 1000;
                            double x = handJoint.Position.X;
                            double y = handJoint.Position.Y;
                            double z = handJoint.Position.Z;

                            double xVel = (x - LastX) / timeDelta;
                            double yVel = (y - LastY) / timeDelta;
                            double zVel = (z - LastZ) / timeDelta;

                            LastRelativeTime = tstamp;

                            LastX = x;
                            LastY = y;
                            LastZ = z;

                            LastRelativeTime = tstamp;

                            sensorValueLabel.Content = String.Format("{0:0.000}, {1:0.000}, {2:0.000}", xVel, yVel, zVel);

                            ((KinectDataViewModel)this.DataContext).AddDataPoint(DateTime.Now, xVel, yVel, 0);
                        }
                        else
                        {
                            //sensorValueLabel.Content = String.Format("??");
                        }
                    }
                }
            }
        }

    }

    public class KinectDataViewModel
    {

        private LineSeries XSeries;
        private LineSeries YSeries;
        private LineSeries ZSeries;

        public PlotModel PlotModel { get; private set; }

        public KinectDataViewModel()
        {

            this.XSeries = new LineSeries { Title = "Hand X" };
            this.YSeries = new LineSeries { Title = "Hand Y" };
            this.ZSeries = new LineSeries { Title = "Hand Z" };

            var dataPlotModel = new PlotModel()
            {
                Title = "Right Hand Position"
            };

            dataPlotModel.Series.Add(XSeries);
            dataPlotModel.Series.Add(YSeries);
            dataPlotModel.Series.Add(ZSeries);

            dataPlotModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom });

            this.KinectDataModel = dataPlotModel;
        }

        public void AddDataPoint(DateTime tstamp, double x, double y, double z)
        {
            double timeValue = DateTimeAxis.ToDouble(tstamp);

            if (XSeries.Points.Count == 200)
            {
                XSeries.Points.RemoveAt(0);
            }

            XSeries.Points.Add(new DataPoint(timeValue, x));

            if (YSeries.Points.Count == 200)
            {
                YSeries.Points.RemoveAt(0);
            }

            YSeries.Points.Add(new DataPoint(timeValue, y));

            if (ZSeries.Points.Count == 200)
            {
                ZSeries.Points.RemoveAt(0);
            }

            ZSeries.Points.Add(new DataPoint(timeValue, z));

            this.KinectDataModel.InvalidatePlot(true);
        }

        public PlotModel KinectDataModel { get; private set; }

    }

}
