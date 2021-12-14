using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public class BudgetEditorPanel : Panel
    {
        static BudgetEditorPanel()
        {
        }

        public BudgetEditorPanel()
        {
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _budgetEditor = this.FindAncestorOfType<BudgetEditor>();
        }

        private BudgetEditor _budgetEditor;

        protected BudgetEditor FindBudgetEditor()
        {
            return _budgetEditor;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var budgetEditor = FindBudgetEditor();
            Size layoutSlotSize = new Size(budgetEditor.MonthColumnWidth, Double.PositiveInfinity);
            double height = 0.0;
            foreach (var child in Children)
            {
                child.Measure(layoutSlotSize);
                if (child.DesiredSize.Height > height)
                {
                    height = child.DesiredSize.Height;
                }
            }
            return availableSize.WithHeight(height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var budgetEditor = FindBudgetEditor();
            var columnWidth = budgetEditor.MonthColumnWidth;
            var marginLeft = budgetEditor.MonthMarginLeft;
            double horizontalOffset = 0.0;
            for (int i = 0; i < Children.Count; i++)
            {
                if (i > 0)
                {
                    horizontalOffset += budgetEditor.MonthMarginLeft;
                }

                var child = Children[i];
                child.Arrange(new Rect(horizontalOffset, 0, columnWidth, finalSize.Height));
                horizontalOffset += columnWidth;
            }
            return finalSize;
        }
    }
}
