using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HomeworkMemo;

public class Attachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "file";
    public string OriginalName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public long Size { get; set; }
    public string Text { get; set; } = "";

    [JsonIgnore] public string KindLabel => Kind == "text" ? "📝 " : "📎 ";
    [JsonIgnore] public string DisplayText => Kind == "text"
        ? (string.IsNullOrWhiteSpace(Text) ? "(空白笔记)" : Text.Replace("\r", " ").Replace("\n", " ").Trim())
        : OriginalName;
    [JsonIgnore] public string ToolTipText => Kind == "text" ? Text : OriginalName;
}

public class HomeworkItem : INotifyPropertyChanged
{
    private string _title = "";
    private string? _course;
    private DateTime _deadline = DateTime.Now.AddDays(1);
    private bool _isDone;
    private int _remindBeforeMinutes = 30;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title
    {
        get => _title;
        set { if (_title != value) { _title = value; OnPropertyChanged(); } }
    }

    public string? Course
    {
        get => _course;
        set { if (_course != value) { _course = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCourse)); } }
    }

    public DateTime Deadline
    {
        get => _deadline;
        set { if (_deadline != value) { _deadline = value; OnPropertyChanged(); NotifyCountdownChanged(); } }
    }

    public bool IsDone
    {
        get => _isDone;
        set { if (_isDone != value) { _isDone = value; OnPropertyChanged(); NotifyCountdownChanged(); } }
    }

    public int RemindBeforeMinutes
    {
        get => _remindBeforeMinutes;
        set { if (_remindBeforeMinutes != value) { _remindBeforeMinutes = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Attachment> Attachments { get; set; } = new();

    public DateTime? CompletedAt { get; set; }

    [JsonIgnore] public DateTime? LastRemindedAt { get; set; }

    [JsonIgnore] public bool HasCourse => !string.IsNullOrWhiteSpace(Course);

    [JsonIgnore] public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "(无标题)" : Title;

    [JsonIgnore]
    public string DeadlineText
    {
        get
        {
            if (IsDone) return $"已完成 · 截止 {Deadline:MM-dd HH:mm}";
            var diff = Deadline - DateTime.Now;
            if (diff <= TimeSpan.Zero) return $"已逾期 {FormatSpan(-diff)} · 截止 {Deadline:MM-dd HH:mm}";
            return $"剩 {FormatSpan(diff)} · 截止 {Deadline:MM-dd HH:mm}";
        }
    }

    [JsonIgnore]
    public string Urgency
    {
        get
        {
            if (IsDone) return "normal";
            var diff = Deadline - DateTime.Now;
            if (diff <= TimeSpan.Zero) return "overdue";
            if (diff < TimeSpan.FromHours(24)) return "soon";
            return "normal";
        }
    }

    public void NotifyCountdownChanged()
    {
        OnPropertyChanged(nameof(DeadlineText));
        OnPropertyChanged(nameof(Urgency));
    }

    public static string FormatSpan(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"{(int)t.TotalDays} 天 {t.Hours} 小时";
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours} 小时 {t.Minutes} 分";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes} 分钟";
        return "不到 1 分钟";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class AppSettings
{
    public string StorageRoot { get; set; } = "";
    public bool NotifyEnabled { get; set; } = true;
    public int DefaultRemindBeforeMinutes { get; set; } = 30;
    public bool AlwaysOnTop { get; set; } = true;
    public double Opacity { get; set; } = 0.92;
    public bool AutoStart { get; set; }
}
