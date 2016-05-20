using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CSharp_GestorDescargas_proyecto
{
    //Clase que representa un item a mostrar por el cliente o el servidor
    public class Item
    {
        private string nombre;
        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        private string tamaño;
        public string Tamaño
        {
            get { return tamaño; }
            set { tamaño = value; }
        }

        private string ruta;
        public string Ruta
        {
            get { return ruta; }
            set { ruta = value; }
        }

        private BitmapImage imagen;
        public BitmapImage Imagen
        {
            get { return imagen; }
            set { imagen = value; }
        }

        private string extension;
        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        private int download_count;
        public int DownloadCount
        {
            get { return download_count; }
            set { download_count = value; }
        }

        //Constructor que recibe el nombre y la ruta
        //Y a partir de esos dos datos a darle
        public Item(string ruta)
        {
            this.ruta = ruta;
            string[] aux = ruta.Split(@"\".ToCharArray());

            nombre = aux[aux.Length - 1];

            //Se extrae la extension del archivo a partir del nombre
            aux = nombre.Split(".".ToCharArray());
            extension = aux[1];

            //Se obtiene el directorio de las imagenes
            DirectoryInfo files_list = new DirectoryInfo(Environment.CurrentDirectory + @"\png");

            //Bandera para saber si se le asigno una imagen especifica
            bool check = false;

            //Por cada archivo .png
            foreach (FileInfo file in files_list.GetFiles("*.png"))
            {
                //Se compara dicho archivo con la extension
                if ((file.Name.Split(".".ToCharArray()))[0].Equals(extension))
                {
                    //Se le asigna la respectiva imagen
                    Imagen = new BitmapImage(new Uri(file.FullName));

                    //Notificar que ya se le asignó imagen
                    check = true;
                    break;
                }
            }

            try
            {
                FileStream file_stream = new FileStream(ruta, FileMode.Open);
                tamaño = (file_stream.Length / 1024) + " KB";
                file_stream.Close();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Excepcion");
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("Excepcion");
            }

            //Si no se le habia asignado, se le asigna una por defecto
            if (!check)
                Imagen = new BitmapImage(new Uri(files_list.FullName + @"\blank.png"));
        }
    }
}
