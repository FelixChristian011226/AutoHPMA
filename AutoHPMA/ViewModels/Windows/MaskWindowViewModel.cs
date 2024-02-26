using System.ComponentModel;

namespace AutoHPMA.ViewModels.Windows
{
    public class MaskWindowViewModel : INotifyPropertyChanged
    {
        private string _log;

        public string Log
        {
            get { return _log; }
            set
            {
                if (_log != value)
                {
                    _log = value;
                    OnPropertyChanged(nameof(Log));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
