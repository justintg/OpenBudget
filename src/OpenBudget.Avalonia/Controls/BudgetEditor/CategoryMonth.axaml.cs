using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public partial class CategoryMonth : UserControl
    {
        public CategoryMonth()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
