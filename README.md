# ScriptTrayTool - 脚本托盘助手

一个用于执行本地脚本的 Windows 桌面工具软件，支持批处理(.bat)和 PowerShell(.ps1)脚本。

## 功能特性

### 主要功能
- **脚本管理**: 添加、编辑、删除脚本
- **脚本执行**: 支持 .bat 和 .ps1 脚本执行
- **托盘集成**: 最小化到系统托盘，右键菜单快速执行脚本
- **日志记录**: 记录脚本执行结果和输出
- **内置编辑器**: 使用 AvalonEdit 提供语法高亮的脚本编辑器

### 界面功能
- **脚本管理页**: 查看、添加、编辑、删除脚本
- **执行日志页**: 查看脚本执行历史和输出
- **托盘功能**: 双击恢复主界面，右键菜单执行脚本

## 技术架构

### 开发环境
- **语言**: C#
- **框架**: WPF + MVVM (CommunityToolkit.Mvvm)
- **平台**: .NET 8.0 Windows
- **数据库**: SQLite (Microsoft.Data.Sqlite)
- **编辑器**: AvalonEdit

### 项目结构
```
ScriptTrayTool/
├── Models/                 # 数据模型
│   ├── Script.cs          # 脚本模型
│   └── LogEntry.cs        # 日志模型
├── Services/              # 服务层
│   ├── DatabaseService.cs # 数据库服务
│   ├── ScriptRunner.cs    # 脚本执行服务
│   └── TrayIconService.cs # 托盘图标服务
├── ViewModels/            # 视图模型
│   ├── MainViewModel.cs
│   ├── ScriptListViewModel.cs
│   ├── LogViewModel.cs
│   └── ScriptEditorViewModel.cs
├── Views/                 # 视图
│   ├── ScriptListView.xaml
│   ├── LogView.xaml
│   └── ScriptEditorWindow.xaml
├── App.xaml              # 应用程序入口
└── MainWindow.xaml       # 主窗口
```

## 使用说明

### 安装和运行
1. 确保已安装 .NET 8.0 Runtime
2. 编译项目: `dotnet build`
3. 运行应用: `dotnet run`

### 基本操作

#### 1. 脚本管理
- 点击"新建脚本"添加新脚本
- 选择脚本类型（批处理或PowerShell）
- 在编辑器中编写脚本内容
- 保存脚本

#### 2. 执行脚本
- 在脚本列表中选择要执行的脚本
- 点击"执行脚本"按钮
- 或者在系统托盘右键菜单中选择脚本执行

#### 3. 查看日志
- 切换到"执行日志"页面
- 选择日期查看历史日志
- 查看脚本执行结果和输出

#### 4. 托盘功能
- 关闭主窗口时程序最小化到托盘
- 双击托盘图标恢复主界面
- 右键托盘图标显示脚本菜单
- 点击脚本名称直接执行

## 数据存储

### 数据库位置
应用程序数据存储在用户目录下：
```
%APPDATA%\ScriptTrayTool\
├── scripttray.db          # SQLite 数据库
├── scripts/               # 临时脚本文件目录
└── logs/                  # 执行日志目录
```

### 数据库结构
```sql
-- 脚本表
CREATE TABLE Scripts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ScriptType TEXT NOT NULL, -- 'Batch' or 'PowerShell'
    Content TEXT NOT NULL,
    LastExecuted DATETIME
);

-- 应用设置表
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
```

## 脚本执行机制

### 执行流程
1. 创建临时脚本文件
2. 根据脚本类型选择执行器（cmd.exe 或 powershell.exe）
3. 捕获标准输出和错误输出
4. 记录执行结果到日志文件
5. 清理临时文件

### 支持的脚本类型
- **批处理 (.bat)**: 使用 cmd.exe 执行
- **PowerShell (.ps1)**: 使用 powershell.exe 执行，绕过执行策略

## 开发说明

### 依赖包
- `CommunityToolkit.Mvvm`: MVVM 框架
- `Microsoft.Data.Sqlite`: SQLite 数据库
- `AvalonEdit`: 代码编辑器控件

### 扩展功能
可以通过以下方式扩展功能：
1. 添加新的脚本类型支持
2. 增加脚本参数功能
3. 添加定时执行功能
4. 实现脚本分组管理

## 注意事项

1. **权限**: 某些脚本可能需要管理员权限
2. **安全**: 请谨慎执行来源不明的脚本
3. **路径**: 脚本在临时目录中执行，注意相对路径问题
4. **编码**: 脚本文件使用 UTF-8 编码保存

## 许可证

本项目基于 MIT 许可证开源。
