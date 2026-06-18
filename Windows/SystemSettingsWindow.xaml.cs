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
using System.Windows.Shapes;
using GazeStream.AppData;
using Velopack.Locators;
namespace GazeStream.Windows
{
    /// <summary>
    /// Interaction logic for SystemSettingsWindow.xaml
    /// </summary>
    public partial class SystemSettingsWindow : Window
    {
        bool initialized;
        public SystemSettingsWindow()
        {
            InitializeComponent();
            DataContext = Settings.I;            
        }

        private void OnStartupChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                App.AddAppToStartup();
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void OnStartupUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                App.RemoveAppFromStartup();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
    }
}
