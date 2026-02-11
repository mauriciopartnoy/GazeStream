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
    }
}
