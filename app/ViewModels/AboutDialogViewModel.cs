using CommunityToolkit.Mvvm.ComponentModel;

namespace fModLoader.ViewModels;

public class AboutDialogViewModel : ObservableObject
{
    public string Title => "About fModLoader";
    public string AppName => "fModLoader";
    public string Version => "v1.0.65 Beta";
    public string Codename => "Project Vectoris";
    public string Author => "Nexus Tribarixa";
    public string Description => "A visual, high-performance font mod loading engine designed to patch custom vector outline glyphs directly into TrueType and OpenType tables.";
}
