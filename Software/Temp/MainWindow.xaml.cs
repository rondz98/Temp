using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Temp.Entities;
using Temp.Handlers;
using Temp.Helpers;

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
            for (int i = 0; i < Select_Com.Items.Count; i++)
            {
                if (Select_Com.Items[i].ToString() == Properties.Settings.Default.LastComPort)
                {
                    Select_Com.SelectedIndex = i;
                    break;
                }
            }

            if (Select_Com.SelectedIndex == -1)
            {
                Select_Com.SelectedIndex = 0;
            }

#if EMULATE
            myOven = new OvenSim(10.0, 7.5, 20);
#endif
            curveParser = new CustomCurveParse();
            graphHelper = new GraphHelper();
            string previousCurve = Properties.Settings.Default.LastCurve;
            int index = -1;
            parseCurvesFiles(index);
            if (previousCurve != "")
            {
                for (int i = 0; i < Curve_ComboBox.Items.Count; i++)
                {
                    if (((ComboBoxItem)Curve_ComboBox.Items[i]).Tag.ToString() == previousCurve)
                    {
                        index = i;
                        break;
                    }
                }
            }
            parseCurvesFiles(index);

            Refresh_IdealCurve(string.IsNullOrEmpty(previousCurve) ? "" : previousCurve);
        }

        private void parseCurvesFiles(int previousSelected = -1)
        {
            curveParser.parseFiles();
            Curve_ComboBox.Items.Clear();
            ComboBoxItem comboBoxItem = new ComboBoxItem();
            comboBoxItem.Tag = "None";
            comboBoxItem.Name = "None_" + 0;
            comboBoxItem.Content = "Nuova curva personalizzata";
            Curve_ComboBox.Items.Add(comboBoxItem);

            Curve_ComboBox.SelectedIndex = 0;

            if (curveParser.curves.Count > 0)
            {
                int i = 1;
                foreach (Curve curve in curveParser.curves)
                {
                    comboBoxItem = new ComboBoxItem();
                    comboBoxItem.Tag = curve.Name;
                    comboBoxItem.Name = curve.Name + "_" + i;
                    comboBoxItem.Content = curve.Name;
                    Curve_ComboBox.Items.Add(comboBoxItem);
                    i++;
                }
            }
            if (previousSelected != -1)
            {
                Curve_ComboBox.SelectedIndex = previousSelected;
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Select_Com.ItemsSource = SerialPort.GetPortNames();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (string.Equals(Connect_button.Content.ToString(), "Connetti"))
            {
#if EMULATE
                WorkingTimer.Interval = ReadingTempTimerRate;
                WorkingTimer.Elapsed += ReadingTempTimer_Elapsed;
                WorkingTimer.Start();

                Connect_button.Content = "Disconnetti";
                Select_Com.IsEnabled = false;
                Start_button.IsEnabled = true;
                OnOff_Button.IsEnabled = true;
                Curve_ComboBox.IsEnabled = false;
                Pause_button.Visibility = Visibility.Visible;
#else
                if (Select_Com.Text != "")
                {
                    if (handlerArduino.Connect(Select_Com.Text))
                    {
                        Thread.Sleep(100);
                        if (handlerArduino.Read_Temp_and_Status(true) != "failed") {
                            WorkingTimer.Interval = ReadingTempTimerRate;
                            WorkingTimer.Elapsed += ReadingTempTimer_Elapsed;
                            WorkingTimer.Start();

                            if (Properties.Settings.Default.SaveLog)
                            {
                                logFileName = Properties.Settings.Default.LogPath + "\\" + DateTime.Now.ToString("yyMMdd_hhmmss") + "_Log.txt";
                                LogTimer.Interval = Properties.Settings.Default.LogRate;
                                LogTimer.Elapsed += LogTimer_Elapsed;
                                LogTimer.Start();
                            }

                            Connect_button.Content = "Disconnetti";
                            Select_Com.IsEnabled = false;
                            Start_button.IsEnabled = true;
                            OnOff_Button.IsEnabled = true;

                            Pause_button.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            MessageBox.Show("La porta selezionata non è quella giusta poichè non ha risposto ai comandi!.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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

                Select_Com.IsEnabled = true;
                OnOff_Button.IsEnabled = false;
                Curve_ComboBox.IsEnabled = true;
                Pause_button.Visibility = Visibility.Hidden;
#else
                if (handlerArduino.Disconnect())
                {
                    Start_button.IsEnabled = false;
                    Connect_button.Content = "Connetti";
                    if (WorkingTimer.Enabled)
                    {
                        WorkingTimer.Stop();
                    }
                    if (OngoingTimer.Enabled)
                    {
                        OngoingTimer.Stop();
                    }
                    if (LogTimer.Enabled)
                    {
                        LogTimer.Stop();
                    }

                    MinutePassed = 0;

                    Custom_Button.IsEnabled = true;
                    Select_Com.IsEnabled = true;
                    OnOff_Button.IsEnabled = false;
                    Pause_button.Visibility = Visibility.Hidden;
                }
                else
                {
                    MessageBox.Show("Non è stato possibile chiudere la connessione con la porta!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
#endif
            }

            Mouse.OverrideCursor = null;
        }

        private void LogTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            using (StreamWriter sw = new StreamWriter(logFileName, true))
            {
                sw.WriteLine(DateTime.Now + "\t" + ReadTemperature +"\t" + switchStatus);
            }
        }

        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if (graphHelper.IdealCurve.Count != 0)
            {
                if (string.Equals(Start_button.Content.ToString(), "Avvia"))
                {
                    OngoingTimer.Interval = Properties.Settings.Default.CheckRate;
                    OngoingTimer.Elapsed += OngoingTimer_Elapsed;
                    OngoingTimer.Start();
                    OngoingTimer_Elapsed(this, null);

                    Start_button.Content = "Ferma";

                    Custom_Button.IsEnabled = false;
                    Mode_button.IsEnabled = false;
                    Pause_button.IsEnabled = true;
                    Curve_ComboBox.IsEnabled = false;
                    Settings_button.IsEnabled = false;
                    Custom_Button.IsEnabled = false;

                    startTime = DateTime.Now;
                    previousPassedMinute = startTime.Minute;
                }
                else
                {
                    Start_button.Content = "Avvia";
                    OngoingTimer.Stop();
                    ReadingChart.Clear();
                    MinutePassed = 0;
                    Custom_Button.IsEnabled = true;
                    Mode_button.IsEnabled = true;
                    Pause_button.IsEnabled = false;
                    Curve_ComboBox.IsEnabled = true;
                    Settings_button.IsEnabled = true;
                    Custom_Button.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("Selezionare prima una curva!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Custom_Button_Click(object sender, RoutedEventArgs e)
        {
            string curveName = (Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
            int selectedIndex = Curve_ComboBox.SelectedIndex;
            CurveCustomization curveCustomization = new CurveCustomization(curveParser, curveName);
            curveCustomization.ShowDialog();

            parseCurvesFiles(selectedIndex);
            Refresh_IdealCurve(curveName);
        }

        void Refresh_IdealCurve(string curveName, bool addNewModifiedCurve = false)
        {
            if (!string.IsNullOrEmpty(curveName))
            {
                Curve usedCurve = curveParser.parseCurveFromName(curveName);
                if (usedCurve != null)
                {
                    graphHelper.graphGeneration(usedCurve, 0);
                    Dispatcher.Invoke(() =>
                    {
                        if (addNewModifiedCurve)
                        {
                            ch.Series = new SeriesCollection
                            {
                                new LineSeries
                                {
                                    Title = "Curva ideale",
                                    Values = graphHelper.IdealCurve.AsChartValues(),
                                    Stroke = Brushes.Blue
                                },
                                new LineSeries
                                {
                                    Title = "Vecchia curva ideale",
                                    Values = graphHelper.oldIdealCurve.AsChartValues(),
                                    Stroke = Brushes.Gray
                                }
                            };
                        }
                        else
                        {
                            ch.Series = new SeriesCollection
                            {
                                new LineSeries
                                {
                                    Title = "Curva ideale",
                                    Values = graphHelper.IdealCurve.AsChartValues(),
                                    Stroke = Brushes.Blue
                                }
                            };
                        }
                        GraphGrid.Children.Clear();
                        GraphGrid.Children.Add(ch);
                    });
                }
            }
        }

        private void ReadingTempTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
#if EMULATE
            ReadTemperature = (int)Math.Floor(myOven.GetTemp());
#else
            string rawString = handlerArduino.Read_Temp_and_Status();
            ReadTemperature = Convert.ToInt32(rawString.Split(';')[0]);
            if (rawString.Split(';')[1].Contains("0"))
                switchStatus = false;
            else
                switchStatus = true;
#endif
            Dispatcher.Invoke(() =>
            {
                TempActual_TextBox.Content = ReadTemperature + " °C";
#if !EMULATE
                if (switchStatus)
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
            bool addPoint = true;

            if (TempBlockIndex < graphHelper.wantedCurve.points.Count - 1)
            {
                int ComplessiveTime = 0;
                if (TempBlockIndex > 0)
                {
                    for (int i = 0; i < TempBlockIndex; i++)
                    {
                        ComplessiveTime += graphHelper.wantedCurve.points[i].TimeValue;
                    }
                }

                if (MinutePassed == (graphHelper.wantedCurve.points[TempBlockIndex].TimeValue + ComplessiveTime))
                {
                    if (graphHelper.IdealCurve[MinutePassed] < graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                    {
                        if (ReadTemperature >= graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                        {
                            TempBlockIndex++;
                            addPoint = false;
                            isTempReached = false;
                        }
                    }
                    else
                    {
                        if (graphHelper.IdealCurve[MinutePassed] > graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                        {
                            if (ReadTemperature <= graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                            {
                                TempBlockIndex++;
                                addPoint = false;
                                isTempReached = false;
                            }
                        }
                        else
                        {
                            addPoint = false;
                            TempBlockIndex++;
                            isTempReached = false;
                        }
                    }

                    if (addPoint && !isTempReached)
                    {
                        AddPointOnCurve();
                    }
                }
                else
                {
                    if (graphHelper.IdealCurve.Count < MinutePassed)
                    {
                        if (graphHelper.IdealCurve[MinutePassed] < graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                        {
                            if (ReadTemperature >= graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                            {
                                isTempReached = true;
                            }
                        }
                        else
                        {
                            if (graphHelper.IdealCurve[MinutePassed] > graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                            {
                                if (ReadTemperature <= graphHelper.wantedCurve.points[TempBlockIndex].TempValue)
                                {
                                    isTempReached = true;
                                }
                            }
                            else
                            {
                                isTempReached = true;
                            }
                        }
                    }
                }
            }

            RegulateTemp();

            var mapper = new CartesianMapper<double>()
                .X((value, index) => MinutePassed)
                .Y((value, index) => value);

            var p = new System.Windows.Point() { X = MinutePassed, Y = ReadTemperature };
            ReadingChart.Add(p);

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {

                LineSeries Point = new LineSeries(mapper)
                {
                    Title = "Valore attuale",
                    Values = new ChartValues<double> { ReadTemperature },
                    Stroke = Brushes.Red,
                    PointGeometrySize = 10,
                    PointForeground = Brushes.Red
                };
                ReadingPoint.Configuration = new CartesianMapper<System.Windows.Point>()
                        .X(p => p.X)
                        .Y(p => p.Y);
                ReadingPoint.Values = ReadingChart;

                if (LastPoint != null)
                {
                    ch.Series.Remove(LastPoint);
                }
                if (LastPointReading != null)
                {
                    ch.Series.Remove(LastPointReading);
                }
                ch.Series.Add(Point);
                ch.Series.Add(ReadingPoint);
                LastPoint = Point;
                LastPointReading = ReadingPoint;

                GraphGrid.Children.Clear();
                GraphGrid.Children.Add(ch);

                int hour = Convert.ToInt32(Math.Floor((double)MinutePassed / 60));
                int minute = MinutePassed - (hour * 60);
                TimeCount_Value_Label.Content = hour.ToString("00") + ":" + minute.ToString("00") + " h";
                int lefthour = Convert.ToInt32(Math.Floor(((double)graphHelper.IdealCurve.Count() - MinutePassed) / 60));
                int leftminute = Convert.ToInt32((double)graphHelper.IdealCurve.Count() - MinutePassed) - (lefthour * 60);
                if (lefthour >= 0 && leftminute >= 0)
                {
                    TimeExpected_Value_Label.Content = lefthour.ToString("00") + ":" + leftminute.ToString("00") + " h";
                }
                else
                {
                    TimeExpected_Value_Label.Content = "--:-- h";
                }
            }));

#if EMULATE
            MinutePassed++;
#else
            if (DateTime.Now.Minute != previousPassedMinute)
            {
                MinutePassed++;
                previousPassedMinute = DateTime.Now.Minute;
            }
#endif
        }

        private void AddPointOnCurve()
        {
            if (graphHelper.oldIdealCurve.Count == 0)
            {
                graphHelper.oldIdealCurve = new List<double>(graphHelper.IdealCurve);
            }

            graphHelper.IdealCurve.Insert(MinutePassed + 1, graphHelper.wantedCurve.points[TempBlockIndex].TempValue);
            graphHelper.wantedCurve.points[TempBlockIndex].TimeValue++;

            Dispatcher.Invoke(() =>
            {
                Refresh_IdealCurve((Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString(), true);
            });
        }

        private void RegulateTemp()
        {
            if (graphHelper.IdealCurve.Count() > MinutePassed)
            {
                if (ReadTemperature < graphHelper.IdealCurve[MinutePassed])
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
            if (string.Equals(Mode_button.Content.ToString(), "Modalità manuale"))
            {
                Start_button.Visibility = Visibility.Hidden;

                OnOff_Button.Visibility = Visibility.Visible;
                Custom_Button.Visibility = Visibility.Hidden;

                Mode_button.Content = "Modalità auto";
            }
            else
            {
                Start_button.Visibility = Visibility.Visible;

                OnOff_Button.Visibility = Visibility.Hidden;
                Custom_Button.Visibility = Visibility.Visible;
                if (string.Equals(Connect_button.Content.ToString(), "Disconnetti"))
                    OnOff_Button_Click(null, e);
                Mode_button.Content = "Modalità manuale";

            }
        }

        private void OnOff_Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                if (string.Equals(OnOff_Button.Content.ToString(), "Accendi"))
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
            else
            {
                if (handlerArduino.writeOutput(0))
                {
                    OnOff_Button.Content = "Accendi";
                }
            }
        }
        private void Pause_button_Click(object sender, RoutedEventArgs e)
        {
            if (string.Equals(Pause_button.Content.ToString(), "Pausa"))
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

        private void Curve_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Curve_ComboBox.SelectedItem != null)
            {
                Refresh_IdealCurve((Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "None")
            {
                Properties.Settings.Default.LastCurve = (Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
            }
            Properties.Settings.Default.Save();
        }

        private void Settings_button_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();

            settings.ShowDialog();

            string curveName = (Curve_ComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
            int selectedIndex = Curve_ComboBox.SelectedIndex;

            parseCurvesFiles(selectedIndex);
            Refresh_IdealCurve(curveName);
        }

        /// <summary>
        /// parser for custom curves files
        /// </summary>
        CustomCurveParse curveParser;

        /// <summary>
        /// read temperature value
        /// </summary>
        int ReadTemperature = 0;

        /// <summary>
        /// Minute passed from the start of monitorization
        /// </summary>
        int MinutePassed;

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
        /// Timer of Ongoing curve
        /// </summary>
        System.Timers.Timer LogTimer = new System.Timers.Timer();

        /// <summary>
        /// Speed at which temperature read is done
        /// </summary>
#if EMULATE
        const int ReadingTempTimerRate = 250;
#else
        const int ReadingTempTimerRate = 2000;
#endif

        /// <summary>
        /// Last point in the graph
        /// </summary>
        LineSeries LastPoint;

        /// <summary>
        /// Handler for Arduino communication
        /// </summary>
        HandlerArduino handlerArduino = new HandlerArduino();

        /// <summary>
        /// Chart of the reading temperature
        /// </summary>
        ChartValues<System.Windows.Point> ReadingChart = new ChartValues<System.Windows.Point>();

        /// <summary>
        /// Point of the reading temperature
        /// </summary>
        LineSeries ReadingPoint = new LineSeries();

        /// <summary>
        /// Lines of the reading temperature
        /// </summary>
        LineSeries LastPointReading;

        /// <summary>
        /// index of ongoing points
        /// </summary>
        int TempBlockIndex = 0;

        /// <summary>
        /// Helper for graph creation
        /// </summary>
        GraphHelper graphHelper;

        /// <summary>
        /// Time of start
        /// </summary>
        DateTime startTime;

        /// <summary>
        /// Last minute that has passed
        /// </summary>
        int previousPassedMinute;


        int tempTime;


        bool switchStatus;

        bool isTempReached;

        string logFileName;
#if EMULATE
        /// <summary>
        /// Simulator for the oven
        /// </summary>
        OvenSim myOven;
#endif

    }
}
