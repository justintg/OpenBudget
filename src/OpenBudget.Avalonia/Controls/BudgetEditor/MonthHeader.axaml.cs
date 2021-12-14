using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public partial class MonthHeader : UserControl
    {
        public MonthHeader()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
