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
using RemoteMate.Views;

namespace RemoteMate.Views
{
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow(string info)
        {
            InitializeComponent();
            txtInfo.Text = info;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
