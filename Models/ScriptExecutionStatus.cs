using System;
using System.ComponentModel;

namespace ScriptTrayTool.Models
{
    public class ScriptExecutionStatus : INotifyPropertyChanged
    {
        private bool _isRunning;
        private string _currentOutput = string.Empty;
        private DateTime? _startTime;
        private Script? _currentScript;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public string CurrentOutput
        {
            get => _currentOutput;
            set
            {
                if (_currentOutput != value)
                {
                    _currentOutput = value;
                    OnPropertyChanged(nameof(CurrentOutput));
                }
            }
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
        }

        public Script? CurrentScript
        {
            get => _currentScript;
            set
            {
                if (_currentScript != value)
                {
                    _currentScript = value;
                    OnPropertyChanged(nameof(CurrentScript));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Reset()
        {
            IsRunning = false;
            CurrentOutput = string.Empty;
            StartTime = null;
            CurrentScript = null;
        }
    }
}
