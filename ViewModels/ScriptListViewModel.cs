using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptTrayTool.Models;
using ScriptTrayTool.Services;
using ScriptTrayTool.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScriptTrayTool.ViewModels
{
    public partial class ScriptListViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly ScriptRunner _scriptRunner;
        private ScriptOutputWindow? _outputWindow;

        [ObservableProperty]
        private ObservableCollection<Script> _scripts = new();

        [ObservableProperty]
        private Script? _selectedScript;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isAnyScriptRunning;

        [ObservableProperty]
        private Script? _currentRunningScript;

        public ICommand AddScriptCommand { get; }
        public ICommand EditScriptCommand { get; }
        public ICommand DeleteScriptCommand { get; }
        public ICommand ExecuteScriptCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowOutputWindowCommand { get; }

        public ScriptListViewModel(DatabaseService databaseService, ScriptRunner scriptRunner)
        {
            _databaseService = databaseService;
            _scriptRunner = scriptRunner;

            AddScriptCommand = new RelayCommand(AddScript);
            EditScriptCommand = new RelayCommand<Script>(EditScript);
            DeleteScriptCommand = new RelayCommand<Script>(DeleteScript);
            ExecuteScriptCommand = new RelayCommand<Script>(ExecuteScript, CanExecuteScript);
            RefreshCommand = new RelayCommand(async () => await RefreshScriptsAsync());
            ShowOutputWindowCommand = new RelayCommand(ShowOutputWindow);

            // Subscribe to execution status changes
            _scriptRunner.ExecutionStatusChanged += OnExecutionStatusChanged;
        }

        public async Task LoadScriptsAsync()
        {
            IsLoading = true;
            try
            {
                var scripts = await _databaseService.GetAllScriptsAsync();
                Scripts.Clear();
                foreach (var script in scripts)
                {
                    Scripts.Add(script);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载脚本列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshScriptsAsync()
        {
            await LoadScriptsAsync();
        }

        private void AddScript()
        {
            var editorWindow = new Views.ScriptEditorWindow(_databaseService);
            if (editorWindow.ShowDialog() == true)
            {
                _ = RefreshScriptsAsync();
            }
        }

        private void EditScript(Script? script)
        {
            script ??= SelectedScript;
            if (script == null) return;

            var editorWindow = new Views.ScriptEditorWindow(_databaseService, script);
            if (editorWindow.ShowDialog() == true)
            {
                _ = RefreshScriptsAsync();
            }
        }

        private async void DeleteScript(Script? script)
        {
            script ??= SelectedScript;
            if (script == null) return;

            var result = MessageBox.Show(
                $"确定要删除脚本 '{script.Name}' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _databaseService.DeleteScriptAsync(script.Id);
                    await RefreshScriptsAsync();
                    MessageBox.Show("脚本删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除脚本失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanExecuteScript(Script? script)
        {
            return !IsAnyScriptRunning && !IsLoading;
        }

        private void OnExecutionStatusChanged(object? sender, ScriptExecutionStatus status)
        {
            IsAnyScriptRunning = status.IsRunning;
            CurrentRunningScript = status.CurrentScript;

            // Update command can execute status
            ((RelayCommand<Script>)ExecuteScriptCommand).NotifyCanExecuteChanged();
        }

        private void ShowOutputWindow()
        {
            // Check if window is null, closed, or not visible
            if (_outputWindow == null || !IsWindowUsable(_outputWindow))
            {
                var outputViewModel = new ScriptOutputViewModel(_scriptRunner);

                // Sync current execution status if script is running
                outputViewModel.SyncCurrentStatus();

                _outputWindow = new ScriptOutputWindow(outputViewModel);
                _outputWindow.Closed += (s, e) => _outputWindow = null;
            }

            _outputWindow.Show();
            _outputWindow.Activate();
        }

        private static bool IsWindowUsable(Window? window)
        {
            if (window == null) return false;

            try
            {
                // Check if window is still valid by accessing its handle
                // This will throw an exception if the window is closed
                var hwnd = new WindowInteropHelper(window).Handle;
                return window.IsLoaded && hwnd != IntPtr.Zero;
            }
            catch
            {
                // If any exception occurs, consider window unusable
                return false;
            }
        }

        private async void ExecuteScript(Script? script)
        {
            script ??= SelectedScript;
            if (script == null) return;

            try
            {
                IsLoading = true;

                // Show output window if not already visible
                ShowOutputWindow();

                var result = await _scriptRunner.ExecuteScriptAsync(script);

                // 更新最后执行时间
                await _databaseService.UpdateScriptLastExecutedAsync(script.Id, DateTime.Now);
                await RefreshScriptsAsync();

                // 显示执行结果
                var message = result.Success
                    ? $"脚本执行成功！\n\n耗时: {result.Duration.TotalSeconds:F2} 秒\n退出代码: {result.ExitCode}"
                    : $"脚本执行失败！\n\n错误信息: {result.Error}\n退出代码: {result.ExitCode}";

                var icon = result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning;
                MessageBox.Show(message, "执行结果", MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行脚本时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }


    }
}
