using ScriptTrayTool.Services;
using ScriptTrayTool.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptTrayTool
{
    public partial class App : Application
    {
        private TrayIconService? _trayIconService;
        private SingleInstanceManager? _singleInstanceManager;

        public TrayIconService? TrayIconService => _trayIconService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Initialize single instance manager first
            _singleInstanceManager = new SingleInstanceManager();

            // Check if this is the first instance
            if (!_singleInstanceManager.TryAcquireInstance())
            {
                // Another instance is already running, notify it and exit
                await _singleInstanceManager.NotifyExistingInstanceAsync(e.Args);
                _singleInstanceManager.Dispose();
                Shutdown();
                return;
            }

            // Subscribe to activation requests from other instances
            _singleInstanceManager.ActivationRequested += OnActivationRequested;

            base.OnStartup(e);

            try
            {
                // 初始化服务
                var databaseService = new DatabaseService();
                var scriptRunner = new ScriptRunner();
                var trayIconService = new TrayIconService(databaseService, scriptRunner);

                _trayIconService = trayIconService;

                // 创建主窗口和 ViewModel
                var mainViewModel = new MainViewModel(databaseService, scriptRunner, trayIconService);
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // 初始化托盘服务
                trayIconService.Initialize(mainWindow);
                trayIconService.ShowMainWindowRequested += (s, e) => trayIconService.ShowMainWindow();

                // 初始化数据
                await mainViewModel.InitializeAsync();

                // 显示主窗口
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void OnActivationRequested(object? sender, EventArgs e)
        {
            // Bring the main window to foreground when another instance tries to start
            if (MainWindow != null)
            {
                SingleInstanceManager.BringWindowToForeground(MainWindow);
            }
            else if (_trayIconService != null)
            {
                // If main window is not available, show it through tray service
                _trayIconService.ShowMainWindow();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Force close the main window to prevent it from canceling the shutdown
            if (MainWindow is MainWindow mainWindow)
            {
                mainWindow.ForceClose();
            }

            _singleInstanceManager?.Dispose();
            _trayIconService?.Dispose();
            base.OnExit(e);
        }
    }
}
