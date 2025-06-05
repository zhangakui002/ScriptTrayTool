using ScriptTrayTool.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace ScriptTrayTool.Services
{
    public class TrayIconService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly DatabaseService _databaseService;
        private readonly ScriptRunner _scriptRunner;
        private Window? _mainWindow;

        public event EventHandler<Script>? ScriptExecutionRequested;
        public event EventHandler? ShowMainWindowRequested;

        public TrayIconService(DatabaseService databaseService, ScriptRunner scriptRunner)
        {
            _databaseService = databaseService;
            _scriptRunner = scriptRunner;
        }

        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            CreateNotifyIcon();
        }

        private void CreateNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateDefaultIcon(),
                Text = "ScriptTrayTool - 脚本托盘助手",
                Visible = true
            };

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            _notifyIcon.MouseClick += OnNotifyIconMouseClick;

            // 创建右键菜单
            UpdateContextMenu();
        }

        private Icon CreateDefaultIcon()
        {
            // 创建一个简单的默认图标
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Blue);
                graphics.FillEllipse(Brushes.White, 2, 2, 12, 12);
                graphics.DrawString("S", new Font("Arial", 8, System.Drawing.FontStyle.Bold), Brushes.Blue, 4, 2);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public async void UpdateContextMenu()
        {
            if (_notifyIcon == null) return;

            var contextMenu = new ContextMenuStrip();

            try
            {
                // 获取所有脚本
                var scripts = await _databaseService.GetAllScriptsAsync();

                if (scripts.Any())
                {
                    // 添加脚本菜单项
                    foreach (var script in scripts)
                    {
                        var menuItem = new ToolStripMenuItem(script.Name)
                        {
                            Tag = script
                        };
                        menuItem.Click += OnScriptMenuItemClick;
                        contextMenu.Items.Add(menuItem);
                    }

                    contextMenu.Items.Add(new ToolStripSeparator());
                }

                // 添加固定菜单项
                var showMainWindowItem = new ToolStripMenuItem("显示主界面");
                showMainWindowItem.Click += OnShowMainWindowClick;
                contextMenu.Items.Add(showMainWindowItem);

                var exitItem = new ToolStripMenuItem("退出");
                exitItem.Click += OnExitClick;
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
            }
            catch (Exception ex)
            {
                ShowBalloonTip("错误", $"更新菜单失败: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private async void OnScriptMenuItemClick(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is Script script)
            {
                try
                {
                    // Check if another script is running
                    if (_scriptRunner.ExecutionStatus.IsRunning)
                    {
                        ShowBalloonTip("执行被阻止",
                            $"无法执行脚本 '{script.Name}'，另一个脚本 '{_scriptRunner.ExecutionStatus.CurrentScript?.Name}' 正在运行中。",
                            ToolTipIcon.Warning);
                        return;
                    }

                    ShowBalloonTip("执行中", $"正在执行脚本: {script.Name}", ToolTipIcon.Info);

                    var result = await _scriptRunner.ExecuteScriptAsync(script);

                    // 更新最后执行时间
                    await _databaseService.UpdateScriptLastExecutedAsync(script.Id, DateTime.Now);

                    // 显示执行结果
                    var icon = result.Success ? ToolTipIcon.Info : ToolTipIcon.Error;
                    var title = result.Success ? "执行成功" : "执行失败";
                    var message = result.Success
                        ? $"脚本 '{script.Name}' 执行成功\n耗时: {result.Duration.TotalSeconds:F2} 秒"
                        : $"脚本 '{script.Name}' 执行失败\n错误: {result.Error}";

                    ShowBalloonTip(title, message, icon);

                    // 触发事件通知主界面更新
                    ScriptExecutionRequested?.Invoke(this, script);
                }
                catch (Exception ex)
                {
                    ShowBalloonTip("执行错误", $"执行脚本 '{script.Name}' 时发生错误: {ex.Message}", ToolTipIcon.Error);
                }
            }
        }

        private void OnShowMainWindowClick(object? sender, EventArgs e)
        {
            ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
        {
            // 右键点击时显示菜单（已由 ContextMenuStrip 自动处理）
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void ShowMainWindow()
        {
            if (_mainWindow is MainWindow mainWindow)
            {
                // Use the enhanced window restoration method
                mainWindow.RestoreWindow();

                // Also use the enhanced window activation from SingleInstanceManager
                SingleInstanceManager.BringWindowToForeground(mainWindow);
            }
            else if (_mainWindow != null)
            {
                // Fallback for generic Window
                SingleInstanceManager.BringWindowToForeground(_mainWindow);
            }
        }

        public void HideMainWindow()
        {
            _mainWindow?.Hide();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
