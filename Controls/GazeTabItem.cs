using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GazeStream.Controls
{
    public class GazeTabItem : TabItem
    {
        public static readonly DependencyProperty GazeContentProperty =
            DependencyProperty.Register(
                nameof(GazeContent),
                typeof(object),
                typeof(GazeTabItem),
                new PropertyMetadata(null));

        public object GazeContent
        {
            get => GetValue(GazeContentProperty);
            set => SetValue(GazeContentProperty, value);
        }

        static GazeTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GazeTabItem),
                new FrameworkPropertyMetadata(typeof(GazeTabItem)));
        }
    }
}
