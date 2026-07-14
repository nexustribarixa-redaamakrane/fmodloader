using Avalonia.Controls;
using Avalonia.Interactivity;

namespace fModLoader.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }

    private void OpenHelpClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new HelpDialog();
        dialog.ShowDialog(this);
    }

    private void OpenGlyphEditorClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new FontEditorView();
        dialog.ShowDialog(this);
    }
}
