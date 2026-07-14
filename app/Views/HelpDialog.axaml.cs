using Avalonia.Controls;
using Avalonia.Interactivity;
using fModLoader.ViewModels;

namespace fModLoader.Views;

public partial class HelpDialog : Window
{
    public HelpDialog()
    {
        InitializeComponent();
        DataContext = new HelpDialogViewModel();
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
