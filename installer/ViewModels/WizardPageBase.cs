using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

/// <summary>
/// Base class for all wizard pages. Provides title, subtitle, and validation.
/// </summary>
public abstract partial class WizardPageBase : ObservableObject
{
    [ObservableProperty] private string _pageTitle = "";
    [ObservableProperty] private string _pageSubtitle = "";
    [ObservableProperty] private bool _canGoNext = true;
    [ObservableProperty] private bool _canGoBack = true;

    /// <summary>Called when navigating to this page.</summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>Called when navigating away from this page.</summary>
    public virtual void OnNavigatedFrom() { }
}
