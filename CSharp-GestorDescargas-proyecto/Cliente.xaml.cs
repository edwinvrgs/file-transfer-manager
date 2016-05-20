using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace CSharp_GestorDescargas_proyecto
{
    /// <summary>
    /// Interaction logic for Cliente.xaml
    /// </summary>
    public partial class Cliente : Window
    {
        //Atributos referentes a la conexion
        public static readonly int MAXPARTS = 20;
        public static readonly int MINPARTS = 1;
        public static string HOST = "127.0.0.1";
        public static readonly int PORT = 20001;

        //Atributos referentes a los archivos
        private ObservableCollection<Item> archivos_compartidos;

        //Metodo delegado
        private delegate void Delegado_Actualizar(string[] data);

        //Manejadores de la conexion
        private NetworkStream stream;
        private BinaryReader reader;
        private BinaryWriter writer;

        //Socket
        private TcpClient handler;

        //Para los archivos a recibir
        private string ruta_guardar;

        private ObservableCollection<Descarga> descargas;
        public ObservableCollection<Descarga> Descargas
        {
            get { return descargas; }
            set { descargas = value; }
        }

        //Para los hilos
        private bool reading;
        public bool Reading
        {
            get { return reading; }
            set { reading = value; }
        }

        public Cliente()
        {
            InitializeComponent();
            Show();

            //Instaciamos la lista para los archivos
            archivos_compartidos = new ObservableCollection<Item>();

            //Instaciamos la lista para las descargas
            descargas = new ObservableCollection<Descarga>();

            reading = true;
        }

        //Metodo a llamar por el hilo PROPIO del cliente
        public void IniciarCliente()
        {
            //Establecer conexion
            handler = new TcpClient(HOST, PORT);

            //Obtener el flujo de datos
            stream = handler.GetStream();

            //Crear el lector y el escritor a partir del flujo de datos
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
            
            //Se inicia el hilo para manejar las lecturas
            Leer();
        }
        
        //Metodo que se encarga de manejar las lecturas
        public void Leer()
        {
            while (reading)
            {
                //Leer
                string lectura = reader.ReadString();
                //Procesar
                ProcesarLectura(lectura);
            }

            //Cuando se cierre el cliente
            handler.Close();
        }

        private void ProcesarLectura(string lectura)
        {
            //Verificar que se haya leido algo valido
            if (lectura == null)
                return;

            //Separar el mensaje
            string[] aux = lectura.Split(Mensajes.SEPARADOR.ToCharArray());

            //Verificamos si el mensaje recibido tiene que ver con la lista
            if (aux[1].Equals(Mensajes.LISTA_) || aux[1].Equals(Mensajes.ACTUALIZAR_))
            {
                string[] data = aux[2].Split(Mensajes.SEPARADOR_AUX.ToCharArray());
                ActualizarArchivos(data);
            }
        }

        //Metodo DELEGADO para actualizar la lista de archivos
        public void ActualizarArchivos(string[] data)
        {
            if (!Dispatcher.CheckAccess())
            {
                Delegado_Actualizar delegado = new Delegado_Actualizar(ActualizarArchivos);
                object[] args = {data};

                Dispatcher.Invoke(delegado, args);
            }
            else
            {
                archivos_compartidos.Clear();

                foreach (string dato in data)
                    if(dato.Length > 3)
                        archivos_compartidos.Add(new Item(dato));

                ActualizarListBoxArchivos(archivos_compartidos);
            }
        }

        //Metodo para actualizar el ListBox
        private void ActualizarListBoxArchivos(ObservableCollection<Item> lista)
        {
            lista_archivos.ItemsSource = lista;
        } 
        
        //Metodo para actualizar el ListBox
        private void ActualizarListBoxDescargas(ObservableCollection<Descarga> lista)
        {
            lista_descargas.ItemsSource = lista;
        }

        //Metodo que maneja el evento del doble click (descarga)
        void ListArchivosDoubleClick(object sender, RoutedEventArgs e)
        {
            Forms.FolderBrowserDialog file_manager = new Forms.FolderBrowserDialog();
            file_manager.Description = "Seleccione el lugar donde se guardará el archivo: ";

            //Si la seleccion es valida
            if (file_manager.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Guardar la ruta seleccionada
                ruta_guardar = file_manager.SelectedPath;

                Item item = lista_archivos.SelectedItem as Item;
                
                //Informacion de la descarga
                string ruta = item.Ruta;
                int particiones = 3; //PENDIENTE!! (particiones != 0)

                if (item != null)
                {
                    writer.Write(Mensajes.SOLICITUD + Mensajes.SEPARADOR +
                        ruta + Mensajes.SEPARADOR_AUX +
                        particiones);

                    Descarga nueva_descarga = new Descarga(Path.Combine(ruta_guardar, item.Nombre), particiones);
                    descargas.Add(nueva_descarga);

                    ActualizarListBoxDescargas(descargas);

                    Thread hilo = new Thread(nueva_descarga.SolicitarTransferencia);
                    hilo.SetApartmentState(ApartmentState.STA);
                    hilo.Start();

                    Forms.MessageBox.Show("¡Descarga iniciada!\n" +
                                           " Se han creado " + particiones + 
                                           " particiones por defecto");
                }
            }
        }
        
        void ClickEliminarParticion(object sender, RoutedEventArgs e)
        {
            //Todo este rollo es para obtener al objeto al que pertenece este boton
            DependencyObject parent = (sender as Button).TemplatedParent;

            object aux = VisualTreeHelper.GetParent(parent);
            aux = VisualTreeHelper.GetParent(aux as DependencyObject);
            aux = (aux as ListBoxItem).Content;

            Descarga descarga = (aux as Descarga);
            
            if (descarga.Partes.Count == MINPARTS)
            {
                Forms.MessageBox.Show("Ya ha alcanzado el mínimo de particiones");
                return;
            }

            if (descarga.Finalizada)
            {
                Forms.MessageBox.Show("La descarga ya ha finalizado");
                return;
            }

            descarga.EliminarParte(false);
            EliminarParte(descarga.Item.Nombre);
        }

        void ClickAgregarParticion(object sender, RoutedEventArgs e)
        {
            //Todo este rollo es para obtener al objeto al que pertenece este boton
            DependencyObject parent = (sender as Button).TemplatedParent;

            object aux = VisualTreeHelper.GetParent(parent);
            aux = VisualTreeHelper.GetParent(aux as DependencyObject);
            aux = (aux as ListBoxItem).Content;

            Descarga descarga = (aux as Descarga);

            if (descarga.Partes.Count == MAXPARTS)
            {
                Forms.MessageBox.Show("Ya ha alcanzado el límite de particiones");
                return;
            }

            if (descarga.Finalizada)
            {
                Forms.MessageBox.Show("La descarga ya ha finalizado");
                return;
            }

            descarga.AgregarParte(false);
            AgregarParte(descarga.Item.Nombre);
        }

        void AgregarParte(string nombre)
        {
            writer.Write(Mensajes.ADD + Mensajes.SEPARADOR +
                nombre);
        }

        void EliminarParte(string nombre)
        {
            writer.Write(Mensajes.REMOVE + Mensajes.SEPARADOR +
                nombre);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            reading = false;
        }
    }
}
