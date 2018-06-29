using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Presentation.Windows.Util;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            double totalWidth = sizeInfo.NewSize.Width - CategoryColumnWidth;

            int numberOfColumns = MaxNumberOfColumns(totalWidth);

            double totalMarginWidth = MONTH_MARGIN_WIDTH * (numberOfColumns - 1);
            totalWidth -= totalMarginWidth;

            MonthColumnWidth = totalWidth / numberOfColumns;
            if (this.DataContext is BudgetEditorViewModel viewModel)
            {
                viewModel.MakeNumberOfMonthsVisible(numberOfColumns);
            }
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
                        TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Up);
                        (e.OriginalSource as UIElement)?.MoveFocus(request);
                        e.Handled = true;
                    }
                    else
                    {
                        //e.Handled = NavigateNextCategoryMonth(textBox, categoryRow, monthView);
                    }
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private bool NavigateNextCategoryMonth(TextBox textBox, BudgetEditorCategoryRow row, BudgetEditorCategoryMonth monthView)
        {
            if (!(this.DataContext is BudgetEditorViewModel editorViewModel)) return false;
            if (!(row.DataContext is CategoryRowViewModel rowViewModel)) return false;
            if (!(monthView.DataContext is CategoryMonthViewModel monthViewModel)) return false;

            var lastRow = rowViewModel.MasterCategory.Categories[rowViewModel.MasterCategory.Categories.Count - 1];

            if (rowViewModel == rowViewModel.MasterCategory.Categories.Last() && rowViewModel.MasterCategory == editorViewModel.MasterCategories.Last())
            {
                return false;
            }
            else
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
                textBox.MoveFocus(request);
                return true;
            }
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
    }
}
