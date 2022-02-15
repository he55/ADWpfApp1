using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ADWpfApp1
{
    public class ListBoxDragDropManager
    {
        ListBox listBox;
        int sel;

        UIElement _originalElement;
        SimpleCircleAdorner _overlayElement;
        Point _startPoint;

        public Action<int, bool, string> DataAction;

        public ListBoxDragDropManager(ListBox listBox)
        {
            this.listBox = listBox;
            this.listBox.DragEnter += ListBox_DragEnter;
            this.listBox.DragOver += ListBox_DragOver;
            this.listBox.Drop += ListBox_Drop;
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            _startPoint = Mouse.GetPosition(this.listBox);
            //DragStarted();
        }

        void ListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            sel = GetIndexUnderDragCursor();

            //DragMoved();
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (lastListBoxItem != null)
                ListBoxItemDragState.SetIsUnderDragCursor(lastListBoxItem, false);

            //DragFinished();

            if (sel != -1)
            {
                DataObject data = (DataObject)e.Data;
                string v = data.GetText();
                if (!string.IsNullOrEmpty(v))
                {
                    DataAction.Invoke(sel, false, v);
                    return;
                }

                StringCollection stringCollection = data.GetFileDropList();
                if (stringCollection.Count != 0)
                {
                    DataAction.Invoke(sel, true, stringCollection[0]);
                    return;
                }
            }
        }


        ListBoxItem lastListBoxItem;
        private int GetIndexUnderDragCursor()
        {
            ListBoxItem overItem = null;
            int index = -1;
            for (int i = 0; i < this.listBox.Items.Count; ++i)
            {
                ListBoxItem item = (ListBoxItem)this.listBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (this.IsMouseOver(item))
                {
                    overItem = item;
                    index = i;
                    break;
                }
            }

            if (lastListBoxItem != overItem)
            {
                if (overItem != null)
                    ListBoxItemDragState.SetIsUnderDragCursor(overItem, true);

                if (lastListBoxItem != null)
                    ListBoxItemDragState.SetIsUnderDragCursor(lastListBoxItem, false);

                lastListBoxItem = overItem;
            }

            return index;
        }

        bool IsMouseOver(Visual target)
        {
            // We need to use MouseUtilities to figure out the cursor
            // coordinates because, during a drag-drop operation, the WPF
            // mechanisms for getting the coordinates behave strangely.

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }




        void DragFinished()
        {
            AdornerLayer.GetAdornerLayer(_overlayElement.AdornedElement).Remove(_overlayElement);
        }

        void DragStarted()
        {
            _overlayElement = new SimpleCircleAdorner(_originalElement);
            var layer = AdornerLayer.GetAdornerLayer(_originalElement);
            layer.Add(_overlayElement);
        }

        void DragMoved()
        {
            var currentPosition = Mouse.GetPosition(this.listBox);

            _overlayElement.LeftOffset = currentPosition.X - _startPoint.X;
            _overlayElement.TopOffset = currentPosition.Y - _startPoint.Y;
        }
    }


    public static class ListBoxItemDragState
    {
        public static readonly DependencyProperty IsUnderDragCursorProperty =
            DependencyProperty.RegisterAttached(
                "IsUnderDragCursor",
                typeof(bool),
                typeof(ListBoxItemDragState),
                new UIPropertyMetadata(false));

        public static bool GetIsUnderDragCursor(ListBoxItem item)
        {
            return (bool)item.GetValue(IsUnderDragCursorProperty);
        }

        internal static void SetIsUnderDragCursor(ListBoxItem item, bool value)
        {
            item.SetValue(IsUnderDragCursorProperty, value);
        }
    }
}
