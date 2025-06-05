using ScriptTrayTool.Services;
using ScriptTrayTool.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace ScriptTrayTool
{
    public partial class MainWindow : Window
    {
        private WindowState _lastWindowState = WindowState.Normal;
        private bool _isClosingToTray = true;

        public MainWindow()
        {
            InitializeComponent();

            // Store the initial window state
            _lastWindowState = WindowState;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isClosingToTray)
            {
                // 阻止窗口关闭，改为隐藏到托盘
                e.Cancel = true;
                Hide();

                // 移除烦人的托盘提示 - 用户已经知道程序在托盘中了
                // 如果需要提示，可以只在第一次最小化时显示
            }
            else
            {
                // Allow actual closing (when application is shutting down)
                base.OnClosing(e);
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            // Store the window state before minimizing
            if (WindowState != WindowState.Minimized)
            {
                _lastWindowState = WindowState;
            }

            if (WindowState == WindowState.Minimized)
            {
                // Use Dispatcher to ensure UI thread safety
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Hide();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }

            base.OnStateChanged(e);
        }

        public void RestoreWindow()
        {
            // Ensure window is properly restored from tray
            try
            {
                // First make sure window is visible
                if (!IsVisible)
                {
                    Show();
                }

                // Force window to normal state first to avoid rendering issues
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                // Then restore to the desired state
                WindowState = _lastWindowState;

                // Force a refresh to prevent black screen
                InvalidateVisual();
                UpdateLayout();

                // Activate and bring to front
                Activate();
                Focus();

                // Additional fallback for stubborn cases
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Topmost = true;
                    Topmost = false;
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring window: {ex.Message}");

                // Fallback: just show and activate
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
        }

        public void ForceClose()
        {
            // Allow the window to actually close (for application shutdown)
            _isClosingToTray = false;
            Close();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Ensure window is properly visible when activated
            if (WindowState == WindowState.Minimized)
            {
                WindowState = _lastWindowState;
            }

            // Force a visual refresh to prevent black screen
            InvalidateVisual();
        }
    }
}
