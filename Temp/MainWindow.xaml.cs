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
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Select_Com.ItemsSource = SerialPort.GetPortNames();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare(Connect_button.Content.ToString(), "Connetti") == 0)
            {
                if (Select_Com.Text != "")
                {
                    Mouse.OverrideCursor = Cursors.Wait;

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
            }
            else
            {
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

                    enableDelete = true;
                }
                else
                {
                    MessageBox.Show("Non è stato possibile chiudere la connessione con la porta!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if (IdealCurve.Count!=0)
            {
                if (String.Compare(Start_button.Content.ToString(), "Avvia") == 0)
                {
#if DEBUG
                    OngoingTimer.Interval = 2000;
#else
                    OngoingTimer.Interval = 60000;
#endif
                    OngoingTimer.Elapsed += OngoingTimer_Elapsed;
                    OngoingTimer.Start();

                    Start_button.Content = "Ferma";

                    Insert_Button.IsEnabled = false;
                    Clear_Button.IsEnabled = false;
                    Mode_button.IsEnabled = false;
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
            double coefficient = (points[Index].MaxTempValue - points[Index].MinTempValue) / Convert.ToDouble(TimeInMinute);
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

            Refresh_graph();
        }

        private void AddPointOnCurve(Points point)
        {
            IdealCurve.Insert(point.MaxTimeValue, point.MaxTempValue);
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
                }
                catch
                {
                    MessageBox.Show("Formato dell'ora inserito errato!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                TotalTime += TimeInMinute;

                Points point = new Points();
                point.ID = Index;
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
            IdealCurve.Clear();
            points.Clear();

            TotalTime = 0;
            Index = 0;

            steps_grid.Items.Clear();
        }

        void Refresh_graph()
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
        }

        private void ReadingTempTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            ReadTemperature = handlerArduino.Read_Temp();

            Dispatcher.Invoke(() =>
            {
                TempActual_TextBox.Content = ReadTemperature + " °C";
            });
        }

        private void OngoingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            MinutePassed++;
            CheckHasbeenDone = false;
            if (ReadTemperature >= points[OnGoingIndex].MaxTempValue)
            {
                OnGoingIndex++;
            }
            else
            {
                if(MinutePassed> points[OnGoingIndex].MaxTimeValue)
                {
                    AddPointOnCurve(points[OnGoingIndex]);
                }
                if (IdealCurve.Count() > MinutePassed)
                    RegulateTemp(true);
                else
                {
                    RegulateTemp(false);
                }
                if (!CheckHasbeenDone)
                {
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

                    CheckHasbeenDone = true;
                }
            }
        }

        private void RegulateTemp(bool hasreference)
        {
            if (hasreference)
            {
                if (ReadTemperature < IdealCurve[MinutePassed])
                {
                    if (!handlerArduino.writeOutput(1))
                    {
                        MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                    }
                }
                else
                {
                    if (!handlerArduino.writeOutput(0))
                    {
                        MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                    }
                }
            }
            else
            {
                if (!handlerArduino.writeOutput(0))
                {
                    MessageBox.Show("Attenzione!!!\n Non è stato possibile spegnere il forno!", "Errore", MessageBoxButton.OK);
                }
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
                    }
                }
                else
                {
                    if (handlerArduino.writeOutput(0))
                    {
                        OnOff_Button.Content = "Accendi";
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

        /// <summary>
        /// Flag for control
        /// </summary>
        bool CheckHasbeenDone = false;

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
#if DEBUG
        const int ReadingTempTimerRate = 2000;
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

    }
}
