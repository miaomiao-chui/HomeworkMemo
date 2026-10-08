using System;
using System.Windows;

namespace HomeworkMemo;

public partial class EditDialog : Window
{
    private static readonly int[] RemindOptions = { 0, 10, 30, 60, 120, 1440 };
    private readonly HomeworkItem _target;

    public HomeworkItem Result => _target;

    public EditDialog(HomeworkItem? item, int defaultRemind)
    {
        InitializeComponent();
        _target = item ?? new HomeworkItem { RemindBeforeMinutes = defaultRemind };

        TitleBox.Text = _target.Title;
        CourseBox.Text = _target.Course ?? "";
        DeadlinePicker.SelectedDate = _target.Deadline.Date;
        TimeBox.Text = _target.Deadline.ToString("HH:mm");

        int idx = Array.IndexOf(RemindOptions, _target.RemindBeforeMinutes);
        if (idx < 0) idx = 2;
        RemindBox.SelectedIndex = idx;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (DeadlinePicker.SelectedDate is not DateTime date)
        {
            MessageBox.Show("请选择截止日期。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryParseTime(TimeBox.Text, out var time))
        {
            MessageBox.Show("时间格式无效，请使用如 20:00 的格式。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _target.Title = TitleBox.Text.Trim();
        _target.Course = string.IsNullOrWhiteSpace(CourseBox.Text) ? null : CourseBox.Text.Trim();
        _target.Deadline = date.Date + time;
        _target.RemindBeforeMinutes = RemindOptions[Math.Clamp(RemindBox.SelectedIndex, 0, RemindOptions.Length - 1)];
        _target.LastRemindedAt = null;
        DialogResult = true;
    }

    private static bool TryParseTime(string? s, out TimeSpan t)
    {
        t = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (TimeSpan.TryParse(s.Trim(), out t)) return true;
        if (DateTime.TryParse(s.Trim(), out var dt)) { t = dt.TimeOfDay; return true; }
        return false;
    }
}
