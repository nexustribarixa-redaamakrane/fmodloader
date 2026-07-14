using Avalonia.Controls;
using Avalonia.Interactivity;
using fModLoader.ViewModels;

namespace fModLoader.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        DataContext = new AboutDialogViewModel();
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
