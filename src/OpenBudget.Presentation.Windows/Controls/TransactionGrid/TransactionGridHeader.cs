using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid
{
    public class TransactionGridHeader : Control
    {
        static TransactionGridHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TransactionGridHeader), new FrameworkPropertyMetadata(typeof(TransactionGridHeader)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var resizeThumb = this.GetTemplateChild("PART_ResizeThumb") as Thumb;
            if (resizeThumb != null)
            {
                resizeThumb.DragStarted += ResizeStarted;
                resizeThumb.DragDelta += ResizeDelta;
            }
        }

        private void ResizeDelta(object sender, DragDeltaEventArgs e)
        {
            this.Width += e.HorizontalChange;
        }

        private void ResizeStarted(object sender, DragStartedEventArgs e)
        {
            var resizeStartWidth = this.ActualWidth;
        }
    }
}
