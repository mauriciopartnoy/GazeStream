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
    public partial class EnumSelectorUniform : System.Windows.Controls.UserControl
    {
        public EnumSelectorUniform()
        {
            InitializeComponent();
            Uid = Guid.NewGuid().ToString();
        }


        public static readonly DependencyProperty ItemMarginProperty =
    DependencyProperty.Register(
        nameof(ItemMargin),
        typeof(Thickness),
        typeof(EnumSelector),
        new PropertyMetadata(new Thickness(6)));

        public Thickness ItemMargin
        {
            get => (Thickness)GetValue(ItemMarginProperty);
            set => SetValue(ItemMarginProperty, value);
        }

        public static readonly DependencyProperty LayoutModeProperty =
    DependencyProperty.Register(
        nameof(LayoutMode),
        typeof(EnumSelectorLayout),
        typeof(EnumSelector),
        new PropertyMetadata(EnumSelectorLayout.Horizontal));

        public EnumSelectorLayout LayoutMode
        {
            get => (EnumSelectorLayout)GetValue(LayoutModeProperty);
            set => SetValue(LayoutModeProperty, value);
        }

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(
                nameof(SelectedValue),
                typeof(object),
                typeof(EnumSelectorUniform),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedValueChanged));

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
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
                typeof(EnumSelectorUniform),
                new PropertyMetadata(null));

        static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (EnumSelectorUniform)d;

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

    public enum EnumSelectorLayout
    {
        Horizontal,
        Vertical,
        Grid
    }
}
