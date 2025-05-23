﻿using EdhDeckBuilder.ViewModel;
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
    /// Interaction logic for CardView.xaml
    /// </summary>
    public partial class CardView : UserControl
    {
        public CardView()
        {
            InitializeComponent();
        }

        private void CardView_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;

            if (key != Key.LeftShift) return;

            var vm = DataContext as CardViewModel;
            vm.RefreshCardImage();
        }

        private void CardView_KeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key;

            if (key != Key.LeftShift) return;

            var vm = DataContext as CardViewModel;
            vm.RefreshCardImage();
        }
    }
}
