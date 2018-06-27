using OpenBudget.Application.ViewModels.BudgetEditor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class BudgetEditor : Control
    {
        static BudgetEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BudgetEditor), new FrameworkPropertyMetadata(typeof(BudgetEditor)));
        }

        private ItemsControl _categoryItemsControl;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _categoryItemsControl = GetTemplateChild("PART_CategoryItemsControl") as ItemsControl;
        }

        public IList<MasterCategoryRowViewModel> MasterCategories
        {
            get { return (IList<MasterCategoryRowViewModel>)GetValue(MasterCategoriesProperty); }
            set { SetValue(MasterCategoriesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MasterCategories.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MasterCategoriesProperty =
            DependencyProperty.Register("MasterCategories", typeof(IList<MasterCategoryRowViewModel>), typeof(BudgetEditor), new PropertyMetadata(null));


    }
}
