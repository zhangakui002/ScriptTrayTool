using ScriptTrayTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTrayTool.Services
{
    public class ScriptRunner
    {
        private readonly string _scriptsDirectory;
        private readonly string _logsDirectory;
        private readonly ScriptExecutionStatus _executionStatus;

        // Events for real-time monitoring
        public event EventHandler<ScriptExecutionStatus>? ExecutionStatusChanged;
        public event EventHandler<string>? OutputReceived;

        public ScriptExecutionStatus ExecutionStatus => _executionStatus;

        public ScriptRunner()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptTrayTool");
            _scriptsDirectory = Path.Combine(appDataPath, "scripts");
            _logsDirectory = Path.Combine(appDataPath, "logs");

            Directory.CreateDirectory(_scriptsDirectory);
            Directory.CreateDirectory(_logsDirectory);

            _executionStatus = new ScriptExecutionStatus();
        }

        public async Task<ScriptExecutionResult> ExecuteScriptAsync(Script script)
        {
            // Check if another script is already running
            if (_executionStatus.IsRunning)
            {
                throw new InvalidOperationException($"无法执行脚本 '{script.Name}'，另一个脚本 '{_executionStatus.CurrentScript?.Name}' 正在运行中。");
            }

            var result = new ScriptExecutionResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                // Set execution status
                _executionStatus.CurrentScript = script;
                _executionStatus.StartTime = DateTime.Now;
                _executionStatus.IsRunning = true;
                _executionStatus.CurrentOutput = string.Empty;

                // Notify status change
                ExecutionStatusChanged?.Invoke(this, _executionStatus);

                // Create temporary script file
                var scriptFileName = $"{script.Name}_{DateTime.Now:yyyyMMdd_HHmmss}{script.GetFileExtension()}";
                var scriptPath = Path.Combine(_scriptsDirectory, scriptFileName);

                // 根据脚本类型选择合适的编码
                var encoding = script.ScriptType switch
                {
                    ScriptType.Batch => new UTF8Encoding(false), // 批处理文件不使用BOM
                    ScriptType.PowerShell => new UTF8Encoding(false), // PowerShell也不使用BOM
                    _ => new UTF8Encoding(false)
                };

                await File.WriteAllTextAsync(scriptPath, script.Content, encoding);

                // Prepare process
                var processInfo = new ProcessStartInfo
                {
                    FileName = script.GetExecutor(),
                    Arguments = script.GetExecutorArguments(scriptPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _scriptsDirectory
                };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                using var process = new Process { StartInfo = processInfo };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);

                        // Update real-time output
                        _executionStatus.CurrentOutput += e.Data + Environment.NewLine;

                        // Notify output received
                        OutputReceived?.Invoke(this, e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuilder.AppendLine(e.Data);

                        // Update real-time output (errors in red would be nice, but for now just add them)
                        _executionStatus.CurrentOutput += "[ERROR] " + e.Data + Environment.NewLine;

                        // Notify output received
                        OutputReceived?.Invoke(this, "[ERROR] " + e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.ExitCode = process.ExitCode;
                result.Output = outputBuilder.ToString();
                result.Error = errorBuilder.ToString();
                result.Success = process.ExitCode == 0;

                // Clean up temporary script file
                try
                {
                    File.Delete(scriptPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                // Save log
                await SaveExecutionLogAsync(script, result);

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = false;
                result.Error = ex.Message;
                result.ExitCode = -1;

                await SaveExecutionLogAsync(script, result);
                return result;
            }
            finally
            {
                // Reset execution status
                _executionStatus.Reset();

                // Notify status change
                ExecutionStatusChanged?.Invoke(this, _executionStatus);
            }
        }

        private async Task SaveExecutionLogAsync(Script script, ScriptExecutionResult result)
        {
            try
            {
                var logFileName = $"script_{DateTime.Now:yyyyMMdd}.log";
                var logPath = Path.Combine(_logsDirectory, logFileName);

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{result.StartTime:yyyy-MM-dd HH:mm:ss}] ========== Script Execution ==========");
                logEntry.AppendLine($"Script: {script.Name}");
                logEntry.AppendLine($"Type: {script.ScriptType}");
                logEntry.AppendLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");
                logEntry.AppendLine($"Exit Code: {result.ExitCode}");
                logEntry.AppendLine($"Success: {result.Success}");
                logEntry.AppendLine();

                if (!string.IsNullOrEmpty(result.Output))
                {
                    logEntry.AppendLine("=== Output ===");
                    logEntry.AppendLine(result.Output);
                    logEntry.AppendLine();
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    logEntry.AppendLine("=== Error ===");
                    logEntry.AppendLine(result.Error);
                    logEntry.AppendLine();
                }

                logEntry.AppendLine("================================================");
                logEntry.AppendLine();

                await File.AppendAllTextAsync(logPath, logEntry.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public async Task<string> GetTodayLogAsync()
        {
            try
            {
                var logFileName = $"script_{DateTime.Now:yyyyMMdd}.log";
                var logPath = Path.Combine(_logsDirectory, logFileName);

                if (File.Exists(logPath))
                {
                    return await File.ReadAllTextAsync(logPath, Encoding.UTF8);
                }

                return "今日暂无执行日志。";
            }
            catch (Exception ex)
            {
                return $"读取日志失败: {ex.Message}";
            }
        }



        public string[] GetAvailableLogDates()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logsDirectory, "script_*.log");
                var dates = new List<string>();

                foreach (var file in logFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.StartsWith("script_") && fileName.Length == 15) // script_yyyyMMdd
                    {
                        var dateStr = fileName.Substring(7);
                        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                        {
                            dates.Add(date.ToString("yyyy-MM-dd"));
                        }
                    }
                }

                dates.Sort((a, b) => string.Compare(b, a)); // 降序排列
                return dates.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task<string> GetLogByDateAsync(DateTime date, bool reverseOrder = true)
        {
            try
            {
                var logFileName = $"script_{date:yyyyMMdd}.log";
                var logPath = Path.Combine(_logsDirectory, logFileName);

                if (File.Exists(logPath))
                {
                    var content = await File.ReadAllTextAsync(logPath, Encoding.UTF8);

                    if (reverseOrder && !string.IsNullOrEmpty(content))
                    {
                        // Parse log entries and reverse their order
                        var entries = ParseLogEntries(content);
                        entries.Reverse();
                        return string.Join(Environment.NewLine, entries);
                    }

                    return content;
                }

                return $"{date:yyyy-MM-dd} 无执行日志。";
            }
            catch (Exception ex)
            {
                return $"读取日志失败: {ex.Message}";
            }
        }

        private List<string> ParseLogEntries(string logContent)
        {
            var entries = new List<string>();
            var lines = logContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var currentEntry = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("[") && line.Contains("] ========== Script Execution =========="))
                {
                    // Start of a new entry
                    if (currentEntry.Count > 0)
                    {
                        entries.Add(string.Join(Environment.NewLine, currentEntry));
                        currentEntry.Clear();
                    }
                    currentEntry.Add(line);
                }
                else
                {
                    currentEntry.Add(line);
                }
            }

            // Add the last entry
            if (currentEntry.Count > 0)
            {
                entries.Add(string.Join(Environment.NewLine, currentEntry));
            }

            return entries;
        }

        public Task<bool> ClearLogByDateAsync(DateTime date)
        {
            try
            {
                var logFileName = $"script_{date:yyyyMMdd}.log";
                var logPath = Path.Combine(_logsDirectory, logFileName);

                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ClearAllLogsAsync()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logsDirectory, "script_*.log");
                foreach (var file in logFiles)
                {
                    File.Delete(file);
                }
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<string> GetTodayLogAsync(bool reverseOrder = true)
        {
            return await GetLogByDateAsync(DateTime.Now, reverseOrder);
        }
    }
}
