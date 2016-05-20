using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace CSharp_GestorDescargas_proyecto
{
    public class Descarga : INotifyPropertyChanged
    {
        private Item item;
        public Item Item
        {
            get { return item; }
            set { item = value; }
        }

        private int num_particiones;
        public int NumParticiones
        {
            get { return num_particiones; }
            set { num_particiones = value; }
        }

        private int progreso;
        public int Progreso
        {
            get { return progreso; }
            set { progreso = value; }
        }

        private bool finalizada;
        public bool Finalizada
        {
            get { return finalizada; }
            set {
                if (value)
                {
                    foreach (Particion parte in partes)
                    {
                        parte.Fin = 0;
                        parte.Finalizado = true;
                    }
                }

                finalizada = value; 
            }
        }

        private ObservableCollection<Particion> partes;
        public ObservableCollection<Particion> Partes
        {
            get { return partes; }
            set { partes = value; }
        }

        private TcpListener listener;
        public TcpListener Listener
        {
            get { return listener; }
            set { listener = value; }
        }

        private FileStream file_stream;

        public Descarga()
        {
            item = null;
            progreso = 0;
            num_particiones = 0;
            partes = new ObservableCollection<Particion>();

            finalizada = false;
        }

        public Descarga(string ruta, int num_particiones)
        {
            item = new Item(ruta);

            progreso = 0;
            this.num_particiones = num_particiones;
            partes = new ObservableCollection<Particion>();

            finalizada = false;
        }

        public void SetListener(TcpListener listener)
        {
            this.listener = listener;
        }

        public void IniciarTransferencia()
        {
            file_stream = new FileStream(item.Ruta, FileMode.OpenOrCreate, FileAccess.Read);
            int tamaño_paquetes = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(file_stream.Length) / Convert.ToDouble(num_particiones)));
            
            for (int i = 0; i < num_particiones; i++)
            { 
                Particion nueva_parte = new Particion(this, file_stream, i * tamaño_paquetes, (i + 1) * tamaño_paquetes, i, listener);
                partes.Add(nueva_parte);
                NotifyPropertyChanged("Partes");

                new Thread(nueva_parte.IniciarTransferencia).Start();
            }

            while (!TransferenciaFinalizada());

            finalizada = true;
            NotifyPropertyChanged("Finalizada");

            lock(partes)
                foreach (Particion part in partes)
                    part.Cliente.Close();

            file_stream.Close();
        }

        public void SolicitarTransferencia()
        {
            file_stream = new FileStream(item.Ruta, FileMode.OpenOrCreate, FileAccess.Write);
            
            for (int i = 0; i < num_particiones; i++)
            {
                TcpClient cliente = new TcpClient(Cliente.HOST, Cliente.PORT + 1);

                Particion nueva_parte = new Particion(this, file_stream, i, cliente);

                lock (partes) 
                    partes.Add(nueva_parte);

                NotifyPropertyChanged("Partes");

                new Thread(nueva_parte.SolicitarTransferencia).Start();
            }

            while (!TransferenciaFinalizada());

            finalizada = true;
            NotifyPropertyChanged("Finalizada");

            lock (partes)
                foreach (Particion part in partes)
                    part.Cliente.Close();

            file_stream.Close();

            //Para abrir el archivo que se ha descargado
            if(item.Extension.Equals("mp3") || 
                item.Extension.Equals("wav"))
            {
                AudioPlayer player = new AudioPlayer(item);
                System.Windows.Threading.Dispatcher.Run();
            }
            else if (item.Extension.Equals("jpg") || 
                item.Extension.Equals("png") || 
                item.Extension.Equals("gif"))
            {
                ImagePlayer player = new ImagePlayer(item);
                System.Windows.Threading.Dispatcher.Run();
            }
            else
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = item.Ruta;
                    process.Start();
                    process.WaitForExit();
                }
                catch (Win32Exception)
                {
                    MessageBox.Show("No hay aplicación por defecto para abrir el archivo descargado");
                }
            }
        }

        public void AgregarParte(bool is_server)
        {
            Int64 menor = Int64.MaxValue;
            Particion aux = null;
            
            foreach (Particion part in partes)
                if (part.Progreso < menor)
                {
                    aux = part;
                    menor = part.Progreso;
                }

            int nuevo_fin = 0;
            int viejo_fin = 0;

            lock(aux)
            {
                int fin = aux.Fin;
                int progreso = aux.Progreso;

                int mitad = (fin - progreso) / 2;
                viejo_fin = aux.Fin;
                nuevo_fin = aux.Fin - mitad;

                aux.Fin = nuevo_fin;

                aux.NotifyPropertyChanged("Fin");
            }

            lock (partes)
            {
                if (is_server)
                {
                    Particion nueva_parte = new Particion(this, file_stream, nuevo_fin, viejo_fin, partes.Count, listener);
                    partes.Add(nueva_parte); partes.Add(nueva_parte);
                    NotifyPropertyChanged("Partes");

                    new Thread(nueva_parte.IniciarTransferencia).Start();
                }
                else
                {
                    TcpClient cliente = new TcpClient(Cliente.HOST, Cliente.PORT + 1);
                    Particion nueva_parte = new Particion(this, file_stream, partes.Count, cliente);
                    partes.Add(nueva_parte);
                    NotifyPropertyChanged("Partes");

                    new Thread(nueva_parte.SolicitarTransferencia).Start();
                }
            }
        }

        public void EliminarParte(bool is_server)
        {
            Particion to_delete = partes[partes.Count - 1];
            Particion new_end = partes[partes.Count - 2];
            
            lock(partes)
            {
                lock (new_end)
                {
                    new_end.Fin = to_delete.Fin;
                    new_end.NotifyPropertyChanged("Fin");
                }

                lock (to_delete)
                {
                    to_delete.Fin = to_delete.Progreso;
                    to_delete.Finalizado = true;
                    to_delete.NotifyPropertyChanged("Fin");

                }

                partes.Remove(to_delete);
            }

            NotifyPropertyChanged("Partes");
        }

        public bool TransferenciaFinalizada()
        {
            /*bool check = true;
            List<Particion> to_delete = new List<Particion>();*/

            lock(partes)
            {
                foreach (Particion part in partes)
                {
                    if (!part.Finalizado)
                        return false;
                    /*else
                        to_delete.Add(part);*/
                }

                /*Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    foreach (Particion part in to_delete)
                        partes.Remove(part);
                }));*/
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
