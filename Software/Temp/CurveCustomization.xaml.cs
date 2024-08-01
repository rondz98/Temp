using System;
using System.Windows;
using System.Windows.Controls;
using Temp.Entities;
using Temp.Helpers;
using Point = Temp.Entities.Point;

namespace Temp
{
    /// <summary>
    /// Interaction logic for CurveCustomization.xaml
    /// </summary>
    public partial class CurveCustomization : Window
    {
        public CurveCustomization(CustomCurveParse curveParser, string curveName)
        {
            InitializeComponent();

            Curve SelectedCurve = curveParser.parseCurveFromName(curveName);
            CurveName = curveName;
            CurveParser = curveParser;
            if (SelectedCurve != null) { 
                int index = 0;
                foreach(Point point in SelectedCurve.points)
                {
                    steps_grid.Items.Add(add_customRow(index,point));
                    index++;
                }
            }
            hasChanged = false;
            Save_Button.Content = "Salvato";
            Save_Button.IsEnabled = false;
            if (string.IsNullOrEmpty(CurveName) || CurveName == "None")
            {
                int index = 1;
                bool isOk = false;
                while (!isOk)
                {
                    CurveName = "Curva_" + index.ToString("00");
                    if (curveParser.findCurveFile(CurveName) == "")
                    {
                        isOk = true;
                    }
                    else
                    {
                        index++;
                    }
                }
            }
            FileName_TextBox.Text = CurveName;
        }

        private Grid add_customRow(int index, Point point)
        {
            Grid grid = new Grid();
            grid.Name = "Curve_point_" + index;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;

            ColumnDefinition column = new ColumnDefinition();
            column.Width = new GridLength(35);

            ColumnDefinition column2 = new ColumnDefinition();
            column2.Width = new GridLength(100);

            ColumnDefinition column3 = new ColumnDefinition();
            column3.Width = new GridLength(100);

            ColumnDefinition column4 = new ColumnDefinition();
            column4.Width = new GridLength(100);

            grid.ColumnDefinitions.Add(column);
            grid.ColumnDefinitions.Add(column2);
            grid.ColumnDefinitions.Add(column3);
            grid.ColumnDefinitions.Add(column4);

            Label LabelId = new Label();
            LabelId.Name = "LabelId_" + index;
            LabelId.Margin = new Thickness(2, 2, 2, 2);
            LabelId.HorizontalAlignment = HorizontalAlignment.Center;
            LabelId.VerticalAlignment = VerticalAlignment.Center;
            LabelId.VerticalContentAlignment = VerticalAlignment.Center;
            LabelId.Content = index + 1;
            Grid.SetColumn(LabelId, 0);
            grid.Children.Add(LabelId);

            TextBox textBoxTemp = new TextBox();
            textBoxTemp.Name = "textBoxTemp_" + index;
            textBoxTemp.Margin = new Thickness(2, 2, 2, 2);
            textBoxTemp.HorizontalAlignment = HorizontalAlignment.Center;
            textBoxTemp.VerticalAlignment = VerticalAlignment.Center;
            textBoxTemp.VerticalContentAlignment = VerticalAlignment.Center;
            textBoxTemp.HorizontalContentAlignment = HorizontalAlignment.Center;
            textBoxTemp.Text = point.TempValue.ToString();
            textBoxTemp.TextChanged += TextBox_TextChanged;
            textBoxTemp.Width = 50;
            textBoxTemp.Height = 30;
            Grid.SetColumn(textBoxTemp, 1);
            grid.Children.Add(textBoxTemp);

            TextBox textBoxTime = new TextBox();
            textBoxTime.Name = "textBoxTime_" + index;
            textBoxTime.Margin = new Thickness(2, 2, 2, 2);
            textBoxTime.HorizontalAlignment = HorizontalAlignment.Center;
            textBoxTime.VerticalAlignment = VerticalAlignment.Center;
            textBoxTime.VerticalContentAlignment = VerticalAlignment.Center;
            textBoxTime.HorizontalContentAlignment = HorizontalAlignment.Center;
            textBoxTime.Text = point.TimeValue.ToString();
            textBoxTime.TextChanged += TextBox_TextChanged;
            textBoxTime.Width = 50;
            textBoxTime.Height = 30;
            Grid.SetColumn(textBoxTime, 2);
            grid.Children.Add(textBoxTime);

            Button CancelButton = new Button();
            CancelButton.Name = "CancelButton_" + index;
            CancelButton.Margin = new Thickness(2, 2, 2, 2);
            CancelButton.HorizontalAlignment = HorizontalAlignment.Center;
            CancelButton.VerticalAlignment = VerticalAlignment.Center;
            CancelButton.Content = "Cancella";
            CancelButton.Height = 30;
            CancelButton.Width = 70;
            CancelButton.Click += CancelButton_Click;
            grid.Children.Add(CancelButton);
            Grid.SetColumn(CancelButton, 3);

            return grid;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            hasChanged = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            foreach (Grid item in steps_grid.Items)
            {
                if (item.Name == "Curve_point_" + ((FrameworkElement)sender).Name.Split("_")[1])
                {
                    steps_grid.Items.RemoveAt(i);
                    break;
                }
                i++;
            }
            i = 1;
            foreach (Grid item in steps_grid.Items)
            {
                (item.Children[0] as Label).Content = i;
                i++;
            }

            hasChanged = true;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Point point = new Point();
            point.TempValue = 0;
            point.TimeValue = 0;
            point.ID = steps_grid.Items.Count;
            steps_grid.Items.Add(add_customRow(point.ID, point));
            hasChanged = true;
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            if(Save_Button.Content.ToString().Contains("Salva"))
            {
                if (FileName_TextBox.Text != CurveName)
                {
                    MessageBoxResult result = MessageBox.Show("Sicuro di voler cambiare il nome della curva?","Attenzione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if(result == MessageBoxResult.Yes)
                    {
                        CurveParser.RenameFile(CurveName, FileName_TextBox.Text);
                    }
                }
                Curve curve = new Curve();
                curve.Name = CurveName;

                foreach(Grid item in steps_grid.Items)
                {
                    Point point = new Point();
                    point.ID = Convert.ToInt32((item.Children[0] as Label).Content) - 1;
                    point.TempValue = Convert.ToInt32((item.Children[1] as TextBox).Text);
                    point.TimeValue = Convert.ToInt32((item.Children[2] as TextBox).Text);
                    curve.points.Add(point);
                }

                CurveParser.SaveCurve(curve);

                Save_Button.Content = "Salvato";
                Save_Button.IsEnabled = false;
                hasChanged = false;
            }
        }

        /// <summary>
        /// Curve name from MainWindow
        /// </summary>
        string CurveName;

        /// <summary>
        /// Curve parser from MainWindow
        /// </summary>
        CustomCurveParse CurveParser;

        private bool HasChanged;

        /// <summary>
        /// Flag for changed values
        /// </summary>
        public bool hasChanged { 
            get { 
                return HasChanged;
            } 
            set
            {
                if(value)
                {
                    Save_Button.Content = "Salva";
                    Save_Button.IsEnabled = true;
                }
                HasChanged = value;
            }
        }
    }
}
