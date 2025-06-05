using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptTrayTool.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace ScriptTrayTool.ViewModels
{
    public partial class LogViewModel : ObservableObject
    {
        private readonly ScriptRunner _scriptRunner;

        [ObservableProperty]
        private string _logContent = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableDates = new();

        [ObservableProperty]
        private string? _selectedDate;

        [ObservableProperty]
        private bool _isLoading;

        public ICommand RefreshCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ClearAllLogsCommand { get; }

        public LogViewModel(ScriptRunner scriptRunner)
        {
            _scriptRunner = scriptRunner;

            RefreshCommand = new RelayCommand(async () => await RefreshLogAsync());
            ClearLogCommand = new RelayCommand(async () => await ClearLogAsync());
            ClearAllLogsCommand = new RelayCommand(async () => await ClearAllLogsAsync());
        }

        public async Task LoadTodayLogAsync()
        {
            IsLoading = true;
            try
            {
                LogContent = await _scriptRunner.GetTodayLogAsync();
                LoadAvailableDates();
                SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                LogContent = $"加载日志失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshLogAsync()
        {
            if (string.IsNullOrEmpty(SelectedDate))
            {
                await LoadTodayLogAsync();
                return;
            }

            IsLoading = true;
            try
            {
                if (DateTime.TryParse(SelectedDate, out var date))
                {
                    LogContent = await _scriptRunner.GetLogByDateAsync(date);
                }
                else
                {
                    LogContent = await _scriptRunner.GetTodayLogAsync();
                }

                LoadAvailableDates();
            }
            catch (Exception ex)
            {
                LogContent = $"加载日志失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadAvailableDates()
        {
            var dates = _scriptRunner.GetAvailableLogDates();
            AvailableDates.Clear();

            // 添加今天（如果不在列表中）
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (!dates.Contains(today))
            {
                AvailableDates.Add(today);
            }

            foreach (var date in dates)
            {
                AvailableDates.Add(date);
            }
        }

        private async Task ClearLogAsync()
        {
            if (string.IsNullOrEmpty(SelectedDate))
            {
                await ClearTodayLogAsync();
                return;
            }

            var result = MessageBox.Show(
                $"确定要永久删除 {SelectedDate} 的日志吗？此操作无法撤销。",
                "确认删除日志",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    if (DateTime.TryParse(SelectedDate, out var date))
                    {
                        var success = await _scriptRunner.ClearLogByDateAsync(date);
                        if (success)
                        {
                            LogContent = $"{SelectedDate} 的日志已永久删除。";
                            LoadAvailableDates();

                            // If current date was deleted, switch to today
                            if (!AvailableDates.Contains(SelectedDate))
                            {
                                SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                            }
                        }
                        else
                        {
                            LogContent = $"删除 {SelectedDate} 的日志失败，可能文件不存在。";
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogContent = $"删除日志失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task ClearTodayLogAsync()
        {
            var result = MessageBox.Show(
                "确定要永久删除今日的日志吗？此操作无法撤销。",
                "确认删除日志",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    var success = await _scriptRunner.ClearLogByDateAsync(DateTime.Now);
                    if (success)
                    {
                        LogContent = "今日日志已永久删除。";
                        LoadAvailableDates();
                    }
                    else
                    {
                        LogContent = "删除今日日志失败，可能文件不存在。";
                    }
                }
                catch (Exception ex)
                {
                    LogContent = $"删除日志失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task ClearAllLogsAsync()
        {
            var result = MessageBox.Show(
                "确定要永久删除所有日志吗？此操作无法撤销。",
                "确认删除所有日志",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    var success = await _scriptRunner.ClearAllLogsAsync();
                    if (success)
                    {
                        LogContent = "所有日志已永久删除。";
                        AvailableDates.Clear();
                        AvailableDates.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                        SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        LogContent = "删除所有日志失败。";
                    }
                }
                catch (Exception ex)
                {
                    LogContent = $"删除日志失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        partial void OnSelectedDateChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = RefreshLogAsync();
            }
        }
    }
}
