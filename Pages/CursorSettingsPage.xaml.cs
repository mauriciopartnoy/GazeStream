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
using GazeStream.Eyetracker;
using M = Microsoft.Win32;
using GazeStream.AppData;

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
            DataContext = Settings.I;
        }

        //public ImageSource CursorImage
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(Settings.I.CustomCursorPath.Value))
        //            return null;

        //        return new BitmapImage(new Uri(CursorImagePath));
        //    }
        //}

        void OpenCursorDialog(object sender, RoutedEventArgs args)
        {
            SelectCursorImage();
        }

        private void SelectCursorImage()
        {
            var dialog = new M.OpenFileDialog();

            dialog.Title = "Select Cursor Image";
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == true)
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.UriSource = new Uri(dialog.FileName);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                //CursorPreview.Source = image;
            }
        }
    }
}
