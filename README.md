# 作业备忘录（HomeworkMemo）— 界面原型

Windows 原生 WPF 桌面备忘录原型，用于先确认交互，再做完整版。

## 原型已实现

- 桌面挂件式无边框窗口：可拖动、半透明、可置顶
- 作业列表：完成勾选、标题、课程（选填）、截止时间与倒计时/逾期状态
- 新增 / 编辑作业：标题、课程、截止日期 + 时间、提前提醒
- 拖拽文件到作业卡片上添加附件（复制到本地管理目录）
- 点击附件文件名用系统默认程序打开，✕ 移除附件
- 设置：附件存放位置（可改，可选迁移）、通知开关、置顶、透明度
- 截止前提醒（应用内 Toast，会话内每项提醒一次）
- 关闭到系统托盘，托盘菜单可「显示 / 退出」
- 数据本地 JSON 持久化，完全离线

## 运行要求

- Windows 10 / 11
- .NET 8 SDK（或更高，需包含 Windows Desktop 工作负载）
  - 下载：https://dotnet.microsoft.com/download

## 运行方式

在 `E:\HomeworkMemo` 目录下执行：

```powershell
dotnet run
```

或在 Visual Studio 中打开 `HomeworkMemo.csproj` 后按 F5。

## 数据与附件位置

- 数据文件：`%APPDATA%\HomeworkMemo\data.json`
- 附件默认目录：`%APPDATA%\HomeworkMemo\files`

## 说明

- 这是确认交互的原型，数据用 JSON；正式版建议换 SQLite，并补充跨会话重复提醒、回收站式删除、系统原生 Toast 通知等。
- 关闭按钮是「收起托盘」，完全退出请右键托盘图标选「退出」。
