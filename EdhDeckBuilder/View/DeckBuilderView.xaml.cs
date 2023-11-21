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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EdhDeckBuilder.View
{
    /// <summary>
    /// Interaction logic for DeckBuilderView.xaml
    /// </summary>
    public partial class DeckBuilderView : UserControl
    {
        public DeckBuilderView()
        {
            InitializeComponent();
        }

        private void CardView_MouseEnter(object sender, MouseEventArgs e)
        {
            var cardView = sender as CardView;
            var cardVm = cardView.DataContext as CardViewModel;
            cardVm.Hovered = true;

            var vm = DataContext as DeckBuilderViewModel;
            vm.HoveredCardVm = cardVm;
        }

        private void CardView_MouseLeave(object sender, MouseEventArgs e)
        {
            var cardView = sender as CardView;
            var cardVm = cardView.DataContext as CardViewModel;
            cardVm.Hovered = false;

            var vm = DataContext as DeckBuilderViewModel;
            vm.HoveredCardVm = null;
        }
    }
}
