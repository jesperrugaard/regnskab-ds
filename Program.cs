using System;
using System.IO.Compression;
using System.IO;

namespace RegnskabsHenter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //Hent regnskab ...
            Guid g = Guid.NewGuid();
            var d = Directory.CreateDirectory("./"+ g);
            File.Copy("./Program.cs", "./"+g+"/x.cs");
            //Zip ting
            ZipFile.CreateFromDirectory("./"+g, "./"+g+".zip");
            if(File.Exists("./"+g+ ".zip")) {
                Console.WriteLine("Yes");
            }
            //Flyt ting


        }
    }
}
