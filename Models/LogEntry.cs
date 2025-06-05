using System;

namespace ScriptTrayTool.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ScriptName { get; set; } = string.Empty;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ExitCode { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class ScriptExecutionResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
