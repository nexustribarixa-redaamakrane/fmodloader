using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FModLoaderInstaller.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void MinButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            // Don't do anything during cleanup
            if (vm.IsCleaningUp)
                return;

            // On terminal pages (finish, cancel, premature exit, prepare, cleanup) just close
            if (vm.IsFinished
                || vm.CurrentPage is ViewModels.PreparePageViewModel
                || vm.CurrentPage is ViewModels.CancelPageViewModel
                || vm.CurrentPage is ViewModels.PrematureExitPageViewModel
                || vm.CurrentPage is ViewModels.CleanupPageViewModel)
            {
                Close();
            }
            else if (vm.IsInstalling)
            {
                // During active install/uninstall, show cancel confirmation
                vm.CancelCommand.Execute(null);
            }
            else
            {
                vm.CancelCommand.Execute(null);
            }
        }
        else
        {
            Close();
        }
    }
}
