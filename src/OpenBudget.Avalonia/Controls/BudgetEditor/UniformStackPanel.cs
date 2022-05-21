using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Avalonia.Controls.BudgetEditor
{
    public class UniformStackPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            int childCount = this.Children.Count;
            double childWidth = (double)((int)(finalSize.Width / childCount));
            double leftOver = finalSize.Width - childWidth * childCount;
            double horizonalOffset = 0.0;
            foreach(var child in Children)
            {
                double modifiedChildWidth = childWidth;
                if(leftOver > 0.0)
                {
                    modifiedChildWidth++;
                    leftOver--;
                }
                child.Arrange(new Rect(horizonalOffset, 0, modifiedChildWidth, finalSize.Height));
                horizonalOffset += modifiedChildWidth;
            }

            return finalSize;
        }
    }
}
