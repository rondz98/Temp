using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Temp
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            isInitialized = true;
            InitializeComponent();

            foreach(ComboBoxItem item in BaudRateComboBox.Items)
            {
                if(item.Tag.ToString() == Properties.Settings.Default.BaudRate.ToString())
                {
                    BaudRateComboBox.SelectedItem = item;
                    break;
                }
            }

            rate_TextBox.Text = Properties.Settings.Default.CheckRate.ToString();

            if (Properties.Settings.Default.SaveLog)
            {
                saveLog_CheckBox.IsChecked = true;
            }
            else
            {
                saveLog_CheckBox.IsChecked = false;
            }

            logPath_TextBox.Text = Properties.Settings.Default.LogPath;
            logRate_TextBox.Text = Properties.Settings.Default.LogRate.ToString();

            isInitialized = false;
        }

        private void rate_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized)
            {
                hasChanged = true;
            }
        }

        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized)
            {
                hasChanged = true;
            }
        }

        private void folderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    logPath_TextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Height = 350;
            mainGrid.RowDefinitions[4].Height = new GridLength(40);
            mainGrid.RowDefinitions[5].Height = new GridLength(40);
            if (!isInitialized)
            {
                hasChanged = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Height = 250;
            mainGrid.RowDefinitions[4].Height = new GridLength(0);
            mainGrid.RowDefinitions[5].Height = new GridLength(0);
            if (!isInitialized)
            {
                hasChanged = true;
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BaudRate = Convert.ToInt32(((ComboBoxItem)BaudRateComboBox.SelectedItem).Tag);
            Properties.Settings.Default.CheckRate = Convert.ToInt32(rate_TextBox.Text);

            if(saveLog_CheckBox.IsChecked == true)
            {
                Properties.Settings.Default.SaveLog = true;
            }
            else
            {
                Properties.Settings.Default.SaveLog = false;
            }

            Properties.Settings.Default.LogPath = logPath_TextBox.Text;
            Properties.Settings.Default.LogRate = Convert.ToInt32(logRate_TextBox.Text);

            Properties.Settings.Default.Save();
            saveButton.Content = "Salvato";
            saveButton.IsEnabled = false;
            hasChanged = false;
        }

        private void import_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "File di curva (*.ccw)|*.ccw";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] selectedFiles = openFileDialog.FileNames;
                foreach (string file in selectedFiles)
                {
                    string name = Path.GetFileName(file);
                    File.Move(file, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeDo_Temp_Controller")+ "\\" + name);
                }
            }
        }

        private bool HasChanged;
        private bool isInitialized;

        /// <summary>
        /// Flag for changed values
        /// </summary>
        public bool hasChanged
        {
            get
            {
                return HasChanged;
            }
            set
            {
                if (value)
                {
                    saveButton.Content = "Salva";
                    saveButton.IsEnabled = true;
                }
                HasChanged = value;
            }
        }
    }
}
