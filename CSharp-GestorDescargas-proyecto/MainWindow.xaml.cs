using System;
using System.Collections.Generic;
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

namespace CSharp_GestorDescargas_proyecto
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

        private void button_salir_click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
            button_salir.IsEnabled = false;
        }

        private void button_server_click(object sender, RoutedEventArgs e)
        {
            Servidor server = new Servidor();
            new Thread(server.IniciarServidor).Start();
            //AudioPlayer player = new AudioPlayer(new Item(@"C:\Users\CAPOA\Desktop\Roar (from Monsters University).mp3")); 
        }

        private void button_client_click(object sender, RoutedEventArgs e)
        {
            Cliente server = new Cliente();
            new Thread(server.IniciarCliente).Start();
        }
    }
}
