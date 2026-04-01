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
using GazeStream.AppData;
using GazeStream.ViewModels;

namespace GazeStream.Pages
{
    /// <summary>
    /// Interaction logic for SoundSettingsPage.xaml
    /// </summary>
    public partial class SoundSettingsPage : System.Windows.Controls.UserControl
    {
        public SoundSettingsPage()
        {
            InitializeComponent();
            DataContext = Settings.I;
        }
    }
}
