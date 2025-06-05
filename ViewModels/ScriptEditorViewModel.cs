using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptTrayTool.Models;
using ScriptTrayTool.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScriptTrayTool.ViewModels
{
    public partial class ScriptEditorViewModel : ObservableValidator
    {
        private readonly DatabaseService _databaseService;
        private readonly Script _originalScript;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "脚本名称不能为空")]
        [StringLength(100, ErrorMessage = "脚本名称不能超过100个字符")]
        private string _scriptName = string.Empty;

        [ObservableProperty]
        private ScriptType _scriptType = ScriptType.Batch;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "脚本内容不能为空")]
        private string _scriptContent = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _windowTitle = "新建脚本";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? RequestClose;

        public ScriptEditorViewModel(DatabaseService databaseService, Script? script = null)
        {
            _databaseService = databaseService;
            _originalScript = script ?? new Script();

            IsEditMode = script != null;
            WindowTitle = IsEditMode ? $"编辑脚本 - {script!.Name}" : "新建脚本";

            // 先初始化命令
            SaveCommand = new RelayCommand(SaveScript, CanSaveScript);
            CancelCommand = new RelayCommand(Cancel);

            // 然后设置属性值，这样OnChanged方法就能正常工作
            if (IsEditMode)
            {
                ScriptName = _originalScript.Name;
                ScriptType = _originalScript.ScriptType;
                ScriptContent = _originalScript.Content;
            }
        }

        private bool CanSaveScript()
        {
            return !HasErrors &&
                   !string.IsNullOrWhiteSpace(ScriptName) &&
                   !string.IsNullOrWhiteSpace(ScriptContent);
        }

        private async void SaveScript()
        {
            try
            {
                var script = new Script
                {
                    Id = _originalScript.Id,
                    Name = ScriptName.Trim(),
                    ScriptType = ScriptType,
                    Content = ScriptContent,
                    LastExecuted = _originalScript.LastExecuted
                };

                await _databaseService.SaveScriptAsync(script);

                var message = IsEditMode ? "脚本更新成功！" : "脚本创建成功！";
                MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                var action = IsEditMode ? "更新" : "创建";
                MessageBox.Show($"{action}脚本失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        partial void OnScriptNameChanged(string value)
        {
            if (SaveCommand is RelayCommand relayCommand)
            {
                relayCommand.NotifyCanExecuteChanged();
            }
        }

        partial void OnScriptContentChanged(string value)
        {
            if (SaveCommand is RelayCommand relayCommand)
            {
                relayCommand.NotifyCanExecuteChanged();
            }
        }

        public string GetSyntaxHighlighting()
        {
            return ScriptType switch
            {
                ScriptType.PowerShell => "PowerShell",
                ScriptType.Batch => "Batch",
                _ => "Text"
            };
        }
    }
}
