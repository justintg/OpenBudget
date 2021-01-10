using OpenBudget.Presentation.Windows.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    /// <summary>
    /// Interaction logic for CategoryMonth.xaml
    /// </summary>
    public partial class BudgetEditorCategoryMonth : UserControl
    {
        public BudgetEditorCategoryMonth()
        {
            InitializeComponent();
        }

        private void AmountBudgetedTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            AmountBudgetedTextbox.SelectAll();
        }

        private void AmountBudgetedTextbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                var position = e.GetPosition(textBox);

                PopupButton popupButton = null;
                DependencyObject child = null;
                if (position != null)
                {
                    //child = VisualTreeHelper.HitTest(this, position)?.VisualHit;
                    VisualTreeHelper.HitTest(textBox, null, (result) =>
                    {
                        child = result.VisualHit;
                        popupButton = child.FindParent<PopupButton>();
                        if (popupButton != null)
                        {
                            return HitTestResultBehavior.Stop;
                        }
                        else
                        {
                            return HitTestResultBehavior.Continue;
                        }
                    }, new PointHitTestParameters(position));
                }

                if (popupButton == null)
                {
                    if (!textBox.IsKeyboardFocusWithin)
                    {
                        // If the text box is not yet focussed, give it the focus and
                        // stop further processing of this click event.
                        textBox.Focus();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
