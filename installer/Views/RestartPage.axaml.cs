using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FModLoaderInstaller.Views
{
    public partial class RestartPage : UserControl
    {
        public RestartPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
