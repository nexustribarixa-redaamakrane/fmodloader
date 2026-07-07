using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

public partial class PreparePageViewModel : WizardPageBase
{
    [ObservableProperty] private bool _preparationComplete;
    [ObservableProperty] private double _loadingProgressValue;
    [ObservableProperty] private string _statusText = "Initializing setup components…";

    public PreparePageViewModel()
    {
        PageTitle = "Welcome";
        PageSubtitle = "Preparing the setup…";
        CanGoBack = false;
        CanGoNext = false;
    }

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await StartPreparation();
    }

    private async Task StartPreparation()
    {
        // Smooth progress animation over 1.5 seconds
        int steps = 50;
        int delay = 1500 / steps;
        for (int i = 0; i <= steps; i++)
        {
            await Task.Delay(delay);
            LoadingProgressValue = (double)i / steps * 100;

            if (i > 40)
                StatusText = "Loading wizard flow…";
            else if (i > 20)
                StatusText = "Checking platform components…";
        }

        PreparationComplete = true;
    }
}
