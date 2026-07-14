using Avalonia.Controls;
using fModLoader.ViewModels;

namespace fModLoader.Views;

public partial class FontEditorView : Window
{
    public FontEditorView()
    {
        InitializeComponent();
        DataContext = new FontEditorViewModel();
    }
}
