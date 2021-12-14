using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using OpenBudget.Application.ViewModels.BudgetEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public class BudgetEditor : TemplatedControl
    {
        private const double MONTH_MARGIN_WIDTH = 5.0;
        private const double MIN_MONTH_WIDTH = 250.0;
        private const double CATEGORY_COLUMN_DEFAULT_WIDTH = 200.0;

        public double CategoryColumnWidth
        {
            get { return this.GetValue<double>(CategoryColumnWidthProperty); }
            set { this.SetValue<double>(CategoryColumnWidthProperty, value); }
        }

        public static readonly StyledProperty<double> CategoryColumnWidthProperty = AvaloniaProperty.Register<BudgetEditor, double>(nameof(CategoryColumnWidth), defaultValue: CATEGORY_COLUMN_DEFAULT_WIDTH);

        private double _monthColumnWidth = 0.0;

        public double MonthColumnWidth
        {
            get { return _monthColumnWidth; }
            protected set { SetAndRaise(MonthColumnWidthProperty, ref _monthColumnWidth, value); }
        }

        public static readonly DirectProperty<BudgetEditor, double> MonthColumnWidthProperty
            = AvaloniaProperty.RegisterDirect<BudgetEditor, double>(nameof(MonthColumnWidth), o => o.MonthColumnWidth);

        private double _monthMarginLeft = MONTH_MARGIN_WIDTH;

        public double MonthMarginLeft
        {
            get { return _monthMarginLeft; }
            protected set { SetAndRaise(MonthMarginLeftProperty, ref _monthMarginLeft, value); }
        }

        public static readonly DirectProperty<BudgetEditor, double> MonthMarginLeftProperty
            = AvaloniaProperty.RegisterDirect<BudgetEditor, double>(nameof(MonthMarginLeft), o => o.MonthMarginLeft);

        static BudgetEditor()
        {
            BoundsProperty.Changed.AddClassHandler<BudgetEditor>(OnBoundsChanged);
        }

        private static void OnBoundsChanged(BudgetEditor editor, AvaloniaPropertyChangedEventArgs arg2)
        {
            editor.OnRenderSizeChanged();
        }

        public BudgetEditor()
        {
        }

        private void OnRenderSizeChanged()
        {
            double totalWidth = Bounds.Width - CategoryColumnWidth;

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
    }
}
