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
using Temp.Handlers;

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

            ch.Zoom = ZoomingOptions.Xy;
            ch.DisableAnimations = true;

            Select_Com.ItemsSource = SerialPort.GetPortNames();
            Select_Com.SelectedIndex = 0;

#if EMULATE
            myOven = new OvenSim(3.0, 1.5, 20);
#endif
#if SAMPLECURVE
            /*InsertCurvePoint(100,10);
            InsertCurvePoint(200,0);
            InsertCurvePoint(200,5);
            InsertCurvePoint(100,0);
            InsertCurvePoint(100,5);
            InsertCurvePoint(20,0);*/

           /* InsertCurvePoint(500, 120);
            InsertCurvePoint(500, 15);
            InsertCurvePoint(770, 0);
            InsertCurvePoint(770, 10);
            InsertCurvePoint(480, 0);
            InsertCurvePoint(480, 60);*/

            InsertCurvePoint(500, 120);
            InsertCurvePoint(500, 15);
            InsertCurvePoint(770, 0);
            InsertCurvePoint(770, 10);
            InsertCurvePoint(850, 0);
            InsertCurvePoint(850, 10);
            InsertCurvePoint(480, 0);
            InsertCurvePoint(480, 60);
#endif
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Select_Com.ItemsSource = SerialPort.GetPortNames();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare(Connect_button.Content.ToString(), "Connetti") == 0)
            {
#if EMULATE
                Mouse.OverrideCursor = Cursors.Wait;
                WorkingTimer.Interval = ReadingTempTimerRate;
                WorkingTimer.Elapsed += ReadingTempTimer_Elapsed;
                WorkingTimer.Start();
                Mouse.OverrideCursor = null;

                Connect_button.Content = "Disconnetti";
                Select_Com.IsEnabled = false;
                Start_button.IsEnabled = true;
                OnOff_Button.IsEnabled = true;
                Pause_button.Visibility = Visibility.Visible;
#else
                if (Select_Com.Text != "")
                {
                    if (handlerArduino.Connect(Select_Com.Text))
                    {

                        Thread.Sleep(100);

                        WorkingTimer.Interval = ReadingTempTimerRate;
                        WorkingTimer.Elapsed += ReadingTempTimer_Elapsed;
                        WorkingTimer.Start();
                        Mouse.OverrideCursor = null;

                        Connect_button.Content = "Disconnetti";
                        Select_Com.IsEnabled = false;
                        Start_button.IsEnabled = true;
                        OnOff_Button.IsEnabled = true;
                        Pause_button.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Non è stato possibile connettersi alla porta selezionata.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
            }
            else
                {
                    MessageBox.Show("Selezionare una porta!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
#endif
            }
            else
            {
#if EMULATE
                    Start_button.IsEnabled = false;
                    Connect_button.Content = "Connetti";
                    if (WorkingTimer.Enabled)
                        WorkingTimer.Stop();
                    if (OngoingTimer.Enabled)
                        OngoingTimer.Stop();
                    MinutePassed = 0;

                    Insert_Button.IsEnabled = true;
                    Clear_Button.IsEnabled = true;
                    Select_Com.IsEnabled = true;
                    OnOff_Button.IsEnabled = false;
                    Pause_button.Visibility = Visibility.Hidden;

                enableDelete = true;
#else
                if (handlerArduino.Disconnect())
                {
                    Start_button.IsEnabled = false;
                    Connect_button.Content = "Connetti";
                    if (WorkingTimer.Enabled)
                        WorkingTimer.Stop();
                    if (OngoingTimer.Enabled)
                        OngoingTimer.Stop();
                    MinutePassed = 0;

                    Insert_Button.IsEnabled = true;
                    Clear_Button.IsEnabled = true;
                    Select_Com.IsEnabled = true;
                    OnOff_Button.IsEnabled = false;
                    Pause_button.Visibility = Visibility.Hidden;

                    enableDelete = true;
                }
                else
                {
                    MessageBox.Show("Non è stato possibile chiudere la connessione con la porta!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
#endif
            }
        }


        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if (IdealCurve.Count!=0)
            {
                if (String.Compare(Start_button.Content.ToString(), "Avvia") == 0)
                {
#if EMULATE
                    OngoingTimer.Interval = 250;
#else
                    OngoingTimer.Interval = 60000;
#endif
                    OngoingTimer.Elapsed += OngoingTimer_Elapsed;
                    OngoingTimer.Start();
                    OngoingTimer_Elapsed(this, null);

                    Start_button.Content = "Ferma";

                    Insert_Button.IsEnabled = false;
                    Clear_Button.IsEnabled = false;
                    Mode_button.IsEnabled = false;
                    Pause_button.IsEnabled = true;
                    enableDelete = false;

                }
                else
                {
                    Start_button.Content = "Avvia";
                    OngoingTimer.Stop();
                    ReadingChart.Clear();
                    MinutePassed = 0;
                    Insert_Button.IsEnabled = true;
                    Clear_Button.IsEnabled = true;
                    Mode_button.IsEnabled = true;
                    Pause_button.IsEnabled = false;
                    enableDelete = true;
                }
            }
            else
            {
                MessageBox.Show("Creare prima una curva!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurvesGeneration(int time = -1)
        {
            double coefficient = 1;
            if (time != -1)
            {
                if(time > 1)
                    coefficient = (points[Index].MaxTempValue - points[Index].MinTempValue) / Convert.ToDouble(time);
            }
            else
            {
                coefficient = (points[Index].MaxTempValue - points[Index].MinTempValue) / Convert.ToDouble(TimeInMinute);
            }
            double value = points[Index].MinTempValue;

            int delta = 0;
            if (points[Index].MinTimeValue == 0)
                delta = 1;

            for (int i = points[Index].MinTimeValue; i < points[Index].MaxTimeValue + delta; i++)
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

            Refresh_graph();
        }

        private void AddPointOnCurve(Points point)
        {
            IdealCurve.Insert(MinutePassed + 1, point.MaxTempValue);
            Refresh_graph();
        }

        private void Insert_Button_Click(object sender, RoutedEventArgs e)
        {
            if (TimeValue_TextBox.Text != "" && TempValue_TextBox.Text != "")
            {
                try
                {
                    if (TimeValue_TextBox.Text.Contains(':'))
                    {
                        TimeInMinute = (Convert.ToInt32(TimeValue_TextBox.Text.Split(':')[0]) * 60) + Convert.ToInt32(TimeValue_TextBox.Text.Split(':')[1]);
                    }
                    else
                    {
                        TimeInMinute = Convert.ToInt32(TimeValue_TextBox.Text) * 60;
                    }
                    if (TimeInMinute == 0)
                        TimeInMinute = 1;
                }
                catch
                {
                    MessageBox.Show("Formato dell'ora inserito errato!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                TotalTime += TimeInMinute;

                Points point = new Points();
                point.ID = Index + 1;
                if (points.Count() > 0)
                {
                    point.MinTempValue = points.Last().MaxTempValue;
                    point.MinTimeValue = points.Last().MaxTimeValue;
                }
                else
                {
                    point.MinTempValue = Convert.ToDouble(ReadTemperature);
                    point.MinTimeValue = 0;
                }

                point.MaxTimeValue = TimeInMinute + point.MinTimeValue;
                point.MaxTempValue = Convert.ToDouble(TempValue_TextBox.Text);

                points.Add(point);

                Grid grid = new Grid();
                grid.Name = "Curve_point_" + Index;

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
                LabelTime.Content = TimeInMinute;
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
            else
            {
                MessageBox.Show("Inserire un valore per Temperatura e tempo!", "Errore", MessageBoxButton.OK);
            }
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
                        if (points.Count() > 1)
                        {
                            for (int j = points[i].MaxTimeValue + 1; j >= points[i].MinTimeValue; j--)
                            {
                                IdealCurve.RemoveAt(j);
                            }
                        }
                        else
                        {
                            Clear_Button_Click(null, e);
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
                        if (points.Count > 0)
                        {
                            points.RemoveAt(points.Count - 1);
                            steps_grid.Items.RemoveAt(i);
                            Index--;
                        }
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
            IdealCurve.Clear();
            points.Clear();

            TotalTime = 0;
            Index = 0;

            steps_grid.Items.Clear();
        }

        void Refresh_graph()
        {

            Dispatcher.Invoke(() =>
            {
                ch.Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Curva ideale",
                        Values = IdealCurve.AsChartValues()
                    }
                };

                GraphGrid.Children.Clear();
                GraphGrid.Children.Add(ch);
            });
        }

        private void ReadingTempTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
#if EMULATE
            ReadTemperature = (int)Math.Floor(myOven.GetTemp());
#else
            string rawString = handlerArduino.Read_Temp_and_Status();
            ReadTemperature = Convert.ToInt32(rawString.Split(';')[0]);
            Status = Convert.ToBoolean(rawString.Split(';')[1]);
#endif
            Dispatcher.Invoke(() =>
            {
                TempActual_TextBox.Content = ReadTemperature + " °C";
#if !EMULATE
                if (Status)
                {
                    switchStatus_Label.Visibility = Visibility.Visible;
                    switchStatus_Label.Content = "Acceso";
                    switchStatus_Label.Background = Brushes.Green;
                }
                else
                {
                    switchStatus_Label.Visibility = Visibility.Visible;
                    switchStatus_Label.Content = "Spento";
                    switchStatus_Label.Background = Brushes.Red;
                }
#endif
            });
        }

        private void OngoingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            bool addingPointNotNeeded = false;

            if (OnGoingIndex < points.Count - 1)
            {
                if (points[OnGoingIndex].MinTempValue < points[OnGoingIndex].MaxTempValue)
                {
                    if (ReadTemperature >= points[OnGoingIndex].MaxTempValue)
                    {
                        OnGoingIndex++;
                    }
                }
                else
                {
                    if (points[OnGoingIndex].MinTempValue > points[OnGoingIndex].MaxTempValue)
                    {
                        if (ReadTemperature <= points[OnGoingIndex].MaxTempValue)
                        {
                            OnGoingIndex++;
                        }
                    }
                    else
                    {
                        addingPointNotNeeded = true;
                        if (MinutePassed > points[OnGoingIndex].MaxTimeValue)
                            OnGoingIndex++;
                    }
                }

                if (MinutePassed > points[OnGoingIndex].MaxTimeValue)
                {

                    if (!addingPointNotNeeded)
                    {
                        AddPointOnCurve(points[OnGoingIndex]);

                        points[OnGoingIndex].MaxTimeValue++;

                        for (int i = OnGoingIndex + 1; i < points.Count; i++)
                        {
                            points[i].MinTimeValue++;
                            points[i].MaxTimeValue++;
                        }
                    }
                }
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                int hour = Convert.ToInt32(Math.Floor((double)MinutePassed / 60));
                int minute = MinutePassed - (hour * 60);
                TimeCount_Value_Label.Content = hour.ToString("00") + ":" + minute.ToString("00") + " h";
                int lefthour = Convert.ToInt32(Math.Floor(((double)IdealCurve.Count() - MinutePassed)/60));
                int leftminute = Convert.ToInt32((double)IdealCurve.Count() - MinutePassed)-(lefthour*60);
                if(lefthour > 0 && leftminute > 0) { 
                    TimeExpected_Value_Label.Content = lefthour.ToString("00") + ":" + leftminute.ToString("00") + " h";
                }
                else
                {
                    TimeExpected_Value_Label.Content = "--:-- h";
                }
            }));

            if (e != null)
                MinutePassed++;

            if (IdealCurve.Count() > MinutePassed)
            {
                RegulateTemp(true);
            }
            else
            {
                RegulateTemp(false);
            }

            var mapper = new CartesianMapper<double>()
                .X((value, index) => MinutePassed)
                .Y((value, index) => value);

            var p = new Point() { X = MinutePassed, Y = ReadTemperature };
            ReadingChart.Add(p);

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {

                LineSeries Point = new LineSeries(mapper)
                {
                    Title = "Valore attuale",
                    Values = new ChartValues<double> { ReadTemperature },
                    Fill = Brushes.Red,
                    Stroke = Brushes.Red,
                    PointGeometrySize = 10,
                    PointForeground = Brushes.Red
                };
                ReadingPoint.Configuration = new CartesianMapper<Point>()
                        .X(p => p.X)
                        .Y(p => p.Y);
                ReadingPoint.Values = ReadingChart;

                if (LastPoint != null)
                    ch.Series.Remove(LastPoint);
                if (LastPointReading != null)
                    ch.Series.Remove(LastPointReading);
                ch.Series.Add(Point);
                ch.Series.Add(ReadingPoint);
                LastPoint = Point;
                LastPointReading = ReadingPoint;

                GraphGrid.Children.Clear();
                GraphGrid.Children.Add(ch);
            }));
            Thread.Sleep(200);
        }

        private void RegulateTemp(bool hasreference)
        {
            if (hasreference)
            {
                if (ReadTemperature < IdealCurve[MinutePassed])
                {
#if !EMULATE
                    if (!handlerArduino.writeOutput(1))
                    {
                        MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                    }
#else
                    myOven.Start();
                    Dispatcher.Invoke(() =>
                    {
                        switchStatus_Label.Visibility = Visibility.Visible;
                        switchStatus_Label.Content = "Acceso";
                        switchStatus_Label.Background = Brushes.Green;
                    });
#endif
                }
                else
                {
#if !EMULATE
                    if (!handlerArduino.writeOutput(0))
                    {
                        MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                    }
#else
                    myOven.Stop();
                    Dispatcher.Invoke(() =>
                    {
                        switchStatus_Label.Visibility = Visibility.Visible;
                        switchStatus_Label.Content = "Spento";
                        switchStatus_Label.Background = Brushes.Red;
                    });
#endif
                }
            }
            else
            {
#if !EMULATE
                if (!handlerArduino.writeOutput(0))
                {
                    MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                }
#endif
            }
        }

        private void Mode_button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare(Mode_button.Content.ToString(), "Modalità manuale") == 0 )
            {
                Start_button.Visibility = Visibility.Hidden;

                OnOff_Button.Visibility = Visibility.Visible;
                Temp_Label.Visibility = Visibility.Hidden;
                TempUm_Label.Visibility = Visibility.Hidden;
                Time_Label.Visibility = Visibility.Hidden;
                TimeUm_Label.Visibility = Visibility.Hidden;
                TimeValue_TextBox.Visibility = Visibility.Hidden;
                TempValue_TextBox.Visibility = Visibility.Hidden;
                Insert_Button.Visibility = Visibility.Hidden;

                Title_grid.Visibility = Visibility.Hidden;
                steps_grid.Visibility = Visibility.Hidden;

                Mode_button.Content = "Modalità auto";
            }
            else
            {
                Start_button.Visibility = Visibility.Visible;

                OnOff_Button.Visibility = Visibility.Hidden;
                Temp_Label.Visibility = Visibility.Visible;
                TempUm_Label.Visibility = Visibility.Visible;
                Time_Label.Visibility = Visibility.Visible;
                TimeUm_Label.Visibility = Visibility.Visible;
                TimeValue_TextBox.Visibility = Visibility.Visible;
                TempValue_TextBox.Visibility = Visibility.Visible;
                Insert_Button.Visibility = Visibility.Visible;

                Title_grid.Visibility = Visibility.Visible;
                steps_grid.Visibility = Visibility.Visible;
                if(String.Compare(Connect_button.Content.ToString(), "Disconnetti") == 0)
                    OnOff_Button_Click(null, e);
                Mode_button.Content = "Modalità manuale";

            }
        }

        private void OnOff_Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                if (String.Compare(OnOff_Button.Content.ToString(), "Accendi") == 0)
                {
                    if (handlerArduino.writeOutput(1))
                    {
                        OnOff_Button.Content = "Spegni";
                        switchStatus_Label.Content = "Acceso";
                        switchStatus_Label.Background = Brushes.Green;
                    }
                }
                else
                {
                    if (handlerArduino.writeOutput(0))
                    {
                        OnOff_Button.Content = "Accendi";
                        switchStatus_Label.Content = "Spento";
                        switchStatus_Label.Background = Brushes.Red;
                    }
                }
            }
            else {
                if (handlerArduino.writeOutput(0))
                {
                    OnOff_Button.Content = "Accendi";
                }
            }
        }
        private void Pause_button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare(Pause_button.Content.ToString(), "Pausa") == 0)
            {
                OngoingTimer.Stop();
                Pause_button.Content = "In Pausa";
                Pause_button.Background = Brushes.Green;
            }
            else
            {
                OngoingTimer.Start();
                Pause_button.Content = "Pausa";
                Pause_button.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            }

        }

        private void InsertCurvePoint(double Temp, int duration)
        {
            Points point = new Points();
            point.ID = Index + 1;
            if (points.Count() > 0)
            {
                point.MinTempValue = points.Last().MaxTempValue;
                point.MinTimeValue = points.Last().MaxTimeValue;
            }
            else
            {
                point.MinTempValue = Convert.ToDouble(ReadTemperature);
                point.MinTimeValue = 0;
            }

            point.MaxTimeValue = duration + point.MinTimeValue;
            point.MaxTempValue = Convert.ToDouble(Temp);

            points.Add(point);

            Grid grid = new Grid();
            grid.Name = "Curve_point_" + Index;

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
            LabelTemp.Content = Temp;
            grid.Children.Add(LabelTemp);

            Label LabelTime = new Label();
            LabelTime.Name = "LabelTime_" + Index;
            LabelTime.Margin = new Thickness(111, 2, 2, 2);
            LabelTime.HorizontalAlignment = HorizontalAlignment.Left;
            LabelTime.VerticalAlignment = VerticalAlignment.Top;
            LabelTime.VerticalContentAlignment = VerticalAlignment.Center;
            LabelTime.Width = 50;
            LabelTime.Height = 30;
            LabelTime.Content = duration;
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

            CurvesGeneration(duration);
            Index++;
        }

        /// <summary>
        /// Ideal Curve
        /// </summary>
        List<double> IdealCurve = new List<double>();

        /// <summary>
        /// Total time
        /// </summary>
        int TotalTime = 0;

        /// <summary>
        /// read temperature value
        /// </summary>
        int ReadTemperature = 0;

        /// <summary>
        /// Index used for curve naming
        /// </summary>
        int Index;

        /// <summary>
        /// Minute passed from the start of monitorization
        /// </summary>
        int MinutePassed;

        /// <summary>
        /// List of temperature points
        /// </summary>
        List<Points> points = new List<Points>();

        /// <summary>
        /// CartesianChart used to display temperature curve
        /// </summary>
        CartesianChart ch = new CartesianChart();

        /// <summary>
        /// Reading timer
        /// </summary>
        System.Timers.Timer WorkingTimer = new System.Timers.Timer();

        /// <summary>
        /// Timer of Ongoing curve
        /// </summary>
        System.Timers.Timer OngoingTimer = new System.Timers.Timer();

        /// <summary>
        /// Speed at which temperature read is done
        /// </summary>
#if EMULATE
        const int ReadingTempTimerRate = 250;
#else
        const int ReadingTempTimerRate = 2000;
#endif

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
        bool enableDelete = true;

        /// <summary>
        /// Handler for Arduino communication
        /// </summary>
        HandlerArduino handlerArduino = new HandlerArduino();

        /// <summary>
        /// Chart of the reading temperature
        /// </summary>
        ChartValues<Point> ReadingChart = new ChartValues<Point>();

        /// <summary>
        /// Point of the reading temperature
        /// </summary>
        LineSeries ReadingPoint = new LineSeries();

        /// <summary>
        /// Lines of the reading temperature
        /// </summary>
        LineSeries LastPointReading;

        /// <summary>
        /// Time in minutes from hour
        /// </summary>
        int TimeInMinute = 0;

        /// <summary>
        /// index of ongoing points
        /// </summary>
        int OnGoingIndex = 0;

        /// <summary>
        /// status of the oven
        /// </summary>
        bool Status = false;

#if EMULATE
        OvenSim myOven;
#endif

    }
}
