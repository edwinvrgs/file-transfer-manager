using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace CSharp_GestorDescargas_proyecto
{
    public class Particion : INotifyPropertyChanged
    {
        public static readonly int BUFFERSIZE = 1024 * 10;

        private Descarga parent;
        private byte[] buffer;
        private int ID;

        private TcpListener servidor;
        public TcpListener Servidor
        {
            get { return servidor; }
            set { servidor = value; }
        }

        private TcpClient cliente;
        public TcpClient Cliente
        {
            get { return cliente; }
            set { cliente = value; }
        }

        private FileStream file_stream;

        private bool finalizado;
        public bool Finalizado
        {
            get { return finalizado; }
            set { finalizado = value; }
        }

        private int velocidad;
        public int Velocidad
        {
            get { return velocidad; }
            set { velocidad = value; }
        }

        private int inicio;
        public int Inicio
        {
            get { return inicio; }
            set { inicio = value; }
        }

        private int fin;
        public int Fin
        {
            get { return fin; }
            set { fin = value; }
        }

        private int progreso;
        public int Progreso
        {
            get { return progreso; }
            set { progreso = value; }
        }

        private double completado;
        public double Completado
        {
            get { return completado; }
            set { completado = value; }
        }

        public Particion(Descarga parent, FileStream file_stream, int inicio, int fin, int ID,
            TcpListener servidor = null)
        {
            this.parent = parent;

            buffer = new byte[BUFFERSIZE];
            progreso = 0;
            velocidad = 0;

            this.inicio = inicio;
            this.fin = fin;

            this.ID = ID;

            this.file_stream = file_stream;
            this.servidor = servidor;

            finalizado = false;
        }

        public Particion(Descarga parent, FileStream file_stream, int ID,
            TcpClient cliente = null)
        {
            this.parent = parent;

            buffer = new byte[BUFFERSIZE];
            progreso = 0;
            velocidad = 0;

            this.ID = ID;

            this.file_stream = file_stream;
            this.cliente = cliente;

            finalizado = false;
        }

        //Solo se ejecuta en el servidor
        public void IniciarTransferencia()
        {
            cliente = servidor.AcceptTcpClient(); 
            BinaryWriter writer = new BinaryWriter(cliente.GetStream());

            lock (cliente)
            {
                writer.Write(inicio);
                writer.Write(fin);
            }

            int readed = 0;
            int bytes_readed = inicio;
            int bytes_enviados = 0;

            progreso = inicio;

            while (bytes_readed < fin)
            {
                lock (file_stream)
                {
                    file_stream.Seek(bytes_readed, SeekOrigin.Begin);
                    readed = file_stream.Read(buffer, 0, buffer.Length);
                    file_stream.Seek(0, SeekOrigin.Begin);
                }
                
                lock (cliente)
                    writer.Write(buffer, 0, readed);     

                bytes_readed += readed;
                bytes_enviados += readed;

                progreso += readed;

                if(readed == 0)
                    break;
                
                Console.WriteLine(readed + " bytes enviados...");
            }

            Console.WriteLine("{0} bytes enviados del hilo {1}", bytes_enviados, ID);
            Console.WriteLine("Terminó el envio");

            finalizado = true;
        }

        //Solo se ejecuta en el cliente
        public void SolicitarTransferencia()
        {
            Stopwatch stopWatch = new Stopwatch();
            BinaryReader reader = new BinaryReader(cliente.GetStream());

            lock (cliente)
            {
                inicio = reader.ReadInt32();
                NotifyPropertyChanged("Inicio");

                progreso = inicio;
                NotifyPropertyChanged("Progreso");

                fin = reader.ReadInt32();
                NotifyPropertyChanged("Fin");
            }

            int writed = 0;
            int bytes_writed = inicio;
            int bytes_recibidos = 0;

            stopWatch.Start();

            while (!finalizado)
            {
                lock (cliente)
                     writed= reader.Read(buffer, 0, buffer.Length);

                lock (file_stream)
                {
                    file_stream.Seek(bytes_writed, SeekOrigin.Begin);
                    file_stream.Write(buffer, 0, writed);
                    file_stream.Seek(0, SeekOrigin.Begin);
                }

                bytes_writed += writed;
                bytes_recibidos += writed;

                progreso += writed;
                NotifyPropertyChanged("Progreso");

                if (writed == 0)
                {
                    Console.WriteLine("{0} bytes recibidos del hilo {1}", bytes_recibidos, ID);
                    Console.WriteLine("Terminó la recepción");
                    UpdateInfo(0, null);
                    break;
                }

                UpdateInfo(bytes_recibidos, stopWatch);
                Console.WriteLine(writed + " bytes recibidos...");
            }

            stopWatch.Stop();
            finalizado = true;
        }

        public void UpdateInfo(int progreso, Stopwatch stopWatch)
        {
            if(progreso == 0)
            {
                completado = 100;
                NotifyPropertyChanged("Completado");

                velocidad = 0;
                NotifyPropertyChanged("Velocidad");

                return;
            }

            if(fin - inicio != 0)
                completado = Convert.ToInt32((Convert.ToDouble(progreso) / Convert.ToDouble(fin - inicio)) * (100));
            NotifyPropertyChanged("Completado");

            if (stopWatch != null)
                if (stopWatch.Elapsed.Seconds > 0)
                    velocidad = Convert.ToInt32((progreso / 1024) / (stopWatch.Elapsed.TotalSeconds));

            NotifyPropertyChanged("Velocidad");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
