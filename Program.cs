using System;
using System.IO.Compression;
using System.IO;
using Nest;

namespace RegnskabsHenter
{
    class Program
    {
        const string offentliggoerelse = "http://distribution.virk.dk";
        static private Uri erst_dist = new Uri(offentliggoerelse);
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //Hent regnskab ...
            ConnectionSettings con_settings = new ConnectionSettings(erst_dist).DefaultIndex("offentliggoerelser");
            var client = new ElasticClient(con_settings);
            //
            var result = client.Search<Indberetning>();
            var docs = result.Documents;
            Guid g = Guid.NewGuid();
            var d = Directory.CreateDirectory("./" + g);
            File.Copy("./Program.cs", "./" + g + "/x.cs");
            //Zip ting
            ZipFile.CreateFromDirectory("./" + g, "./" + g + ".zip");
            if (File.Exists("./" + g + ".zip"))
            {
                Console.WriteLine("Yes");
            }
            //Flyt ting


        }
    }
}
