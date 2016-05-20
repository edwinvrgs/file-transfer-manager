using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace CSharp_GestorDescargas_proyecto
{
    /// <summary>
    /// Interaction logic for AudioPlayer.xaml
    /// </summary>
    public partial class AudioPlayer : Window
    {
        private Player player;

        public AudioPlayer(Item item)
        {
            InitializeComponent();
            player = new Player(item);
            nombre_cancion.Content = item.Nombre;
            Show();
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Play();
        }

        private void button_pause_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Pause();
        }

        private void button_stop_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Stop();
        }
    }

    public class Player
    {
        private Item item;
        public Item Item
        {
            get { return item; }
            set { item = value; }
        }
        
        private string mediaName = "MediaFile";

        public Player(Item item)
        {
            string command = "open \"" + item.Ruta +
                                "\" type mpegvideo alias " + mediaName;
            mciSendString(command, null, 0, IntPtr.Zero);

            Console.WriteLine(item.Ruta);
        }

        public void Play()
        {
            string command = "play" + mediaName;
            mciSendString(command, null, 0, IntPtr.Zero);
        }

        public void Pause()
        {
            string command = "pause" + mediaName;
            mciSendString(command, null, 0, IntPtr.Zero);
        }

        public void Stop()
        {
            string command = "stop" + mediaName;
            mciSendString(command, null, 0, IntPtr.Zero);
        }

        [DllImport("winmm.dll")]
        private static extern long mciSendString(
            string lpstrCommand,
            StringBuilder lpstrReturnString,
            int uReturnLength,
            IntPtr hwndCallback);
    }
}
