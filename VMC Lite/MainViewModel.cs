using System.ComponentModel;

namespace VMC_Lite
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string? _currentAngle;
        private int _leftAngle;
        private int _rightAngle;
        private int _accelerateProgress;
        private int _brakeProgress;
        private int _clutchProgress;
        public int ClutchProgressValue
        {
            get => _clutchProgress;
            set
            {
                if (_clutchProgress != value)
                {
                    _clutchProgress = value;
                    OnPropertyChanged(nameof(ClutchProgressValue));
                }
            }
        }
        public int BrakeProgressValue
        {
            get => _brakeProgress;
            set
            {
                if (_brakeProgress != value)
                {
                    _brakeProgress = value;
                    OnPropertyChanged(nameof(BrakeProgressValue));
                }
            }
        }
        public int AccelerateProgressValue
        {
            get => _accelerateProgress;
            set
            {
                if (_accelerateProgress != value)
                {
                    _accelerateProgress = value;
                    OnPropertyChanged(nameof(AccelerateProgressValue));
                }
            }
        }
        public string CurrentAngle
        {
            get => _currentAngle;
            set
            {
                if (_currentAngle != value)
                {
                    _currentAngle = value;
                    OnPropertyChanged(nameof(CurrentAngle));
                }
            }
        }

        public int LeftAngle
        {
            get => _leftAngle;
            set
            {
                if (_leftAngle != value)
                {
                    _leftAngle = value;
                    OnPropertyChanged(nameof(LeftAngle));
                }
            }
        }

        public int RightAngle
        {
            get => _rightAngle;
            set
            {
                if (_rightAngle != value)
                {
                    _rightAngle = value;
                    OnPropertyChanged(nameof(RightAngle));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
