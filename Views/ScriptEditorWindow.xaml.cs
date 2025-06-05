using ICSharpCode.AvalonEdit.Highlighting;
using ScriptTrayTool.Models;
using ScriptTrayTool.Services;
using ScriptTrayTool.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScriptTrayTool.Views
{
    public partial class ScriptEditorWindow : Window
    {
        private ScriptEditorViewModel _viewModel;

        public ScriptEditorWindow(DatabaseService databaseService, Script? script = null)
        {
            try
            {
                InitializeComponent();

                _viewModel = new ScriptEditorViewModel(databaseService, script);
                DataContext = _viewModel;

                // 订阅事件
                _viewModel.RequestClose += OnRequestClose;

                // 设置编辑器内容
                if (script != null)
                {
                    ScriptEditor.Text = script.Content;
                }

                // 绑定编辑器内容到 ViewModel
                ScriptEditor.TextChanged += (s, e) => _viewModel.ScriptContent = ScriptEditor.Text;

                // 设置语法高亮
                UpdateSyntaxHighlighting();

                // 监听脚本类型变化以更新语法高亮
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ScriptEditorViewModel.ScriptType))
                    {
                        UpdateSyntaxHighlighting();
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化脚本编辑器失败: {ex.Message}\n\n详细信息: {ex}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void UpdateSyntaxHighlighting()
        {
            try
            {
                var highlightingName = _viewModel.GetSyntaxHighlighting();

                // 设置语法高亮
                if (highlightingName == "PowerShell")
                {
                    // AvalonEdit 可能没有内置 PowerShell 高亮，使用 XML 或 C# 作为替代
                    ScriptEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                }
                else if (highlightingName == "Batch")
                {
                    // 批处理文件可能也没有专门的高亮，使用文本模式
                    ScriptEditor.SyntaxHighlighting = null;
                }
                else
                {
                    ScriptEditor.SyntaxHighlighting = null;
                }
            }
            catch (Exception ex)
            {
                // 语法高亮失败不应该影响编辑器的基本功能
                System.Diagnostics.Debug.WriteLine($"设置语法高亮失败: {ex.Message}");
                ScriptEditor.SyntaxHighlighting = null;
            }
        }

        private void OnRequestClose(object? sender, bool result)
        {
            DialogResult = result;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.RequestClose -= OnRequestClose;
            base.OnClosed(e);
        }
    }

    // 脚本类型显示转换器
    public class ScriptTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScriptType scriptType)
            {
                return scriptType switch
                {
                    ScriptType.Batch => "批处理 (.bat)",
                    ScriptType.PowerShell => "PowerShell (.ps1)",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
