using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptTrayTool.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScriptTrayTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly ScriptRunner _scriptRunner;
        private readonly TrayIconService _trayIconService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _selectedMenuItem = "Scripts";

        public ScriptListViewModel ScriptListViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public ICommand NavigateToScriptsCommand { get; }
        public ICommand NavigateToLogsCommand { get; }

        public MainViewModel(
            DatabaseService databaseService,
            ScriptRunner scriptRunner,
            TrayIconService trayIconService)
        {
            _databaseService = databaseService;
            _scriptRunner = scriptRunner;
            _trayIconService = trayIconService;

            // 初始化子 ViewModels
            ScriptListViewModel = new ScriptListViewModel(_databaseService, _scriptRunner);
            LogViewModel = new LogViewModel(_scriptRunner);

            // 初始化命令
            NavigateToScriptsCommand = new RelayCommand(() => NavigateToView("Scripts"));
            NavigateToLogsCommand = new RelayCommand(() => NavigateToView("Logs"));

            // 设置默认视图
            CurrentView = ScriptListViewModel;

            // 订阅托盘服务事件
            _trayIconService.ScriptExecutionRequested += OnScriptExecutionRequested;
        }

        private void NavigateToView(string viewName)
        {
            SelectedMenuItem = viewName;

            CurrentView = viewName switch
            {
                "Scripts" => ScriptListViewModel,
                "Logs" => LogViewModel,
                _ => ScriptListViewModel
            };
        }

        private async void OnScriptExecutionRequested(object? sender, Models.Script script)
        {
            // 刷新脚本列表以更新最后执行时间
            await ScriptListViewModel.RefreshScriptsAsync();

            // 刷新日志视图
            await LogViewModel.RefreshLogAsync();
        }

        public async Task InitializeAsync()
        {
            await _databaseService.InitializeDatabaseAsync();
            await ScriptListViewModel.LoadScriptsAsync();
            await LogViewModel.LoadTodayLogAsync();
        }
    }
}
