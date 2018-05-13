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

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid
{
    public class TransactionGridCell : ContentControl
    {
        private enum CellState
        {
            None,
            Normal,
            Editing
        }

        private CellState _cellState = CellState.None;

        static TransactionGridCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TransactionGridCell), new FrameworkPropertyMetadata(typeof(TransactionGridCell)));
        }

        public DataTemplate NormalTemplate
        {
            get { return (DataTemplate)GetValue(NormalTemplateProperty); }
            set { SetValue(NormalTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NormalTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NormalTemplateProperty =
            DependencyProperty.Register("NormalTemplate", typeof(DataTemplate), typeof(TransactionGridCell), new PropertyMetadata(null));

        public DataTemplate EditTemplate
        {
            get { return (DataTemplate)GetValue(EditTemplateProperty); }
            set { SetValue(EditTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditTemplateProperty =
            DependencyProperty.Register("EditTemplate", typeof(DataTemplate), typeof(TransactionGridCell), new PropertyMetadata(null));

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof(bool), typeof(TransactionGridCell), new PropertyMetadata(false, OnIsEditingChanged));

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cell = d as TransactionGridCell;
            cell.LoadCellContent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LoadCellContent();
        }

        private void LoadCellContent()
        {
            CellState newState = IsEditing ? CellState.Editing : CellState.Normal;

            if (_cellState == CellState.None || _cellState != newState)
            {
                DataTemplate cellTemplate = null;
                if (IsEditing)
                {
                    cellTemplate = EditTemplate;
                }
                else
                {
                    cellTemplate = NormalTemplate;
                }

                if (cellTemplate == null) throw new InvalidOperationException();

                var content = cellTemplate.LoadContent();
                this.Content = content;
                _cellState = newState;
            }
        }
    }
}
