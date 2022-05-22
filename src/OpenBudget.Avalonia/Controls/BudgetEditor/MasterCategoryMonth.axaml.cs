using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public partial class MasterCategoryMonth : UserControl
    {
        public MasterCategoryMonth()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
