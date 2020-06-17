using GongSolutions.Wpf.DragDrop;
using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore.Internal;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Presentation.Windows.Controls.DragDrop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class BudgetEditorMasterCategoryRow : ItemsControl
    {
        static BudgetEditorMasterCategoryRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BudgetEditorMasterCategoryRow), new FrameworkPropertyMetadata(typeof(BudgetEditorMasterCategoryRow)));
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(BudgetEditorMasterCategoryRow), new PropertyMetadata(true, OnIsExpandedChanged));

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BudgetEditorMasterCategoryRow row)
            {
                row.IsExpandedChanged((bool)e.NewValue);
            }
        }

        private Storyboard _expandStoryboard;
        private DoubleAnimation _expandAnimation;
        private double _originalSize;

        private void IsExpandedChanged(bool newValue)
        {
            if (_expandStoryboard != null)
            {
                _expandStoryboard?.Remove();
                _expandStoryboard = null;
            }

            if (newValue)
            {
                double startHeight = _categoryRowItemPresenter.ActualHeight;
                _expandAnimation = new DoubleAnimation(startHeight, _originalSize, new Duration(TimeSpan.FromSeconds(0.3)));
                Storyboard.SetTarget(_expandAnimation, _categoryRowItemPresenter);
                Storyboard.SetTargetProperty(_expandAnimation, new PropertyPath(ItemsPresenter.HeightProperty));
                _expandStoryboard = new Storyboard();
                _expandStoryboard.Children.Add(_expandAnimation);
                _expandStoryboard.Completed += (sender, e) =>
                {
                    _expandStoryboard?.Remove();
                    _expandStoryboard = null;
                    _categoryRowItemPresenter.Height = double.NaN;
                };
                _expandStoryboard.Begin();
            }
            else
            {
                _originalSize = _categoryRowItemPresenter.ActualHeight;
                _expandAnimation = new DoubleAnimation(_categoryRowItemPresenter.ActualHeight, 0.0, new Duration(TimeSpan.FromSeconds(0.3)));
                Storyboard.SetTarget(_expandAnimation, _categoryRowItemPresenter);
                Storyboard.SetTargetProperty(_expandAnimation, new PropertyPath(ItemsPresenter.HeightProperty));
                _expandStoryboard = new Storyboard();
                _expandStoryboard.Children.Add(_expandAnimation);
                _expandStoryboard.Completed += (sender, e) =>
                 {
                     _expandStoryboard?.Remove();
                     _expandStoryboard = null;
                     _categoryRowItemPresenter.Height = 0.0;
                 };
                _expandStoryboard.Begin();
            }
        }

        private ItemsPresenter _categoryRowItemPresenter;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _categoryRowItemPresenter = this.GetTemplateChild("CategoryRowItemPresenter") as ItemsPresenter;
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
