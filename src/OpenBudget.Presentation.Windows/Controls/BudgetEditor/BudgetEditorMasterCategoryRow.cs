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
            if (this.DataContext is MasterCategoryRowViewModel viewModel)
            {
                var categoryRow = dropInfo.Data as CategoryRowViewModel;
                if (viewModel.MasterCategory.Categories.Contains(categoryRow.Category))
                {
                    categoryRow.Category.SetSortOrder(dropInfo.InsertIndex);
                    categoryRow.Category.Model.SaveChanges();
                }
                else
                {
                    viewModel.MasterCategory.Categories.Add(categoryRow.Category);
                    categoryRow.Category.SetSortOrder(dropInfo.InsertIndex);
                    categoryRow.Category.Model.SaveChanges();
                }

                viewModel.Categories.ForceSort();
            }
        }
    }
}
