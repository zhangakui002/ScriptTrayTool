using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptTrayTool.Services
{
    public class SingleInstanceManager : IDisposable
    {
        private const string MUTEX_NAME = "ScriptTrayTool_SingleInstance_Mutex";
        private const string PIPE_NAME = "ScriptTrayTool_SingleInstance_Pipe";
        private const string ACTIVATION_MESSAGE = "ACTIVATE_WINDOW";

        private Mutex? _mutex;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        public event EventHandler? ActivationRequested;
        public event EventHandler<string[]>? ArgumentsReceived;

        // Windows API for bringing window to foreground
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        public bool TryAcquireInstance()
        {
            try
            {
                // Try to create or open the mutex
                _mutex = new Mutex(true, MUTEX_NAME, out bool createdNew);

                if (createdNew)
                {
                    // This is the first instance
                    StartPipeServer();
                    return true;
                }
                else
                {
                    // Another instance is already running
                    _mutex.Dispose();
                    _mutex = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in TryAcquireInstance: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> NotifyExistingInstanceAsync(string[]? args = null)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);

                // Try to connect with timeout
                var connectTask = pipeClient.ConnectAsync(3000);
                await connectTask;

                if (pipeClient.IsConnected)
                {
                    // Send activation message with optional arguments
                    var message = ACTIVATION_MESSAGE;
                    if (args != null && args.Length > 0)
                    {
                        message += "|" + string.Join("|", args);
                    }

                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await pipeClient.FlushAsync();
                    return true;
                }
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("Timeout connecting to existing instance");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error notifying existing instance: {ex.Message}");
            }

            return false;
        }

        private void StartPipeServer()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () => await RunPipeServerAsync(_cancellationTokenSource.Token));
        }

        private async Task RunPipeServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (_pipeServer.IsConnected)
                    {
                        var buffer = new byte[1024];
                        int bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead > 0)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var parts = message.Split('|');

                            if (parts.Length > 0 && parts[0] == ACTIVATION_MESSAGE)
                            {
                                // Extract arguments if present
                                var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                                // Notify on UI thread
                                Application.Current?.Dispatcher.BeginInvoke(() =>
                                {
                                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                                    if (args.Length > 0)
                                    {
                                        ArgumentsReceived?.Invoke(this, args);
                                    }
                                });
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Pipe server error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }
        }

        public static void BringWindowToForeground(Window window)
        {
            if (window == null) return;

            try
            {
                // First ensure window is visible and not minimized
                if (!window.IsVisible)
                {
                    window.Show();
                }

                // Force refresh to prevent black screen issues
                window.InvalidateVisual();
                window.UpdateLayout();

                var windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

                // If window is minimized, restore it
                if (IsIconic(windowHandle))
                {
                    ShowWindow(windowHandle, SW_RESTORE);
                }
                else
                {
                    ShowWindow(windowHandle, SW_SHOW);
                }

                // Force window to foreground using Windows API
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != windowHandle)
                {
                    GetWindowThreadProcessId(foregroundWindow, out uint foregroundThreadId);
                    uint currentThreadId = GetCurrentThreadId();

                    if (foregroundThreadId != currentThreadId)
                    {
                        AttachThreadInput(currentThreadId, foregroundThreadId, true);
                        SetForegroundWindow(windowHandle);
                        AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                    else
                    {
                        SetForegroundWindow(windowHandle);
                    }
                }

                // Ensure window is activated and focused
                window.Activate();
                window.Focus();

                // Additional refresh after activation
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    window.InvalidateVisual();
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error bringing window to foreground: {ex.Message}");

                // Fallback to WPF methods
                try
                {
                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    window.Show();
                    window.InvalidateVisual();
                    window.UpdateLayout();
                    window.Activate();
                    window.Topmost = true;
                    window.Topmost = false;
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"Fallback window activation failed: {fallbackEx.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cancellationTokenSource?.Cancel();
            _pipeServer?.Dispose();
            _cancellationTokenSource?.Dispose();
            _mutex?.Dispose();

            _disposed = true;
        }
    }
}
