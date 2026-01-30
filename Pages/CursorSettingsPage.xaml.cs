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

namespace GazeStream.Pages
{
    /// <summary>
    /// Interaction logic for CursorSettingsPage.xaml
    /// </summary>
    public partial class CursorSettingsPage : System.Windows.Controls.UserControl
    {
        public CursorSettingsPage()
        {
            InitializeComponent();
            DataContext = new CursorSettingsViewModel();
        }

    }
}
