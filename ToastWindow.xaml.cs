using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HomeworkMemo;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromSeconds(8) };

    public ToastWindow(string title, string body)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;
        _closeTimer.Tick += (s, e) => Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var area = System.Windows.SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 16;
        Top = area.Bottom - ActualHeight - 16;
        _closeTimer.Start();
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Close();
}
