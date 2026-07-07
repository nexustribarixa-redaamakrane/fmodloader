using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

public partial class CleanupPageViewModel : WizardPageBase
{
    [ObservableProperty] private bool _cleanupComplete;
    [ObservableProperty] private double _loadingProgressValue;
    [ObservableProperty] private string _statusText = "Setup will cleanup before closing...";

    public CleanupPageViewModel()
    {
        PageTitle = "Cleaning Up";
        PageSubtitle = "Please wait while setup completes";
        CanGoBack = false;
        CanGoNext = false;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await StartCleanup();
    }

    private async Task StartCleanup()
    {
        // Smooth progress animation over 1.5 seconds
        int steps = 50;
        int delay = 1500 / steps;
        for (int i = 0; i <= steps; i++)
        {
            await Task.Delay(delay);
            LoadingProgressValue = (double)i / steps * 100;
        }

        CleanupComplete = true;
    }
}
