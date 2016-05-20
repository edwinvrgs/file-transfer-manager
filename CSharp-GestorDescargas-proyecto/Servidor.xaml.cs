using System;
using Forms = System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;

namespace CSharp_GestorDescargas_proyecto
{
    /// <summary>
    /// Interaction logic for Servidor.xaml
    /// </summary>
    public partial class Servidor : Window
    {
        //Mensajes

        //Parametros de la conexion
        private readonly int PORT = 20001;

        //Atributos referentes a los archivos
        private ObservableCollection<Item> archivos_compartidos;
        private string ruta_archivos;

        //Metodo delegado
        private delegate void Delegado_Actualizar();

        //Clientes conectados
        private Dictionary<TcpClient, Descarga> clientes;

        //Manejadores de la conexion
        private TcpClient handler;
        private TcpListener listener;
        private TcpListener listener_descargas;

        //Para los hilos
        private bool listening;
        public bool Listening
        {
            get { return listening; }
            set { listening = value; }
        }

        private bool alive;
        public bool Alive
        {
            get { return alive; }
            set { alive = value; }
        }

        private bool reading;
        public bool Reading
        {
            get { return reading; }
            set { reading = value; }
        }

        public Servidor()
        {
            InitializeComponent();
            Show();

            //Obtener ruta por defecto (Mis Documentos)
            ruta_archivos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            //Obtener los archivos en la ruta por defecto
            archivos_compartidos = LoadFiles(ruta_archivos);

            //Asignar la lista al ListBox
            ActualizarListBox(archivos_compartidos);

            //Crear e inciar la conexion
            listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            Console.WriteLine("Servidor iniciado en el puerto: " + PORT.ToString());

            //Crear e inciar la conexion para descargas
            listener_descargas = new TcpListener(IPAddress.Any, PORT + 1);
            listener_descargas.Start();

            //Instanciar la lista de clientes
            clientes = new Dictionary<TcpClient, Descarga>();

            listening = true;
            alive = true;
            reading = true;
        }

        //Metodo a llamar por el hilo PROPIO del servidor
        public void IniciarServidor()
        {
            //Creacion de los hilos alternos para manejar otras cosas
            new Thread(EstarPilas).Start();
            new Thread(Leer).Start();

            while (alive)
            {
                //Verificar si hay cambios en la lista de archivos
                ActualizarArchivos();
                RevisarClientes();
            }
        }

        //Metodo a ejecutar por el hilo auxiliar para esperar multiples conexiones
        public void EstarPilas()
        {
            while (listening)
            { 
                if (!listener.Pending())
                {
                    Thread.Sleep(300); 
                    continue; 
                }

                //Esperar la conexion
                Console.WriteLine("Esperando conexión");
                handler = listener.AcceptTcpClient();
                Console.WriteLine("Conexión recibida");

                //Asegurarse que nadie mas maneje la lista en este momento
                lock (clientes)
                {
                    clientes.Add(handler, null);
                    EscribirParaTodos(Mensajes.LISTA);
                }
            }
            
            /*listener.Stop();
            listener_descargas.Stop();*/
        }

        private void RevisarClientes()
        {
            lock (clientes)
            {
                for (int i = 0; i < clientes.Keys.Count; i++)
                {
                    TcpClient cliente = System.Linq.Enumerable.ToArray(clientes.Keys)[i];

                    if (!cliente.Connected)
                    {
                        clientes[cliente].Finalizada = true;
                        clientes.Remove(cliente);
                    }
                }
            }
        }

        private void Leer()
        {
            while (reading)
            {
                //Asegurarse que nadie mas maneje la lista en este momento
                lock (clientes)
                {
                    //Recorrer todos los clientes
                    for (int i = 0; i < clientes.Keys.Count; i++)
                    {
                        TcpClient cliente = System.Linq.Enumerable.ToArray(clientes.Keys)[i];
                        //Si hay datos disponibles
                        if (cliente.GetStream().DataAvailable)
                        {
                            //Obtener el lector a partir del flujo de datos
                            BinaryReader reader = new BinaryReader(cliente.GetStream());
                            string lectura = reader.ReadString();

                            //Procesar lectura
                            ProcesarLectura(lectura, cliente);
                        }
                    }
                }
            }
        }

        private void ProcesarLectura(string lectura, TcpClient cliente)
        {
            //Verificar que se haya leido algo valido
            if (lectura == null)
                return;

            //Primera separacion
            string[] aux = lectura.Split(Mensajes.SEPARADOR.ToCharArray());

            //Verificar si es una solicitud
            if (aux[1].Equals(Mensajes.SOLICITUD_))
            {
                //Segunda separacion
                string[] data = aux[2].Split(Mensajes.SEPARADOR_AUX.ToCharArray());

                Descarga nueva_descarga = new Descarga(data[0], Int32.Parse(data[1]));
                nueva_descarga.SetListener(listener_descargas);

                lock(clientes)
                    clientes[cliente] = nueva_descarga;
                
                Thread hilo = new Thread(nueva_descarga.IniciarTransferencia);
                hilo.SetApartmentState(ApartmentState.STA);
                hilo.Start();
            }
            else if (aux[1].Equals(Mensajes.ADD_) || aux[1].Equals(Mensajes.REMOVE_))
            {
                string data = aux[2];

                foreach (Descarga descarga in clientes.Values)
                    if(descarga != null)
                        if (descarga.Item.Nombre.Equals(data))
                        {
                            if (aux[1].Equals(Mensajes.ADD_))
                                descarga.AgregarParte(true);
                            else if (aux[1].Equals(Mensajes.REMOVE_))
                                descarga.EliminarParte(true);
                        }
            }
        }

        //Metodo para actualizar la lista de archivos
        public void ActualizarArchivos()
        {
            if (!Dispatcher.CheckAccess())
            {
                Delegado_Actualizar delegado = new Delegado_Actualizar(ActualizarArchivos);
                object[] args = { };

                Dispatcher.Invoke(delegado, args);
            }
            else
            {
                //Lista nueva
                DirectoryInfo files_list = new DirectoryInfo(ruta_archivos);

                //Lista nueva vs. lista vieja
                if (files_list.GetFiles().Length != archivos_compartidos.Count)
                {
                    //Actualizar la lista
                    archivos_compartidos = LoadFiles(ruta_archivos);

                    //Avisar cambio
                    EscribirParaTodos(Mensajes.ACTUALIZAR);

                    //Metodo para acceder a la interfaz grafica desde otro hilo
                    ActualizarListBox(archivos_compartidos);
                }
            }
        }

        //Metodo para actualizar el ListBox
        private void ActualizarListBox(ObservableCollection<Item> lista)
        {
            lista_archivos.ItemsSource = lista;
        }

        public void Escribir(string mensaje, NetworkStream cliente)
        {
            BinaryWriter writer = new BinaryWriter(cliente);
            writer.Write(mensaje);
        }

        //Metodo para enviar informacion a todos los clientes
        public void EscribirParaTodos(string mensaje)
        {
            //Si el mensaje a enviar tiene que ver con la lista
            //Modificamos el mensaje para que envíe la misma
            if (mensaje.Equals(Mensajes.LISTA) || mensaje.Equals(Mensajes.ACTUALIZAR))
            {
                mensaje += Mensajes.SEPARADOR;

                foreach (Item aux in archivos_compartidos)
                    mensaje += aux.Ruta + Mensajes.SEPARADOR_AUX;
            }

            //Asegurarse que nadie mas maneje la lista en este momento
            lock (clientes)
            { 
                //Recorrer todos los clientes
                foreach (TcpClient cliente in clientes.Keys)
                {
                    //Obtener el escritor a partir del flujo de datos
                    BinaryWriter writer = new BinaryWriter(cliente.GetStream());
                    //Limpiar el flujo de datos
                    writer.Flush();
                    //Escribir el mensaje
                    writer.Write(mensaje);
                }
            }
        }

        //Evento del boton
        private void ClickBotonCarpeta(object sender, RoutedEventArgs e)
        {
            //Crear el explorador de directorios
            Forms.FolderBrowserDialog file_manager = new Forms.FolderBrowserDialog();
            file_manager.Description = "Seleccione la carpeta a compartir: ";

            //Si la seleccion es valida
            if (file_manager.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Guardar la ruta seleccionada
                ruta_archivos = file_manager.SelectedPath;
                //Obtener los archivos de dicha ruta
                archivos_compartidos = LoadFiles(file_manager.SelectedPath);

                //Actualizar la lista del ListBox
                ActualizarListBox(archivos_compartidos);
                EscribirParaTodos(Mensajes.ACTUALIZAR);
            }
        }

        //Metodo para obtener los archivos dada una ruta
        private ObservableCollection<Item> LoadFiles(string ruta)
        {
            ObservableCollection<Item> list = new ObservableCollection<Item>();

            DirectoryInfo files_list = new DirectoryInfo(ruta);

            foreach (FileInfo file in files_list.GetFiles())
                list.Add(new Item(file.FullName));
            
            return list;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            listening = false;
            alive = false;
            reading = false;
        }
    }
}
