using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveCharts.Wpf;
using LiveCharts.Helpers;
using System.IO.Ports;
using System.Threading;
using System.Timers;
using LiveCharts.Configurations;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Temp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            IdealCurve = new List<double>();
            UpperCurve = new List<double>();
            LowerCurve = new List<double>();
            points = new List<Points>();

            ch = new CartesianChart();
            ch.Zoom = ZoomingOptions.Xy;
            ch.DisableAnimations = true;

            Select_Com.ItemsSource = SerialPort.GetPortNames();
            Select_Com.SelectedIndex = 0;
            
            Time = 0;
            TotalTime = 0;
            Temperature = 0;
            UpperTempLimit = 0;
            LowerTempLimit = 0;
            enableDelete = true;
        }


        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Select_Com.ItemsSource = SerialPort.GetPortNames();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare(Connect_button.Content.ToString(), "Connetti") == 0)
            {
                try
                {
                    if (Select_Com.Text != "")
                    {
                        port = new SerialPort(Select_Com.Text, 9600, Parity.None, 8, StopBits.One);
                        Mouse.OverrideCursor = Cursors.Wait;
                        try
                        {
                            port.Open();
                            Thread.Sleep(500);

                            WorkingTimer = new System.Timers.Timer();
                            WorkingTimer.Interval = TimerRate;
                            WorkingTimer.Elapsed += WorkingTimer_Elapsed;
                            WorkingTimer.Start();
                            try
                            {
                                port.Write("RS");
                                if (Convert.ToInt32(port.ReadLine())==1)
                                {
                                    On_button.Background = Brushes.Green;
                                    Off_button.Background = Brushes.Transparent;
                                    On_button.IsEnabled = false;
                                    Off_button.IsEnabled = true;
                                }
                                else
                                {
                                    On_button.Background = Brushes.Transparent;
                                    Off_button.Background = Brushes.Green;
                                    On_button.IsEnabled = true;
                                    Off_button.IsEnabled = false;
                                }
                            }
                            catch { }
                        }
                        finally
                        {
                            Mouse.OverrideCursor = null;
                        }

                        Connect_button.Content = "Disconnetti";
                        Select_Com.IsEnabled = false;
                        On_button.IsEnabled = true;
                        Off_button.IsEnabled = true;
                        Start_button.IsEnabled = true;
                    }
                }
                catch
                {

                }
            }
            else
            {
                if (port != null)
                {
                    port.Close();
                    Start_button.IsEnabled = false;
                }
                Connect_button.Content = "Connetti";
                if(WorkingTimer.Enabled)
                    WorkingTimer.Stop();
                if (OngoingTimer.Enabled)
                    OngoingTimer.Stop();
                MinutePassed = 0;

                Insert_Button.IsEnabled = true;
                Clear_Button.IsEnabled = true;
                Select_Com.IsEnabled = true;
                On_button.IsEnabled = false;
                Off_button.IsEnabled = false;

                enableDelete = true;
            }
        }


        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if (IdealCurve.Count!=0)
            {
                if (String.Compare(Start_button.Content.ToString(), "Avvia") == 0)
                {
                    OngoingTimer = new System.Timers.Timer();

#if DEBUG
                    OngoingTimer.Interval = 60000;
#else
                    OngoingTimer.Interval = 60000;
#endif
                    OngoingTimer.Elapsed += OngoingTimer_Elapsed;
                    OngoingTimer.Start();

                    Start_button.Content = "Ferma";

                    Insert_Button.IsEnabled = false;
                    Clear_Button.IsEnabled = false;
                    On_button.IsEnabled = false;
                    Off_button.IsEnabled = false;
                    enableDelete = false;

                }
                else
                {
                    Start_button.Content = "Avvia";
                    OngoingTimer.Stop();
                    MinutePassed = 0;
                    Insert_Button.IsEnabled = true;
                    Clear_Button.IsEnabled = true;
                    On_button.IsEnabled = true;
                    Off_button.IsEnabled = true;
                    enableDelete = true;
                }
            }
            else
            {
                MessageBox.Show("Creare prima una curva!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurvesGeneration()
        {
            if (points[Index].MaxTempValue != 0)
            {
                double coefficient = (points[Index].MaxTempValue - points[Index].MinTempValue) / (double)Time;
                double value = points[Index].MinTempValue;

                for (int i = points[Index].MinTimeValue; i < points[Index].MaxTimeValue + 1; i++)
                {
                    if (coefficient != 0)
                    {
                        if (value < PhysicalLimit)
                            IdealCurve.Add(value);
                        else
                            IdealCurve.Add(PhysicalLimit);
                        value = coefficient + value;
                    }
                    else
                    {
                        IdealCurve.Add(points[Index].MaxTempValue);
                    }
                }
            }
            else
            {
                IdealCurve.Add(points[Index].MaxTempValue);
            }

            Refresh_graph();
        }

        private void Insert_Button_Click(object sender, RoutedEventArgs e)
        {
            TotalTime += Time;

            Points point = new Points();
            point.ID = Index;
            point.MinTimeValue = TotalTime;
            point.MinTempValue = Convert.ToDouble(Temperature);

            point.MaxTimeValue = Convert.ToInt32(TimeValue_TextBox.Text)+point.MinTimeValue;
            point.MaxTempValue = Convert.ToDouble(TempValue_TextBox.Text);

            points.Add(point);

            Time = Convert.ToInt32(TimeValue_TextBox.Text);
            Temperature = Convert.ToInt32(TempValue_TextBox.Text);

            Grid grid = new Grid();
            grid.Name = "Curve_point_" +Index;

            Label LabelId = new Label();
            LabelId.Name = "LabelId_" + Index;
            LabelId.Margin = new Thickness(1, 2, 2, 2);
            LabelId.HorizontalAlignment = HorizontalAlignment.Left;
            LabelId.VerticalAlignment = VerticalAlignment.Top;
            LabelId.VerticalContentAlignment = VerticalAlignment.Center;
            LabelId.Width = 30;
            LabelId.Height = 30;
            LabelId.Content = Index;
            grid.Children.Add(LabelId);

            Label LabelTemp = new Label();
            LabelTemp.Name = "LabelTemp_" + Index;
            LabelTemp.Margin = new Thickness(31, 2, 2, 2);
            LabelTemp.HorizontalAlignment = HorizontalAlignment.Left;
            LabelTemp.VerticalAlignment = VerticalAlignment.Top;
            LabelTemp.VerticalContentAlignment = VerticalAlignment.Center;
            LabelTemp.Width = 80;
            LabelTemp.Height = 30;
            LabelTemp.Content = TempValue_TextBox.Text;
            grid.Children.Add(LabelTemp);

            Label LabelTime = new Label();
            LabelTime.Name = "LabelTime_" + Index;
            LabelTime.Margin = new Thickness(111, 2, 2, 2);
            LabelTime.HorizontalAlignment = HorizontalAlignment.Left;
            LabelTime.VerticalAlignment = VerticalAlignment.Top;
            LabelTime.VerticalContentAlignment = VerticalAlignment.Center;
            LabelTime.Width = 50;
            LabelTime.Height = 30;
            LabelTime.Content = TimeValue_TextBox.Text;
            grid.Children.Add(LabelTime);

            Button ButtonDelete = new Button();
            ButtonDelete.Name = "ButtonDelete_" + Index;
            ButtonDelete.Margin = new Thickness(161, 2, 2, 2);
            ButtonDelete.HorizontalAlignment = HorizontalAlignment.Left;
            ButtonDelete.VerticalAlignment = VerticalAlignment.Top;
            ButtonDelete.VerticalContentAlignment = VerticalAlignment.Center;
            ButtonDelete.Width = 70;
            ButtonDelete.Height = 30;
            ButtonDelete.Content = "Cancella";
            ButtonDelete.Click += ButtonDelete_Click;
            grid.Children.Add(ButtonDelete);

            steps_grid.Items.Add(grid);

            CurvesGeneration();
            Index++;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (enableDelete)
            {
                int i = 0;
                foreach (Grid item in steps_grid.Items)
                {
                    if (item.Name == "Curve_point_" + ((FrameworkElement)sender).Name.Split("_")[1])
                    {
                        for (int j = points[i].MaxTimeValue; j >= points[i].MinTimeValue; j--)
                        {
                            IdealCurve.RemoveAt(j);
                        }
                        Refresh_graph(); 
                        if (i < points.Count - 1)
                        {
                            for (int j = i; j < points.Count - 1; j++)
                            {
                                int diff = points[j + 1].MaxTimeValue - points[j + 1].MinTimeValue;
                                if (j - 1 > 0)
                                    points[j].MinTimeValue = points[j - 1].MaxTimeValue;
                                points[j].MaxTimeValue = points[j].MinTimeValue + diff;
                                points[j].MinTempValue = points[j + 1].MinTempValue;
                                points[j].MaxTempValue = points[j + 1].MaxTempValue;
                            }
                        }
                        points.RemoveAt(points.Count - 1);
                        steps_grid.Items.RemoveAt(i);
                        Index--;
                        return;
                    }
                    i++;
                }
            }
            else
            {
                MessageBox.Show("Impossibile cancellare la curva durante l'utilizzo", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            GraphGrid.Children.Clear();
            UpperCurve.Clear();
            IdealCurve.Clear();
            LowerCurve.Clear();
            points.Clear();

            Time = 0;
            TotalTime = 0;
            Temperature = 0;
            Index = 0;

            steps_grid.Items.Clear();
        }

        void Refresh_graph()
        {
            UpperCurve.Clear();
            LowerCurve.Clear();

            for(int i=0; i < IdealCurve.Count(); i++)
            {
                if(IdealCurve[i] + UpperTempLimit< IdealCurve.Max())
                    UpperCurve.Add(IdealCurve[i] + UpperTempLimit);
                else
                    UpperCurve.Add(IdealCurve.Max());

                if (IdealCurve[i] - LowerTempLimit > IdealCurve.Min())
                    LowerCurve.Add(IdealCurve[i] - LowerTempLimit);
                else
                    LowerCurve.Add(IdealCurve.Min());
            }

            ch.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Limite superiore",
                    Values = UpperCurve.AsChartValues()
                },
                new LineSeries
                {
                    Title = "Curva ideale",
                    Values = IdealCurve.AsChartValues()
                },
                new LineSeries
                {
                    Title = "Limite inferiore",
                    Values = LowerCurve.AsChartValues()
                }
            };

            GraphGrid.Children.Clear();
            GraphGrid.Children.Add(ch);
        }

        private void WorkingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Read_Temp();
        }

        ChartValues<Point> test = new ChartValues<Point>();
        LineSeries point2 = new LineSeries();
        LineSeries LastPoint2;
        private void OngoingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            MinutePassed++;
            CheckHasbeenDone = false;

            if (!CheckHasbeenDone)
            {
                var mapper = new CartesianMapper<double>()
                    .X((value, index) => MinutePassed)
                    .Y((value, index) => value);

                var p = new Point() { X = MinutePassed, Y = ReadTemp };
                test.Add(p);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {

                    LineSeries Point = new LineSeries(mapper)
                    {
                        Title = "Valore attuale",
                        Values = new ChartValues<double> { ReadTemp },
                        Fill = Brushes.Red,
                        Stroke = Brushes.Red,
                        PointGeometrySize = 10,
                        PointForeground = Brushes.Red
                    };
                    point2.Configuration = new CartesianMapper<Point>()
                            .X(p => p.X)
                            .Y(p => p.Y);
                    point2.Values = test;

                    if (LastPoint != null)
                        ch.Series.Remove(LastPoint);
                    if (LastPoint2 != null)
                        ch.Series.Remove(LastPoint2);
                    ch.Series.Add(Point);
                    ch.Series.Add(point2);
                    LastPoint = Point;
                    LastPoint2 = point2;

                    GraphGrid.Children.Clear();
                    GraphGrid.Children.Add(ch);
                }));
                Thread.Sleep(200);
                if (ReadTemp <= LowerCurve[MinutePassed])
                {
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    port.WriteLine("W;1");
                    string read = port.ReadLine();
                    if (!read.Contains("S;1"))
                    {
                        var test3 = 3;
                    }
                }
                else
                {
                    if (ReadTemp >= UpperCurve[MinutePassed])
                    {
                        port.DiscardInBuffer();
                        port.DiscardOutBuffer();
                        port.WriteLine("W;0");
                        string read = port.ReadLine();
                        if (!read.Contains("S;0"))
                        {
                            var test3 = 3;
                        }
                    }
                }
                CheckHasbeenDone = true;
            }
        }

        private void On_button_Click(object sender, RoutedEventArgs e)
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            port.WriteLine("W;1");
            string read = port.ReadLine();
            if (!read.Contains("S;1"))
            {
                var test3 = 3;
            }
            On_button.Background = Brushes.Green;
            Off_button.Background = Brushes.Transparent;
            On_button.IsEnabled = false;
            Off_button.IsEnabled = true;
        }

        private void Off_button_Click(object sender, RoutedEventArgs e)
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            port.WriteLine("W;0");
            string read = port.ReadLine();
            if (!read.Contains("S;0"))
            {
                var test3 = 3;
            }
            On_button.Background = Brushes.Transparent; 
            Off_button.Background = Brushes.Green;
            On_button.IsEnabled = true;
            Off_button.IsEnabled = false;
        }

        private void Read_Temp()
        {

            port.Write("R");
            try
            {
                ReadTemp = Convert.ToInt32(port.ReadLine());
                this.Dispatcher.Invoke(() =>
                {
                    TempActual_TextBox.Content = ReadTemp + " °C";
                });
            }
            catch
            { //absorbe error
            }
        }

        /// <summary>
        /// Flag for control
        /// </summary>
        bool CheckHasbeenDone = false;

        /// <summary>
        /// Serial port
        /// </summary>
        public SerialPort port;

        /// <summary>
        /// Upper limit Curve
        /// </summary>
        List<double> UpperCurve;

        /// <summary>
        /// Ideal Curve
        /// </summary>
        List<double> IdealCurve;

        /// <summary>
        /// Lower limit Curve
        /// </summary>
        List<double> LowerCurve;

        /// <summary>
        /// Time used to reach the max value
        /// </summary>
        int Time;

        /// <summary>
        /// Total time
        /// </summary>
        int TotalTime;

        /// <summary>
        /// max temperature value
        /// </summary>
        int Temperature;

        /// <summary>
        /// Index used for curve naming
        /// </summary>
        int Index;

        /// <summary>
        /// Upper temperature threshold
        /// </summary>
        int UpperTempLimit;

        /// <summary>
        /// Lower temperature threshold
        /// </summary>
        int LowerTempLimit;

        /// <summary>
        /// Temperature read from sensor
        /// </summary>
        int ReadTemp;

        /// <summary>
        /// Minute passed from the start of monitorization
        /// </summary>
        int MinutePassed;

        /// <summary>
        /// List of temperature points
        /// </summary>
        List<Points> points;

        /// <summary>
        /// CartesianChart used to display temperature curve
        /// </summary>
        CartesianChart ch;

        /// <summary>
        /// Reading timer
        /// </summary>
        System.Timers.Timer WorkingTimer;

        /// <summary>
        /// Timer of Ongoing curve
        /// </summary>
        System.Timers.Timer OngoingTimer;

        /// <summary>
        /// Speed at which temperature read is done
        /// </summary>
        const int TimerRate = 2000;

        /// <summary>
        /// Physical temperature limit of the machine
        /// </summary>
        const int PhysicalLimit = 2000;

        /// <summary>
        /// Last point in the graph
        /// </summary>
        LineSeries LastPoint;

        /// <summary>
        /// enable deleting
        /// </summary>
        bool enableDelete;
    }
}
