using GistModulesLib;
using K4W2Accuracy.Model;
using K4W2Accuracy.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

namespace K4W2Accuracy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DepthImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition((Image)sender);
            var mpoint = new MousePoint { X = (int)point.X, Y = (int)point.Y };
            ((MainViewModel)this.DataContext).DepthClickCommand.Execute(mpoint);
        }

        private void ColorImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition((Image)sender);
            var mpoint = new MousePoint { X = (int)point.X, Y = (int)point.Y };
            ((MainViewModel)this.DataContext).ColorClickCommand.Execute(mpoint);
        }
    }

    public class MousePoint
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
