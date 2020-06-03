/*
 * Parts of this code has been adapted from GongSolutions.WPF.DragDrop
 * Original source can be found here: https://github.com/punker76/gong-wpf-dragdrop
 */
using GongSolutions.Wpf.DragDrop.Utilities;
using MahApps.Metro.Controls;
using OpenBudget.Presentation.Windows.Controls.DragDrop;
using OpenBudget.Presentation.Windows.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    internal enum BudgetEditorDragTypes
    {
        MasterCategory,
        Category
    }

    internal class BudgetEditorDragDropHandler
    {
        public static BudgetEditorDragDropHandler CreateFromEvent(BudgetEditor budgetEditor, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(budgetEditor);
            var hitTest = VisualTreeHelper.HitTest(budgetEditor, point);

            var categoryRow = hitTest?.VisualHit.FindParent<BudgetEditorCategoryRow>();
            if (categoryRow != null)
            {
                return new BudgetEditorDragDropHandler(budgetEditor, categoryRow, point);
            }

            var masterCategoryRow = hitTest?.VisualHit.FindParent<BudgetEditorMasterCategoryRow>();
            if (masterCategoryRow != null)
            {
                return new BudgetEditorDragDropHandler(budgetEditor, masterCategoryRow, point);
            }

            return null;
        }

        private BudgetEditorDragDropHandler(BudgetEditor budgetEditor, BudgetEditorMasterCategoryRow masterCategoryRow, Point dragStartPosition)
        {
            this.BudgetEditor = budgetEditor;
            this.DragType = BudgetEditorDragTypes.MasterCategory;
            this.MasterCategoryRow = masterCategoryRow;
            this.DragStartPosition = dragStartPosition;
        }

        private BudgetEditorDragDropHandler(BudgetEditor budgetEditor, BudgetEditorCategoryRow categoryRow, Point dragStartPosition)
        {
            this.BudgetEditor = budgetEditor;
            this.DragType = BudgetEditorDragTypes.Category;
            this.CategoryRow = categoryRow;
            this.DragStartPosition = dragStartPosition;
        }

        public BudgetEditor BudgetEditor { get; private set; }

        public bool IsDragging { get; set; }
        public Point DragStartPosition { get; private set; }
        public BudgetEditorDragTypes DragType { get; private set; }
        public BudgetEditorCategoryRow CategoryRow { get; private set; }
        public BudgetEditorMasterCategoryRow MasterCategoryRow { get; private set; }

        public ContentPresenter DragVisual { get; private set; }

        public FrameworkElement DraggingUIElement
        {
            get
            {
                return (FrameworkElement)CategoryRow ?? MasterCategoryRow;
            }
        }

        private DropTargetInsertAdorner _dropTargetAdorner;

        public DropTargetInsertAdorner DropTargetAdorner
        {
            get { return _dropTargetAdorner; }
            set
            {
                _dropTargetAdorner?.Detach();
                _dropTargetAdorner = value;
            }
        }

        private DragAdorner _dragAdorner;

        public DragAdorner DragAdorner
        {
            get { return _dragAdorner; }
            private set
            {
                _dragAdorner?.Detatch();
                _dragAdorner = value;
            }
        }

        public Point AdornerMousePosition;
        public Size AdornerSize;

        public int InsertPosition { get; set; }
        public BudgetEditorMasterCategoryRow TargetMasterCategory { get; private set; }

        public void StartDrag()
        {
            IsDragging = true;
            var result = System.Windows.DragDrop.DoDragDrop(BudgetEditor, this, DragDropEffects.Move);
            DropTargetAdorner = null;
            DragAdorner = null;
        }

        public void UpdateDrag(DragEventArgs e)
        {
            var categoryItemsControl = BudgetEditor.CategoryItemsControl;
            var position = e.GetPosition(categoryItemsControl);
            if (position.Y < 0 || position.X < 0 || position.Y > categoryItemsControl.ActualHeight || position.X > categoryItemsControl.ActualWidth)
            {
                DragAdorner = null;
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (DragAdorner == null)
            {
                DragAdorner = CreateDragAdorner();
            }

            var movePosition = e.GetPosition(DragAdorner.AdornedElement);
            DragAdorner?.Move(movePosition, new Point(0, 1), ref AdornerMousePosition, ref AdornerSize);

            UpdateDropTarget(categoryItemsControl, position);

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void UpdateDropTarget(ItemsControl categoryItemsControl, Point position)
        {
            if (DragType == BudgetEditorDragTypes.Category)
            {
                UpdateDropTargetForCategoryDrag(categoryItemsControl, position);
            }
            else if (DragType == BudgetEditorDragTypes.MasterCategory)
            {
                UpdateDropTargetForMasterCategoryDrag(categoryItemsControl, position);
            }
        }

        private void UpdateDropTargetForMasterCategoryDrag(ItemsControl categoryItemsControl, Point position)
        {
            var masterCategoryRow = categoryItemsControl.GetItemContainerAt(position).FindChild<BudgetEditorMasterCategoryRow>();
            if (masterCategoryRow != null)
            {
                var masterCategoryRowPoint = categoryItemsControl.TranslatePoint(position, masterCategoryRow);
                if (masterCategoryRowPoint.Y > masterCategoryRow.RenderSize.Height * 0.66)
                {
                    DropTargetAdorner = new DropTargetInsertAdorner(masterCategoryRow, true);
                }
                else
                {
                    DropTargetAdorner = new DropTargetInsertAdorner(masterCategoryRow, false);
                }
            }
        }

        private void UpdateDropTargetForCategoryDrag(ItemsControl categoryItemsControl, Point position)
        {
            var masterCategoryRow = categoryItemsControl.GetItemContainerAt(position).FindChild<BudgetEditorMasterCategoryRow>();
            if (masterCategoryRow != null)
            {
                var masterCategoryRowPoint = categoryItemsControl.TranslatePoint(position, masterCategoryRow);

                var categoryContainer = masterCategoryRow.GetItemContainerAt(categoryItemsControl.TranslatePoint(position, masterCategoryRow));
                if (categoryContainer != null)
                {
                    CategoryDragOverCategory(position, masterCategoryRow, categoryContainer);
                }
                else
                {
                    CategoryDragOverMasterCategoryHeader(masterCategoryRow, masterCategoryRowPoint);
                }
            }
            else
            {
                CategoryDragOverEmptyBottomSpace();
            }
        }

        private void CategoryDragOverEmptyBottomSpace()
        {
            var lastMasterCategory = GetLastMasterCategory();

            TargetMasterCategory = lastMasterCategory;
            InsertPosition = lastMasterCategory.ItemContainerGenerator.Items.Count;
            DropTargetAdorner = new DropTargetInsertAdorner(lastMasterCategory, true);
        }

        private void CategoryDragOverMasterCategoryHeader(BudgetEditorMasterCategoryRow masterCategoryRow, Point masterCategoryRowPoint)
        {
            if (IsFirstMasterCategory(masterCategoryRow))
            {
                var categoryRow = masterCategoryRow.ItemContainerGenerator.ContainerFromIndex(0)?.FindChild<BudgetEditorCategoryRow>();
                if (categoryRow != null)
                {
                    TargetMasterCategory = masterCategoryRow;
                    InsertPosition = 0;
                    DropTargetAdorner = new DropTargetInsertAdorner(categoryRow, false);
                }
            }
            else
            {
                if (IsHalfwayOverHeader(masterCategoryRow, masterCategoryRowPoint))
                {
                    var categoryRow = masterCategoryRow.ItemContainerGenerator.ContainerFromIndex(0)?.FindChild<BudgetEditorCategoryRow>();
                    if (categoryRow != null)
                    {
                        TargetMasterCategory = masterCategoryRow;
                        InsertPosition = 0;
                        DropTargetAdorner = new DropTargetInsertAdorner(categoryRow, false);
                    }
                }
                else
                {
                    var previousMasterCategory = GetPreviousMasterCategoryRow(masterCategoryRow);
                    var lastCategory = GetLastCategory(previousMasterCategory);
                    int length = previousMasterCategory.ItemContainerGenerator.Items.Count;

                    TargetMasterCategory = previousMasterCategory;
                    InsertPosition = length;

                    if (length == 0)
                    {
                        DropTargetAdorner = new DropTargetInsertAdorner(previousMasterCategory, true);
                    }
                    else
                    {
                        DropTargetAdorner = new DropTargetInsertAdorner(lastCategory, true);
                    }
                }
            }
        }

        private void CategoryDragOverCategory(Point categoryControlPosition, BudgetEditorMasterCategoryRow masterCategoryRow, UIElement categoryContainer)
        {
            var categoryItemsControl = BudgetEditor.CategoryItemsControl;
            BudgetEditorCategoryRow categoryRow = categoryContainer.FindChild<BudgetEditorCategoryRow>();
            var categoryRowPoint = categoryItemsControl.TranslatePoint(categoryControlPosition, categoryRow);

            InsertPosition = masterCategoryRow.ItemContainerGenerator.IndexFromContainer(VisualTreeHelper.GetParent(categoryRow));
            TargetMasterCategory = masterCategoryRow;
            bool isTargetNextRow = categoryRowPoint.Y > categoryRow.RenderSize.Height * 0.66;
            if (isTargetNextRow)
            {
                InsertPosition++;
                DropTargetAdorner = new DropTargetInsertAdorner(categoryRow, true);
            }
            else
            {
                DropTargetAdorner = new DropTargetInsertAdorner(categoryRow, false);
            }
        }

        private BudgetEditorMasterCategoryRow GetLastMasterCategory()
        {
            var itemsControl = BudgetEditor.CategoryItemsControl;
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(itemsControl.ItemContainerGenerator.Items.Count - 1);
            return container.FindChild<BudgetEditorMasterCategoryRow>();
        }

        private BudgetEditorCategoryRow GetLastCategory(BudgetEditorMasterCategoryRow masterCategoryRow)
        {
            int length = masterCategoryRow.ItemContainerGenerator.Items.Count;
            var container = masterCategoryRow.ItemContainerGenerator.ContainerFromIndex(length - 1);
            return container.FindChild<BudgetEditorCategoryRow>();
        }

        internal void OnDrop(DragEventArgs e)
        {
            UpdateDrag(e);

            if (DragType == BudgetEditorDragTypes.Category)
            {
                TargetMasterCategory.HandleDrop(this);
            }
        }

        private bool IsHalfwayOverHeader(BudgetEditorMasterCategoryRow masterCategoryRow, Point point)
        {
            var itemsPresenter = masterCategoryRow.FindChild<ItemsPresenter>();
            var headerHeight = masterCategoryRow.RenderSize.Height - itemsPresenter.RenderSize.Height;

            if (point.Y > headerHeight * 0.66)
            {
                return true;
            }

            return false;
        }

        private BudgetEditorMasterCategoryRow GetPreviousMasterCategoryRow(BudgetEditorMasterCategoryRow masterCategoryRow)
        {
            var itemsControl = BudgetEditor.CategoryItemsControl;
            var index = itemsControl.ItemContainerGenerator.IndexFromContainer(VisualTreeHelper.GetParent(masterCategoryRow));
            if (index >= 1)
            {
                var previousContainer = itemsControl.ItemContainerGenerator.ContainerFromIndex(index - 1);
                return previousContainer.FindChild<BudgetEditorMasterCategoryRow>();
            }

            return null;
        }

        private bool IsFirstMasterCategory(BudgetEditorMasterCategoryRow masterCategoryRow)
        {
            var itemsControl = BudgetEditor.CategoryItemsControl;
            var container = VisualTreeHelper.GetParent(masterCategoryRow);
            return itemsControl.ItemContainerGenerator.IndexFromContainer(container) == 0;
        }

        public void DestroyDragAdorner()
        {
            DragAdorner = null;
        }

        private DragAdorner CreateDragAdorner()
        {
            if (DragVisual == null)
            {
                DragVisual = CreateDragVisual();
            }

            var rootElement = Window.GetWindow(DraggingUIElement).Content as UIElement;
            DragAdorner adorner = new DragAdorner(rootElement, DragVisual, new Point(-4, -4));
            return adorner;
        }

        private ContentPresenter CreateDragVisual()
        {
            var adornedElement = DraggingUIElement;
            var dataTemplate = adornedElement.GetCaptureScreenDataTemplate(FlowDirection.LeftToRight);

            var contentPresenter = new ContentPresenter();
            contentPresenter.Content = DraggingUIElement.DataContext;
            contentPresenter.ContentTemplate = dataTemplate;
            contentPresenter.Opacity = 0.5;

            return contentPresenter;
        }
    }
}
