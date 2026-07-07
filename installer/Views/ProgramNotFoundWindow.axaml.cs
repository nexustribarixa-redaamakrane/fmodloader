using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FModLoaderInstaller.Views;

public partial class ProgramNotFoundWindow : Window
{
    public ProgramNotFoundWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void ExitButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
        System.Environment.Exit(0);
    }
}
