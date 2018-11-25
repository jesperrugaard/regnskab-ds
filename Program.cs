using System;
using System.IO.Compression;
using System.IO;
using Nest;
using Elasticsearch.Net;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper;


namespace RegnskabsHenter
{
    class Program
    {
        private static HttpClient client = null;
        static void Main(string[] args)
        {
            RegnskabConfig config = new RegnskabConfig();

            if (!config.InitializeProgram(args))
            {
                return;
            };

            Console.WriteLine("Starter Hent Regnskaber ...");

            Guid g = Guid.NewGuid();
            var koerselskatalog = Directory.CreateDirectory(config.TempDirectory + "/" + config.RunName + DateTime.Now.ToShortDateString() + "-" + g);
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);
            client.BaseAddress = config.RegnskabsUri;
            //Handle lines
            StreamWriter writer = File.CreateText(path: koerselskatalog.FullName + "/koersel.csv");
            using (var csv = new CsvWriter(writer))
            {

                int startFrom = 0;
                List<InfoLine> lines = FetchAndTreatDocuments(startFrom, config, koerselskatalog);
                csv.WriteRecords(lines);

                while (lines.Count == config.PageSize)
                {
                    startFrom += config.PageSize;
                    lines = FetchAndTreatDocuments(startFrom, config, koerselskatalog);
                    csv.WriteRecords(lines);

                }
            }
            //WriteCSVFile();
            ZipFile.CreateFromDirectory(koerselskatalog.FullName, koerselskatalog.FullName + ".zip");
            File.Move(koerselskatalog.FullName + ".zip", config.TargetDirectory);
        }

        public static List<InfoLine> FetchAndTreatDocuments(int startFrom, RegnskabConfig config, DirectoryInfo koerselskatalog)
        {
            List<InfoLine> list = new List<InfoLine>();

            var docs = HentFraES(startFrom, config.PageSize, config.ErstDistUri, config.StartDato, config.SlutDato);
            foreach (var virksomhed in docs)
            {
                InfoLine line = new InfoLine();
                line.CVRNUMMER = virksomhed.cvrNummer;
                line.PeriodeStart = virksomhed.regnskab.regnskabsperiode.startDato;
                line.PeriodeSlut = virksomhed.regnskab.regnskabsperiode.slutDato;
                line.UID = virksomhed.indlaesningsId;

                string uniktnavn = virksomhed.sagsNummer + "-" + virksomhed.cvrNummer;
                Console.WriteLine(uniktnavn);
                var sagskatalog = Directory.CreateDirectory(koerselskatalog.FullName + "/" + uniktnavn);
                foreach (var regnskab in virksomhed.dokumenter)
                {
                    if (regnskab.dokumentMimeType.ToLower().Contains("application/pdf"))
                    {
                        line.PDFDokument = regnskab.dokumentUrl;
                    }
                    else if (regnskab.dokumentMimeType.ToLower().Contains("application/xml"))
                    {
                        String filename = sagskatalog.FullName + "\" + regnskab.dokumentType + regnskab.dokumentUrl.Substring(regnskab.dokumentUrl.LastIndexOf("."));
                        var t = GetFileAsync(regnskab, uniktnavn, sagskatalog);
                        t.Wait();
                        line.XbrlDokument = filename;
                    }
                    Console.WriteLine(regnskab.dokumentUrl);
                }
                list.Add(line);

            }
            return list;
        }

        public static IReadOnlyCollection<Indberetning> HentFraES(int startFrom, int pageSize, Uri erstDist, DateTime from, DateTime to)
        {
            ConnectionSettings con_settings = new ConnectionSettings(erstDist)
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

        public static async Task GetFileAsync(Dokumenter regnskab, string filename, DirectoryInfo sagskatalog)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(regnskab.dokumentMimeType));
            HttpResponseMessage response = await client.GetAsync(regnskab.dokumentUrl);

            if (response.IsSuccessStatusCode)
            {
                System.Net.Http.HttpContent content = response.Content;
                var contentStream = await content.ReadAsStreamAsync(); // get the actual content stream
                using (var fileStream = File.Create(filename))
                {
                    contentStream.CopyTo(fileStream);
                }
            }
            else
            {
                //Log this as an error
                throw new FileNotFoundException();
            }
            return;
        }
    }
}
