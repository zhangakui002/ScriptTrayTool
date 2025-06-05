using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptTrayTool.Models;
using ScriptTrayTool.Services;
using System;
using System.Windows.Input;

namespace ScriptTrayTool.ViewModels
{
    public partial class ScriptOutputViewModel : ObservableObject, IDisposable
    {
        private readonly ScriptRunner _scriptRunner;

        [ObservableProperty]
        private string _output = string.Empty;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private Script? _currentScript;

        [ObservableProperty]
        private DateTime? _startTime;

        [ObservableProperty]
        private bool _autoScroll = true;

        public ICommand ClearOutputCommand { get; }
        public ICommand ToggleAutoScrollCommand { get; }
        public ICommand CopyOutputCommand { get; }

        public ScriptOutputViewModel(ScriptRunner scriptRunner)
        {
            _scriptRunner = scriptRunner;

            ClearOutputCommand = new RelayCommand(ClearOutput);
            ToggleAutoScrollCommand = new RelayCommand(ToggleAutoScroll);
            CopyOutputCommand = new RelayCommand(CopyOutput);

            // Subscribe to script runner events
            _scriptRunner.ExecutionStatusChanged += OnExecutionStatusChanged;
            _scriptRunner.OutputReceived += OnOutputReceived;
        }

        /// <summary>
        /// Synchronize current execution status when window is reopened
        /// </summary>
        public void SyncCurrentStatus()
        {
            var currentStatus = _scriptRunner.ExecutionStatus;

            // Update current status
            IsRunning = currentStatus.IsRunning;
            CurrentScript = currentStatus.CurrentScript;
            StartTime = currentStatus.StartTime;

            if (currentStatus.IsRunning && currentStatus.CurrentScript != null)
            {
                // Restore output header for running script
                Output = $"=== 开始执行脚本: {currentStatus.CurrentScript.Name} ===\n开始时间: {currentStatus.StartTime:yyyy-MM-dd HH:mm:ss}\n\n";

                // Add current output if available
                if (!string.IsNullOrEmpty(currentStatus.CurrentOutput))
                {
                    Output += currentStatus.CurrentOutput;
                }

                // Add a note about reconnection
                Output += "\n[窗口重新连接到正在执行的脚本]\n";
            }
        }

        private void OnExecutionStatusChanged(object? sender, ScriptExecutionStatus status)
        {
            IsRunning = status.IsRunning;
            CurrentScript = status.CurrentScript;
            StartTime = status.StartTime;

            if (!status.IsRunning)
            {
                // Execution finished
                Output += $"\n\n=== 脚本执行完成 ===\n执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            }
            else
            {
                // Execution started
                Output = $"=== 开始执行脚本: {status.CurrentScript?.Name} ===\n开始时间: {status.StartTime:yyyy-MM-dd HH:mm:ss}\n\n";
            }
        }

        private void OnOutputReceived(object? sender, string outputLine)
        {
            if (!string.IsNullOrEmpty(outputLine))
            {
                Output += outputLine + "\n";
            }
        }

        private void ClearOutput()
        {
            Output = string.Empty;
        }

        private void ToggleAutoScroll()
        {
            AutoScroll = !AutoScroll;
        }

        private void CopyOutput()
        {
            if (!string.IsNullOrEmpty(Output))
            {
                System.Windows.Clipboard.SetText(Output);
            }
        }

        public string ElapsedTime
        {
            get
            {
                if (StartTime.HasValue && IsRunning)
                {
                    var elapsed = DateTime.Now - StartTime.Value;
                    return $"{elapsed.TotalSeconds:F1}s";
                }
                return "0s";
            }
        }

        public void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            _scriptRunner.ExecutionStatusChanged -= OnExecutionStatusChanged;
            _scriptRunner.OutputReceived -= OnOutputReceived;
        }
    }
}
