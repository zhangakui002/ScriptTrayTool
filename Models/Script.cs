using System;
using System.ComponentModel.DataAnnotations;

namespace ScriptTrayTool.Models
{
    public class Script
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public ScriptType ScriptType { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime? LastExecuted { get; set; }

        public string GetFileExtension()
        {
            return ScriptType switch
            {
                ScriptType.Batch => ".bat",
                ScriptType.PowerShell => ".ps1",
                _ => ".txt"
            };
        }

        public string GetExecutor()
        {
            return ScriptType switch
            {
                ScriptType.Batch => "cmd.exe",
                ScriptType.PowerShell => "powershell.exe",
                _ => "cmd.exe"
            };
        }

        public string GetExecutorArguments(string scriptPath)
        {
            return ScriptType switch
            {
                ScriptType.Batch => $"/c \"{scriptPath}\"",
                ScriptType.PowerShell => $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                _ => $"/c \"{scriptPath}\""
            };
        }
    }

    public enum ScriptType
    {
        Batch,
        PowerShell
    }
}
