using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GazeStream.ViewModels
{
    public class UpdateViewModel : INotifyPropertyChanged
    {
        private int progress;
        private string status = "Chequeando actualizaciones...";

        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        public string CurrentVersion { get; set; } = "";
        public string NewVersion { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
