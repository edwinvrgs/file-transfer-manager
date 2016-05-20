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

namespace CSharp_GestorDescargas_proyecto
{
    /// <summary>
    /// Interaction logic for ImagePlayer.xaml
    /// </summary>
    public partial class ImagePlayer : Window
    {
        public ImagePlayer(Item item)
        {
            InitializeComponent();
            imagen.Source = new BitmapImage(new Uri(item.Ruta));
            Show();
        }
    }
}
