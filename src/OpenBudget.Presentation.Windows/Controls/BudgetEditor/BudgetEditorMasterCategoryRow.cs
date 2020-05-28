using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class BudgetEditorMasterCategoryRow : ItemsControl
    {
        static BudgetEditorMasterCategoryRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BudgetEditorMasterCategoryRow), new FrameworkPropertyMetadata(typeof(BudgetEditorMasterCategoryRow)));
        }
    }
}
