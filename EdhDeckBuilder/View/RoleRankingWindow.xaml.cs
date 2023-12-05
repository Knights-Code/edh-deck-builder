using EdhDeckBuilder.ViewModel;
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
using System.Windows.Shapes;

namespace EdhDeckBuilder.View
{
    /// <summary>
    /// Interaction logic for RoleRankingWindow.xaml
    /// </summary>
    public partial class RoleRankingWindow : Window
    {
        public RoleRankingWindow()
        {
            InitializeComponent();
            CardList.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;

            if (key != Key.LeftShift) return;

            var vm = DataContext as RoleRankingsViewModel;

            if (vm.SelectedCard == null) return;

            vm.SelectedCard.RefreshCardImage();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key;

            if (key != Key.LeftShift) return;

            var vm = DataContext as RoleRankingsViewModel;

            if (vm.SelectedCard == null) return;

            vm.SelectedCard.RefreshCardImage();
        }

        private void CardList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UIElement selectedElement = (UIElement)CardList.ItemContainerGenerator.ContainerFromItem(CardList.SelectedItem);
            if (selectedElement != null)
            {
                selectedElement.Focus();
            }

            e.Handled = false;
        }
    }
}
