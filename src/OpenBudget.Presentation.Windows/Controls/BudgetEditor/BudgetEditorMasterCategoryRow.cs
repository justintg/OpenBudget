using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore.Internal;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Presentation.Windows.Controls.DragDrop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        internal void HandleDrop(BudgetEditorDragDropHandler dropInfo)
        {
            if (dropInfo.DragType != BudgetEditorDragTypes.Category) throw new InvalidOperationException();

            if (this.DataContext is MasterCategoryRowViewModel viewModel)
            {
                var categoryRow = dropInfo.CategoryRow.DataContext as CategoryRowViewModel;
                if (viewModel.MasterCategory.Categories.Contains(categoryRow.Category))
                {
                    categoryRow.Category.SetSortOrder(dropInfo.InsertPosition);
                    categoryRow.Category.Model.SaveChanges();
                }
                else
                {
                    viewModel.MasterCategory.Categories.Add(categoryRow.Category);
                    categoryRow.Category.SetSortOrder(dropInfo.InsertPosition);
                    categoryRow.Category.Model.SaveChanges();
                }

                viewModel.Categories.ForceSort();
            }
        }
    }
}
