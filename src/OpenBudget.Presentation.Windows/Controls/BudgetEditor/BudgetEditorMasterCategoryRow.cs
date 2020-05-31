using GongSolutions.Wpf.DragDrop;
using OpenBudget.Application.ViewModels.BudgetEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class BudgetEditorMasterCategoryRow : ItemsControl, IDropTarget
    {
        static BudgetEditorMasterCategoryRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BudgetEditorMasterCategoryRow), new FrameworkPropertyMetadata(typeof(BudgetEditorMasterCategoryRow)));
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var categoryRow = dropInfo.Data as CategoryRowViewModel;
            categoryRow.Category.SetSortOrder(dropInfo.InsertIndex);
            categoryRow.Category.Model.SaveChanges();
            if (this.DataContext is MasterCategoryRowViewModel viewModel)
            {
                viewModel.Categories.ForceSort();
            }
        }
    }
}
