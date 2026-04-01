using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GazeStream.AppData;
using System.Collections.ObjectModel;
using M = System.Windows.Media;
using GazeStream.Pages;

namespace GazeStream.ViewModels
{
    public class CursorSettingsViewModel : BaseViewModel
    {
        public ObservableCollection<ColorOption> AvailableColors { get; }

        public CursorSettingsViewModel()
        {
            //Settings.I.BubbleColor.OnChanged += OnColorChanged;
            AvailableColors = new ObservableCollection<ColorOption>
            {
                new ColorOption("Rojo", M.Brushes.Red),
                new ColorOption("Verde", M.Brushes.Lime),
                new ColorOption("Azul", M.Brushes.DodgerBlue),
                new ColorOption("Amarillo", M.Brushes.Yellow),
                new ColorOption("Cyan", M.Brushes.Cyan),
                new ColorOption("Magenta", M.Brushes.Magenta),
                new ColorOption("Púrpura", M.Brushes.Purple),
                new ColorOption("Blanco", M.Brushes.White),
                new ColorOption("Negro", M.Brushes.Black),
            };
        }

        //public M.Brush Color
        //{
        //    get =>  AvailableColors[SelectedColorIndex].Color;
        //}
        //public int SelectedColorIndex
        //{
        //    get => (int)Settings.I.BubbleColor.Value;
        //    set
        //    {
        //        if ((int)Settings.I.BubbleColor.Value == value) return;
        //        Settings.I.BubbleColor.Value = value;
        //        OnPropertyChanged();
        //        OnPropertyChanged(nameof(Color));

        //    }
        //}

        //public int Size
        //{
        //    get => Settings.I.BubbleSize.Value;
        //    set
        //    {
        //        if (Settings.I.BubbleSize.Value == value)
        //            return;

        //        Settings.I.BubbleSize.Value = value;
        //        OnPropertyChanged();
        //    }
        //}

        public double Opacity
        {
            get => Settings.I.BubbleOpacity.Value;
            set
            {
                if (Settings.I.BubbleOpacity.Value == value)
                    return;

                Settings.I.BubbleOpacity.Value = value;
                OnPropertyChanged();
            }
        }


        //private void OnColorChanged()
        //{
        //    OnPropertyChanged(nameof(SelectedColorIndex));
        //    OnPropertyChanged(nameof(Color));
        //}

        public CursorVisualStyle CursorType
        {
            get => Settings.I.CursorStyle.Value;
            set
            {
                if (Settings.I.CursorStyle.Value == value) return;
                Settings.I.CursorStyle.Value = value;
                OnPropertyChanged();
            }
        }
    }

    public class ColorOption
    {
        public string Name { get; }
        public M.Brush Color { get; }

        public ColorOption(string name, M.Brush color)
        {
            Name = name;
            Color = color;

        }
    }
}
