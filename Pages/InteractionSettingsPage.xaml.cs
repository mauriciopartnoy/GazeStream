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
using GazeStream.ViewModels;
using GazeStream.AppData;
namespace GazeStream.Pages
{ 
    public partial class InteractionSettingsPage : System.Windows.Controls.UserControl
    {
        public InteractionSettingsPage()
        {
            InitializeComponent();
            //DataContext = ViewModelsLocator.InteractionSettingsViewModel;
            DataContext = Settings.I;
        }

        private void GazeButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Activated by gaze!");
        }
    }
}
