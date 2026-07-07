using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FModLoaderInstaller.Views
{
    public partial class CleanupPage : UserControl
    {
        public CleanupPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
