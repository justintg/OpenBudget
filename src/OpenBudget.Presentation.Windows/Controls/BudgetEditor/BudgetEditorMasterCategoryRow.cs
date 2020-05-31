using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore.Internal;
using OpenBudget.Application.ViewModels.BudgetEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            if (dropInfo.Data is CategoryRowViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;

                if (this.DataContext is MasterCategoryRowViewModel viewModel)
                {
                    if (viewModel.Categories.Count == 0)
                    {
                        dropInfo.DropTargetAdorner = typeof(DropTargetEmptyInsertionAdorner);
                    }
                }
            }

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
