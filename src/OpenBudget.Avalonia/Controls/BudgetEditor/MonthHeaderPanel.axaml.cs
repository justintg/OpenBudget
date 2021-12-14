using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public partial class MonthHeaderPanel : UserControl
    {
        public MonthHeaderPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
