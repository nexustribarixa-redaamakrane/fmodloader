using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FModLoaderInstaller.ViewModels;

namespace FModLoaderInstaller.Views;

public partial class DirectoryPage : UserControl
{
    public DirectoryPage()
    {
        InitializeComponent();
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DirectoryPageViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Destination Directory",
                    AllowMultiple = false
                });

                if (folders.Any())
                {
                    vm.TargetDirectory = folders.First().Path.LocalPath;
                }
            }
        }
    }
}
