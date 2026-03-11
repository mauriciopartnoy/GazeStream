using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using M = System.Windows.Media;
using GazeStream.AppData;

namespace GazeStream.ViewModels
{
    public class InteractionSettingsViewModel : BaseViewModel
    {
        public bool BubbleToggle
        {
            get => Settings.I.BubbleToggle.Value;
            set
            {
                if (Settings.I.BubbleToggle.Value == value)
                    return;

                Settings.I.BubbleToggle.Value = value;
                OnPropertyChanged();
            }
        }
        public float InicioDeActivacion
        {
            get => Settings.I.TiempoDeInicioDeActivacion.Value;
            set
            {
                if (Settings.I.TiempoDeInicioDeActivacion.Value == value)
                    return;

                Settings.I.TiempoDeInicioDeActivacion.Value = value;
                OnPropertyChanged();
            }
        }

        public float TiempoDeActivacion
        {
            get => Settings.I.TiempoDeActivacion.Value;
            set
            {
                if (Settings.I.TiempoDeActivacion.Value == value)
                    return;

                Settings.I.TiempoDeActivacion.Value = value;
                OnPropertyChanged();
            }
        }

        public float TiempoDeDesactivacion
        {
            get => Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value;
            set
            {
                if (Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value == value)
                    return;

                Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value = value;
                OnPropertyChanged();
            }
        }

        public float Permanencia
        {
            get => Settings.I.PermanenciaDeFijacionesIncompletas.Value;
            set
            {
                if (Settings.I.PermanenciaDeFijacionesIncompletas.Value == value)
                    return;

                Settings.I.PermanenciaDeFijacionesIncompletas.Value = value;
                OnPropertyChanged();
            }
        }

        public Array AnimationTypes => Enum.GetValues(typeof(Button_Animation));
        public Button_Animation AnimationType
        {
            get => Settings.I.ButtonAnimationType.Value;
            set
            {
                if (Settings.I.ButtonAnimationType.Value == value)
                    return;

                Settings.I.ButtonAnimationType.Value = value;
                OnPropertyChanged();
            }
        }

        public Array AnimationColors => Enum.GetValues(typeof(BasicColor));
        public BasicColor AnimationColor
        {
            get => Settings.I.ButtonAnimationColor.Value;
            set
            {
                if (Settings.I.ButtonAnimationColor.Value == value)
                    return;

                Settings.I.ButtonAnimationColor.Value = value;
                OnPropertyChanged();
            }
        }
    }
}
