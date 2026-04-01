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

namespace GazeStream.Windows
{
    /// <summary>
    /// Interaction logic for OptionsMenuWindow.xaml
    /// </summary>
    public partial class OptionsMenuWindow : Window
    {
        public OptionsMenuWindow()
        {
            InitializeComponent();
        }

        void Close_Click(object sender, RoutedEventArgs args)
        {
            this.Close();
        }
    }
}
