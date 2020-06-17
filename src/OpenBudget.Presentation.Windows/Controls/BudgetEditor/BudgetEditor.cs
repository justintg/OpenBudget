using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using MahApps.Metro.Controls;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Model.Entities;
using OpenBudget.Presentation.Windows.Controls.DragDrop;
using OpenBudget.Presentation.Windows.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class BudgetEditor : Control
    {
        private const double MONTH_MARGIN_WIDTH = 5.0;
        private const double MIN_MONTH_WIDTH = 250.0;

        static BudgetEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BudgetEditor), new FrameworkPropertyMetadata(typeof(BudgetEditor)));
        }

        public BudgetEditor()
        {
            MonthMarginLeft = MONTH_MARGIN_WIDTH;
            this.AddHandler(PopupButton.PopupButtonOpenedEvent, new PopupButtonOpenedEventHandler(OnRowEditorOpened));
        }

        private ItemsControl _categoryItemsControl;
        private ScrollViewer _categoryItemsScrollViewer;
        private ScrollBar _categoryItemsScrollBar;

        internal ItemsControl CategoryItemsControl => _categoryItemsControl;
        internal ScrollViewer CategoryItemsScrollViewer => _categoryItemsScrollViewer;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _categoryItemsControl = GetTemplateChild("PART_CategoryItemsControl") as ItemsControl;
            _categoryItemsScrollViewer = GetTemplateChild("PART_CategoryItemsScrollViewer") as ScrollViewer;
            _categoryItemsScrollBar = GetTemplateChild("PART_CategoryItemsScrollBar") as ScrollBar;
            _categoryItemsScrollViewer.SizeChanged += CategoryItemsScrollViewer_SizeChanged;

            BindScrollBarToContent();
        }

        private void BindScrollBarToContent()
        {
            _categoryItemsScrollBar.SetBinding(ScrollBar.MaximumProperty, (new Binding("ScrollableHeight") { Source = _categoryItemsScrollViewer, Mode = BindingMode.OneWay }));
            _categoryItemsScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, (new Binding("ViewportHeight") { Source = _categoryItemsScrollViewer, Mode = BindingMode.OneWay }));

            _categoryItemsScrollBar.Scroll += (sender, e) =>
            {
                _categoryItemsScrollViewer.ScrollToVerticalOffset(e.NewValue);
            };

            _categoryItemsScrollViewer.ScrollChanged += (sender, e) =>
            {
                if (e.OriginalSource != _categoryItemsScrollViewer) return;

                _categoryItemsScrollBar.Value = e.VerticalOffset;
                _categoryItemsScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

                if (e.ViewportHeight >= e.ExtentHeight)
                {
                    _categoryItemsScrollBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _categoryItemsScrollBar.Visibility = Visibility.Visible;
                }
            };

            _categoryItemsScrollViewer.ScrollToHome();
        }

        public IList<MasterCategoryRowViewModel> MasterCategories
        {
            get { return (IList<MasterCategoryRowViewModel>)GetValue(MasterCategoriesProperty); }
            set { SetValue(MasterCategoriesProperty, value); }
        }

        public static readonly DependencyProperty MasterCategoriesProperty =
            DependencyProperty.Register("MasterCategories", typeof(IList<MasterCategoryRowViewModel>), typeof(BudgetEditor), new PropertyMetadata(null));

        public double CategoryColumnWidth
        {
            get { return (double)GetValue(CategoryColumnWidthProperty); }
            protected set { SetValue(CategoryColumnWidthPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey CategoryColumnWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("CategoryColumnWidth", typeof(double), typeof(BudgetEditor), new PropertyMetadata(200.0));

        public static readonly DependencyProperty CategoryColumnWidthProperty =
            CategoryColumnWidthPropertyKey.DependencyProperty;

        public double MonthColumnWidth
        {
            get { return (double)GetValue(MonthColumnWidthProperty); }
            protected set { SetValue(MonthColumnWidthPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey MonthColumnWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("MonthColumnWidth", typeof(double), typeof(BudgetEditor), new PropertyMetadata(0.0));

        public static readonly DependencyProperty MonthColumnWidthProperty =
            MonthColumnWidthPropertyKey.DependencyProperty;

        public double MonthMarginLeft
        {
            get { return (double)GetValue(MonthMarginLeftProperty); }
            protected set { SetValue(MonthMarginLeftPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey MonthMarginLeftPropertyKey =
            DependencyProperty.RegisterReadOnly("MonthMarginLeft", typeof(double), typeof(BudgetEditor), new PropertyMetadata(0.0));

        public static readonly DependencyProperty MonthMarginLeftProperty =
            MonthMarginLeftPropertyKey.DependencyProperty;

        private void CategoryItemsScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double totalWidth = e.NewSize.Width - CategoryColumnWidth;

            int numberOfColumns = MaxNumberOfColumns(totalWidth);

            double totalMarginWidth = MONTH_MARGIN_WIDTH * (numberOfColumns - 1);
            totalWidth -= totalMarginWidth;

            MonthColumnWidth = totalWidth / numberOfColumns;
            if (this.DataContext is BudgetEditorViewModel viewModel)
            {
                viewModel.MakeNumberOfMonthsVisible(numberOfColumns);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
        }

        private int MaxNumberOfColumns(double totalWidth)
        {
            double runningWidth = MIN_MONTH_WIDTH;
            int columns = 1;
            if (totalWidth < MIN_MONTH_WIDTH)
            {
                return 1;
            }

            while (runningWidth + MIN_MONTH_WIDTH + MONTH_MARGIN_WIDTH <= totalWidth)
            {
                columns++;
                runningWidth += MIN_MONTH_WIDTH + MONTH_MARGIN_WIDTH;
            }

            return columns;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                TextBox textBox = e.OriginalSource as TextBox;
                BudgetEditorCategoryMonth monthView = FindParent(textBox);
                BudgetEditorCategoryRow categoryRow = monthView.FindParent<BudgetEditorCategoryRow>();
                if (textBox != null && monthView != null && categoryRow != null)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        e.Handled = NavigatePreviousCategoryMonth(textBox, categoryRow, monthView);
                    }
                    else
                    {
                        e.Handled = NavigateNextCategoryMonth(textBox, categoryRow, monthView);
                    }
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private bool NavigatePreviousCategoryMonth(TextBox textBox, BudgetEditorCategoryRow row, BudgetEditorCategoryMonth monthView)
        {
            if (!(this.DataContext is BudgetEditorViewModel editorViewModel)) return false;
            if (!(row.DataContext is CategoryRowViewModel rowViewModel)) return false;
            if (!(monthView.DataContext is CategoryMonthViewModel monthViewModel)) return false;

            if (rowViewModel == rowViewModel.MasterCategory.Categories.First() && rowViewModel.MasterCategory == editorViewModel.MasterCategories.First())
            {
                var previousCategoryMonth = FindPreviousCategoryMonth(editorViewModel, monthViewModel);
                var previousTextBox = FindAmountBudgetedTextBox(previousCategoryMonth);
                if (previousTextBox != null)
                {
                    previousTextBox.Focus();
                    previousTextBox.BringIntoView();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Up);
                textBox.MoveFocus(request);
                return true;
            }
        }

        private bool NavigateNextCategoryMonth(TextBox textBox, BudgetEditorCategoryRow row, BudgetEditorCategoryMonth monthView)
        {
            if (!(this.DataContext is BudgetEditorViewModel editorViewModel)) return false;
            if (!(row.DataContext is CategoryRowViewModel rowViewModel)) return false;
            if (!(monthView.DataContext is CategoryMonthViewModel monthViewModel)) return false;

            var lastRow = rowViewModel.MasterCategory.Categories[rowViewModel.MasterCategory.Categories.Count - 1];

            if (rowViewModel == rowViewModel.MasterCategory.Categories.Last() && rowViewModel.MasterCategory == editorViewModel.MasterCategories.Last())
            {
                var nextCategoryMonth = FindNextCategoryMonth(editorViewModel, monthViewModel);
                var nextTextBox = FindAmountBudgetedTextBox(nextCategoryMonth);
                if (nextTextBox != null)
                {
                    nextTextBox.Focus();
                    nextTextBox.BringIntoView();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
                textBox.MoveFocus(request);
                return true;
            }
        }

        private TextBox FindAmountBudgetedTextBox(CategoryMonthViewModel categoryMonth)
        {
            var masterCategory = categoryMonth.CategoryRowViewModel.MasterCategory;
            var category = categoryMonth.CategoryRowViewModel;

            BudgetEditorMasterCategoryRow masterCategoryRow = CategoryItemsControl.ItemContainerGenerator.ContainerFromItem(masterCategory).FindChild<BudgetEditorMasterCategoryRow>();
            BudgetEditorCategoryRow categoryRow = masterCategoryRow.ItemContainerGenerator.ContainerFromItem(category).FindChild<BudgetEditorCategoryRow>();
            var categoryMonthControl = categoryRow.ItemContainerGenerator.ContainerFromItem(categoryMonth).FindChild<BudgetEditorCategoryMonth>();
            return categoryMonthControl.FindName("AmountBudgetedTextbox") as TextBox;
        }

        private CategoryMonthViewModel FindPreviousCategoryMonth(BudgetEditorViewModel editorViewModel, CategoryMonthViewModel currentCategoryMonth)
        {
            var budgetMonthViewModel = currentCategoryMonth.BudgetMonthViewModel;
            int index = editorViewModel.VisibleMonthViews.IndexOf(budgetMonthViewModel);
            int count = editorViewModel.VisibleMonthViews.Count;

            int nextMonthIndex;
            if (index - 1 == -1)
            {
                nextMonthIndex = count - 1;
            }
            else
            {
                nextMonthIndex = index - 1;
            }
            BudgetMonthViewModel previousMonthViewModel = editorViewModel.VisibleMonthViews[nextMonthIndex];

            var lastMasterCategory = editorViewModel.MasterCategories.Last(mc => mc.Categories.Count >= 1);
            var lastCategory = lastMasterCategory.Categories[lastMasterCategory.Categories.Count - 1];

            return lastCategory.CategoryMonthViews.Single(cmv => cmv.BudgetMonthViewModel == previousMonthViewModel);
        }

        private CategoryMonthViewModel FindNextCategoryMonth(BudgetEditorViewModel editorViewModel, CategoryMonthViewModel currentCategoryMonth)
        {
            var budgetMonthViewModel = currentCategoryMonth.BudgetMonthViewModel;
            int index = editorViewModel.VisibleMonthViews.IndexOf(budgetMonthViewModel);
            int count = editorViewModel.VisibleMonthViews.Count;

            int nextMonthIndex;
            if (index + 1 == count)
            {
                nextMonthIndex = 0;
            }
            else
            {
                nextMonthIndex = Math.Min(index + 1, count - 1);
            }
            BudgetMonthViewModel nextMonthViewModel = editorViewModel.VisibleMonthViews[nextMonthIndex];

            var firstMasterCategory = editorViewModel.MasterCategories.First(mc => mc.Categories.Count >= 1);
            var firstCategory = firstMasterCategory.Categories[0];

            return firstCategory.CategoryMonthViews.Single(cmv => cmv.BudgetMonthViewModel == nextMonthViewModel);
        }

        private BudgetEditorCategoryMonth FindParent(TextBox textBox)
        {
            if (textBox == null) return null;
            return textBox.FindParent<BudgetEditorCategoryMonth>();
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_dragHandler != null)
                _dragHandler = null;

            var position = e.GetPosition(this);
            DependencyObject child = null;
            if (position != null)
            {
                child = VisualTreeHelper.HitTest(this, position)?.VisualHit;
            }

            var popupButton = child.FindParent<PopupButton>();
            if (_popupButton != null)
            {
                if (popupButton == null || popupButton != _popupButton)
                {
                    _popupButton.IsPopupOpen = false;
                    _popupButton = null;
                }
            }
        }

        private PopupButton _popupButton;

        private void OnRowEditorOpened(object sender, PopupButtonOpenedEventArgs e)
        {
            if (_popupButton != null && _popupButton != e.PopupButton)
            {
                _popupButton.IsPopupOpen = false;
                _popupButton = null;
            }
            _popupButton = e.PopupButton;
        }

        private BudgetEditorDragDropHandler _dragHandler;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            if (position.X <= CategoryColumnWidth)
            {
                _dragHandler = BudgetEditorDragDropHandler.CreateFromEvent(this, e);
            }
            else
            {
                _dragHandler?.DestroyDragAdorner();
                _dragHandler = null;
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (_dragHandler != null && e.LeftButton == MouseButtonState.Pressed && !_dragHandler.IsDragging)
            {
                var newPoint = e.GetPosition(this);
                var distance = Point.Subtract(newPoint, _dragHandler.DragStartPosition);
                if (distance.Length > 4.0)
                {
                    if (_popupButton != null)
                    {
                        _popupButton.IsPopupOpen = false;
                        _popupButton = null;
                    }
                    _dragHandler.StartDrag();
                    _dragHandler.DestroyDragAdorner();
                    _dragHandler = null;
                }
            }
        }

        protected override void OnPreviewDragEnter(DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BudgetEditorDragDropHandler).FullName) == null) return;

            DoDragOver(e);
        }

        protected override void OnPreviewDragOver(DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BudgetEditorDragDropHandler).FullName) == null) return;

            DoDragOver(e);
        }

        private void DoDragOver(DragEventArgs e)
        {
            _dragHandler.UpdateDrag(e);
        }

        protected override void OnPreviewDragLeave(DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BudgetEditorDragDropHandler).FullName) == null) return;

            _dragHandler.DestroyDragAdorner();
            e.Handled = true;
        }

        protected override void OnPreviewDrop(DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BudgetEditorDragDropHandler).FullName) == null) return;

            _dragHandler.OnDrop(e);
        }

        internal void HandleDrop(BudgetEditorDragDropHandler dropInfo)
        {
            if (dropInfo.DragType != BudgetEditorDragTypes.MasterCategory) throw new InvalidOperationException();

            if (this.DataContext is BudgetEditorViewModel viewModel)
            {
                var masterCategoryRow = dropInfo.MasterCategoryRow.DataContext as MasterCategoryRowViewModel;
                masterCategoryRow.MasterCategory.SetSortOrder(dropInfo.InsertPosition);
                viewModel.BudgetModel.SaveChanges();

                viewModel.MasterCategories.ForceSort();
            }
        }

        /*public bool CanStartDrag(IDragInfo dragInfo)
        {
            var hitTest = VisualTreeHelper.HitTest(dragInfo.VisualSource, dragInfo.DragStartPosition);
            if (hitTest == null) return false;

            var categoryRow = hitTest.VisualHit.FindParent<BudgetEditorCategoryRow>();
            if (categoryRow != null)
                return true;

            var masterCategoryRow = hitTest.VisualHit.FindParent<BudgetEditorMasterCategoryRow>();
            if (masterCategoryRow != null)
                return true;

            return false;
        }*/
    }


}
