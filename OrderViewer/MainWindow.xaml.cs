using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using StuRaHsHarz.WebShop.Models;
using StuRaHsHarz.WebShop.Statistics;

namespace OrderViewer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            Orders = new ObservableCollection<Order>();
            InitializeComponent();
            LoadOrdersAsync();
        }

        private ImmutableList<Order> _orders = ImmutableList<Order>.Empty;
        public ObservableCollection<Order> Orders { get; }

        public bool OnlyGrey(Order order)
        {
            if (order.ShippingAddress is null) return false;
            if (order.Items.Count() <= 1) return false;

            bool differentColor = false;

            foreach (OrderItem orderItem1 in order.Items)
            {
                foreach (OrderItem orderItem2 in order.Items)
                {
                    if (orderItem1.Type.Color != orderItem2.Type.Color) return true;
                    
                    /*
                    if (orderItem.Type.Color != ItemColor.RED)
                    {
                        return false;
                    }*/
                }
            }

            return false;
        }

        public async void LoadOrdersAsync()
        {
            try
            {
                _orders = await StuRaHsHarz.WebShop.Statistics.Orders.ReadFromDirectoryAsync();
                Debug.WriteLine(_orders.Count);

                foreach (var order in _orders)
                {
                    if (order.Name.Contains("Sarah"))
                    {
                        Orders.Add(order);
                    }
                }

                Debug.WriteLine(Orders.Count);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Failed to load orders.");
                Debug.WriteLine(exception);
            }
        }

        //Copy id
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Order selectedOrder = Orders[OrderList.SelectedIndex];
            Clipboard.SetData(DataFormats.Text, selectedOrder.Id.ToString("B"));
        }

        //Copy OrderedItems
        private void MenuItem_Click2(object sender, RoutedEventArgs e)
        {
            Order selectedOrder = Orders[OrderList.SelectedIndex];
            Clipboard.SetData(DataFormats.Text, selectedOrder.OrderItemsAsString);
        }

        private void PrintOrder(Order order)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = @"R:\SturaWebshop\InfoPrinter\bin\Debug\InfoPrinter.exe",
            };

            p.StartInfo.ArgumentList.Add(order.Id.ToString("B"));

            foreach (OrderItem orderItem in order.Items)
            {
                p.StartInfo.ArgumentList.Add(orderItem.ToString());
            }

            p.Start();
        }

        //print
        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Order selectedOrder = Orders[OrderList.SelectedIndex];

            PrintOrder(selectedOrder);
        }
    }
}
