using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace HomeworkMemo;

public partial class MainWindow : Window
{
    private readonly StorageService _storage = new();
    private readonly DispatcherTimer _remindTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _countdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private Forms.NotifyIcon? _tray;
    private bool _reallyExit;
    private bool _dragging;
    private Point _dragStartScreen;
    private double _dragStartLeft;
    private double _dragStartTop;

    public ObservableCollection<HomeworkItem> Items => _storage.Items;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Topmost = _storage.Settings.AlwaysOnTop;
        Opacity = _storage.Settings.Opacity;
        UpdatePinButton();
        foreach (var item in Items) SubscribeToItem(item);
        _remindTimer.Tick += (s, e) => CheckReminders();
        _countdownTimer.Tick += (s, e) => RefreshCountdown();
        SetupTray();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionInitial();
        EnsureOnScreen();
        ApplyPinState();
        SortItems();
        ApplyAutoStart();
        RefreshCountdown();
        _remindTimer.Start();
        _countdownTimer.Start();
    }

    private void PositionInitial()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 20;
        Top = area.Top + 20;
    }

    private void SetupTray()
    {
        System.Drawing.Icon? icon = null;
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }

        _tray = new Forms.NotifyIcon
        {
            Icon = icon ?? System.Drawing.SystemIcons.Application,
            Text = "不拖延记事本",
            Visible = true,
        };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示", null, (s, e) => ShowFromTray());
        menu.Items.Add("退出", null, (s, e) => { _reallyExit = true; System.Windows.Application.Current.Shutdown(); });
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (s, e) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        ApplyPinState();
        Activate();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _dragging = true;
        _dragStartScreen = PointToScreen(e.GetPosition(this));
        _dragStartLeft = Left;
        _dragStartTop = Top;
        (sender as UIElement)?.CaptureMouse();
    }

    private void Header_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var screen = PointToScreen(e.GetPosition(this));
        Left = _dragStartLeft + (screen.X - _dragStartScreen.X);
        Top = _dragStartTop + (screen.Y - _dragStartScreen.Y);
    }

    private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        (sender as UIElement)?.ReleaseMouseCapture();
        EnsureOnScreen();
    }

    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var w = Width + e.HorizontalChange;
        var h = Height + e.VerticalChange;
        if (w >= MinWidth) Width = w;
        if (h >= MinHeight) Height = h;
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _storage.Settings.AlwaysOnTop = !_storage.Settings.AlwaysOnTop;
        _storage.Save();
        ApplyPinState();
    }

    private void UpdatePinButton()
    {
        PinButton.Opacity = _storage.Settings.AlwaysOnTop ? 1.0 : 0.45;
        PinButton.ToolTip = _storage.Settings.AlwaysOnTop ? "已固定：置顶显示" : "未固定：贴桌面（不置顶）";
    }

    private void ApplyPinState()
    {
        var handle = GetHandle();
        var left = Left;
        var top = Top;

        if (_storage.Settings.AlwaysOnTop)
        {
            DesktopHelper.DetachFromDesktop(handle);
            Topmost = true;
        }
        else
        {
            Topmost = false;
            DesktopHelper.AttachToDesktop(handle);
        }

        Left = left;
        Top = top;
        EnsureOnScreen();
        UpdatePinButton();
    }

    private IntPtr GetHandle() => new WindowInteropHelper(this).Handle;

    private void EnsureOnScreen()
    {
        var area = SystemParameters.WorkArea;

        if (Left + Width <= area.Left || Left >= area.Right || Top + Height <= area.Top || Top >= area.Bottom)
        {
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
            return;
        }

        if (Left < area.Left) Left = area.Left;
        if (Top < area.Top) Top = area.Top;
        if (Left + Width > area.Right) Left = area.Right - Width;
        if (Top + Height > area.Bottom) Top = area.Bottom - Height;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new EditDialog(null, _storage.Settings.DefaultRemindBeforeMinutes);
        if (dlg.ShowDialog() == true)
        {
            _storage.Items.Add(dlg.Result);
            SubscribeToItem(dlg.Result);
            _storage.Save();
            SortItems();
            RefreshCountdown();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is HomeworkItem item)
        {
            var dlg = new EditDialog(item, _storage.Settings.DefaultRemindBeforeMinutes);
            if (dlg.ShowDialog() == true)
            {
                _storage.Save();
                SortItems();
                RefreshCountdown();
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog(_storage);
        if (dlg.ShowDialog() == true)
        {
            Opacity = _storage.Settings.Opacity;
            UpdatePinButton();
            ApplyAutoStart();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_reallyExit)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _tray?.Dispose();
        }
        base.OnClosing(e);
    }

    private void Item_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Item_Drop(object sender, DragEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not HomeworkItem item) return;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;

        int added = 0;
        foreach (var f in files)
        {
            if (!File.Exists(f)) continue;
            try
            {
                _storage.AddAttachment(item, f);
                added++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加附件失败：{f}\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        if (added > 0) RefreshCountdown();
    }

    private void Attachment_Open_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Attachment att)
        {
            if (att.Kind == "text") EditTextNote(att);
            else _storage.OpenAttachment(att);
        }
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is HomeworkItem item)
        {
            var dlg = new NoteDialog("");
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.NoteText))
            {
                _storage.AddTextNote(item, dlg.NoteText.Trim());
                RefreshCountdown();
            }
        }
    }

    private void EditTextNote(Attachment att)
    {
        var dlg = new NoteDialog(att.Text);
        if (dlg.ShowDialog() == true)
        {
            if (string.IsNullOrWhiteSpace(dlg.NoteText))
            {
                var item = Items.FirstOrDefault(i => i.Attachments.Contains(att));
                if (item != null) _storage.RemoveAttachment(item, att);
            }
            else
            {
                _storage.UpdateTextNote(att, dlg.NoteText.Trim());
            }
            RefreshCountdown();
        }
    }

    private void Attachment_Remove_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Attachment att)
        {
            var item = Items.FirstOrDefault(i => i.Attachments.Contains(att));
            if (item != null) _storage.RemoveAttachment(item, att);
            RefreshCountdown();
        }
    }

    private void CheckReminders()
    {
        if (!_storage.Settings.NotifyEnabled) return;
        var now = DateTime.Now;
        foreach (var item in Items)
        {
            if (item.IsDone || item.LastRemindedAt != null || item.RemindBeforeMinutes <= 0) continue;
            var remindAt = item.Deadline.AddMinutes(-item.RemindBeforeMinutes);
            if (now >= remindAt)
            {
                item.LastRemindedAt = now;
                ShowToast($"作业提醒：{item.DisplayTitle}", $"截止时间：{item.Deadline:MM-dd HH:mm}");
            }
        }
    }

    private void ShowToast(string title, string body)
    {
        var toast = new ToastWindow(title, body);
        toast.Show();
    }

    private void RefreshCountdown()
    {
        foreach (var item in Items) item.NotifyCountdownChanged();
        UpdateSummary();
    }

    private void SubscribeToItem(HomeworkItem item)
    {
        item.PropertyChanged += OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HomeworkItem.IsDone)) return;
        if (sender is HomeworkItem item)
        {
            item.CompletedAt = item.IsDone ? DateTime.Now : null;
        }
        _storage.Save();
        SortItems();
        UpdateSummary();
    }

    private void SortItems()
    {
        var unfinished = Items.Where(i => !i.IsDone).OrderBy(i => i.Deadline).ToList();
        var completed = Items.Where(i => i.IsDone).OrderByDescending(i => i.CompletedAt ?? DateTime.MinValue).ToList();
        var sorted = unfinished.Concat(completed).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = Items.IndexOf(sorted[i]);
            if (cur != i) Items.Move(cur, i);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not HomeworkItem item) return;

        var result = MessageBox.Show(
            $"确定删除「{item.DisplayTitle}」吗？\n该条目的附件文件也会一并删除，且无法恢复。",
            "删除确认",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK) return;

        _storage.DeleteItem(item);
        SortItems();
        UpdateSummary();
    }

    private void ApplyAutoStart()
    {
        const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
            if (key == null) return;
            if (_storage.Settings.AutoStart)
            {
                var exe = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exe))
                    key.SetValue("不拖延记事本", $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue("不拖延记事本", false);
            }
        }
        catch { }
    }

    private void UpdateSummary()
    {
        int total = Items.Count;
        int done = Items.Count(i => i.IsDone);
        SummaryText.Text = $"共 {total} 项 · 未完成 {total - done} 项";
    }
}
