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
using GazeStream.ViewModels;

namespace GazeStream.Windows
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        public static UpdateWindow? Instance { get; private set; }

        public UpdateViewModel ViewModel { get; }

        public UpdateWindow()
        {
            InitializeComponent();

            ViewModel = new UpdateViewModel();
            DataContext = ViewModel;

            Instance = this;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;

            base.OnClosed(e);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            App.CancelUpdate();
        }
    }
}
