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

namespace GazeStream.Controls
{
    public partial class EnumSelector : System.Windows.Controls.UserControl
    {
        public EnumSelector()
        {
            InitializeComponent();
            Uid = Guid.NewGuid().ToString();
        }

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(
                nameof(SelectedValue),
                typeof(Enum),
                typeof(EnumSelector),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedValueChanged));

        public Enum SelectedValue
        {
            get => (Enum)GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        public Array EnumValues
        {
            get => (Array)GetValue(EnumValuesProperty);
            set => SetValue(EnumValuesProperty, value);
        }

        public static readonly DependencyProperty EnumValuesProperty =
            DependencyProperty.Register(
                nameof(EnumValues),
                typeof(Array),
                typeof(EnumSelector),
                new PropertyMetadata(null));

        static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (EnumSelector)d;

            if (e.NewValue == null)
                return;

            // Only generate enum values the first time
            if (control.EnumValues == null)
            {
                var enumType = e.NewValue.GetType();
                control.EnumValues = Enum.GetValues(enumType);
            }
        }

        private void RadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            var rb = (System.Windows.Controls.RadioButton)sender;

            if (SelectedValue != null && rb.Tag.Equals(SelectedValue))
                rb.IsChecked = true;
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var rb = (System.Windows.Controls.RadioButton)sender;
            if (rb.Tag is Enum enumValue && !Equals(enumValue, SelectedValue))
                SelectedValue = enumValue;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
    where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
