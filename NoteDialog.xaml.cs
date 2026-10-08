using System.Windows;

namespace HomeworkMemo;

public partial class NoteDialog : Window
{
    public string NoteText { get; private set; } = "";

    public NoteDialog(string existing)
    {
        InitializeComponent();
        NoteBox.Text = existing;
        NoteBox.Focus();
        NoteBox.CaretIndex = NoteBox.Text.Length;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        NoteText = NoteBox.Text;
        DialogResult = true;
    }
}
