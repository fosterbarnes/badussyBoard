using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace BadussyBoard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<SoundItem> SoundItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            SoundItems = new ObservableCollection<SoundItem>();
            SoundDataGrid.ItemsSource = SoundItems;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: +");

            PickerWindow picker = new PickerWindow(this);
            picker.Owner = this;
            bool? result = picker.ShowDialog(); // blocks until window is closed
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: -");

            // Get the currently selected item in the DataGrid
            var selectedItem = SoundDataGrid.SelectedItem as SoundItem;
            if (selectedItem != null)
                SoundItems.Remove(selectedItem);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Edit");
            //TODO Add logic
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Play");
            //TODO Add logic
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Stop"); 
            //TODO Add logic
        }

        private void Levels_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Levels"); 
            //TODO Add logic
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Settings"); 
            //TODO Add logic
        }
    }
}