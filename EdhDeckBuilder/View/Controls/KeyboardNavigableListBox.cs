using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EdhDeckBuilder.View.Controls
{
    internal class KeyboardNavigableListBox : ListBox
    {
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            var container = (UIElement)ItemContainerGenerator.ContainerFromItem(SelectedItem);

            if (container != null)
            {
                container.Focus();
            }
        }
    }
}
