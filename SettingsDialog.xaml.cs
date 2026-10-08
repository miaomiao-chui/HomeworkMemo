using System;
using System.Windows;
using Forms = System.Windows.Forms;

namespace HomeworkMemo;

public partial class SettingsDialog : Window
{
    private readonly StorageService _storage;

    public SettingsDialog(StorageService storage)
    {
        InitializeComponent();
        _storage = storage;
        RootBox.Text = storage.Settings.StorageRoot;
        NotifyBox.IsChecked = storage.Settings.NotifyEnabled;
        AutoStartBox.IsChecked = storage.Settings.AutoStart;
        OpacitySlider.Value = storage.Settings.Opacity;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog();
        if (dlg.ShowDialog() == Forms.DialogResult.OK)
            RootBox.Text = dlg.SelectedPath;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RootBox.Text))
        {
            MessageBox.Show("请选择附件存放位置。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _storage.ChangeStorageRoot(RootBox.Text.Trim(), MigrateBox.IsChecked == true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"设置存放位置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _storage.Settings.NotifyEnabled = NotifyBox.IsChecked == true;
        _storage.Settings.AutoStart = AutoStartBox.IsChecked == true;
        _storage.Settings.Opacity = OpacitySlider.Value;
        _storage.Save();
        DialogResult = true;
    }
}
