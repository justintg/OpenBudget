using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenBudget.Avalonia.Views
{
    public partial class WelcomeView : UserControl
    {
        public WelcomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}