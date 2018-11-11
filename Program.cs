using System;
using System.IO.Compression;
using System.IO;
using Nest;
using Elasticsearch.Net;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegnskabsHenter
{
    class Program
    {

        private static readonly string offentliggoerelse = "http://distribution.virk.dk";
        private static readonly Uri RegnskabsUri = new Uri("http://regnskaber.virk.dk");
        private static readonly Uri erst_dist = new Uri(offentliggoerelse);

        private static readonly int pageSize = 100;

        private static HttpClient client = null;
        static void Main(string[] args)
        {
            Console.WriteLine("Starter Hent Regnskaber ...");

            Guid g = Guid.NewGuid();
            var d = Directory.CreateDirectory("./Regnskabskørsel-" + DateTime.Now.ToShortDateString() + "-" + g);

            int startFrom = 10;
            var docs = HentFraES(startFrom, pageSize, new DateTime(2018, 9, 1), new DateTime(2018, 9, 3));
            
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);
            client.BaseAddress = RegnskabsUri;
            foreach (var virksomhed in docs)
            {
                string uniktnavn = virksomhed.sagsNummer + "-" + virksomhed.cvrNummer;
                Console.WriteLine(uniktnavn);
                var sagskatalog = Directory.CreateDirectory(d.FullName + "/" + uniktnavn);
                foreach (var regnskab in virksomhed.dokumenter)
                {
                    var t = GetFileAsync(regnskab, uniktnavn, sagskatalog);
                    t.Wait();
                    Console.WriteLine(regnskab.dokumentUrl);
                }

                ZipFile.CreateFromDirectory(sagskatalog.FullName, sagskatalog.FullName + uniktnavn + ".zip");

            }

        }

        public static IReadOnlyCollection<Indberetning> HentFraES(int startFrom, int pageSize, DateTime from, DateTime to)
        {
            ConnectionSettings con_settings = new ConnectionSettings(erst_dist)
                .DefaultIndex("offentliggoerelser")
                .DefaultTypeName("_doc");

            var client = new ElasticClient(con_settings);
            var result = client.Search<Indberetning>(s => s
               .From(startFrom)
               .Size(pageSize)
               .Query(q => q
                   .Bool(b => b
                       .Must(m => m
                           .DateRange(dr => dr
                                .Field(f => f.indlaesningsTidspunkt)
                                .GreaterThanOrEquals(from)
                                .LessThanOrEquals(to)
                       )
                   )
               )));
            return result.Documents;
        }

        public static async Task GetFileAsync(Dokumenter regnskab, string uniktnavn, DirectoryInfo sagskatalog)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(regnskab.dokumentMimeType));
            HttpResponseMessage response = await client.GetAsync(regnskab.dokumentUrl);

            if (response.IsSuccessStatusCode)
            {
                System.Net.Http.HttpContent content = response.Content;
                var contentStream = await content.ReadAsStreamAsync(); // get the actual content stream
               
                using (var fileStream = File.Create(sagskatalog.FullName + "/" + regnskab.dokumentType + regnskab.dokumentUrl.Substring(regnskab.dokumentUrl.LastIndexOf("."))))
                {
                    contentStream.CopyTo(fileStream);
                }
            }
            else
            {
                //Log this as an error
                throw new FileNotFoundException();
            }
            return ;
        }
    }
}
