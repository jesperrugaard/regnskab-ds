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
using log4net;
using System.Reflection;
using log4net.Config;

namespace RegnskabsHenter
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static HttpClient client = null;
        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("app.config"));
            _log.Info("Regnskabsindlæsning startet.");
            RegnskabConfig config = new RegnskabConfig();
           
            if (!config.InitializeProgram(args))
            {
                return;
            };
            _log.Info("Config indlæst.");
            Guid g = Guid.NewGuid();
            var koerselskatalog = Directory.CreateDirectory(config.TempDirectory + "/" + config.RunName +"-"+ DateTime.Now.ToShortDateString() + "--" + g);
            _log.Info("Dannet temp-katalog: " + koerselskatalog.FullName);
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);
            client.BaseAddress = config.RegnskabsUri;
           
            StreamWriter writer = File.CreateText(path: koerselskatalog.FullName + "/index.csv");
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
                startFrom += lines.Count;
                _log.Info("Har indlæst " + startFrom + " regnskaber");
            }
            _log.Info("Zipper: " + koerselskatalog.FullName );
            ZipFile.CreateFromDirectory(koerselskatalog.FullName, koerselskatalog.FullName + ".zip");
             _log.Info("Flytter til: " + config.TargetDirectory );
            File.Copy(koerselskatalog.FullName + ".zip", config.TargetDirectory + koerselskatalog.Name + ".zip");
            File.SetAttributes(koerselskatalog.FullName, FileAttributes.Normal);
            Directory.Delete(koerselskatalog.FullName, true);
            File.Delete(koerselskatalog.Name + ".zip");         
            _log.Info("Slettet temp-filer og afslutter");
        }

        public static List<InfoLine> FetchAndTreatDocuments(int startFrom, RegnskabConfig config, DirectoryInfo koerselskatalog)
        {
            List<InfoLine> list = new List<InfoLine>();

            var docs = HentFraES(startFrom, config.PageSize, config.ErstDistUri, config.StartDato, config.SlutDato);
            _log.Info("Modtaget fra ES: " + docs.Count+ " dokumenter, for datoer: " + config.StartDato  +"/"+ config.SlutDato );
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
                        string shortname =  regnskab.dokumentType + regnskab.dokumentUrl.Substring(regnskab.dokumentUrl.LastIndexOf(".")); 
                        String filename = sagskatalog.FullName + "\\" + shortname;
                        var t = GetFileAsync(regnskab, filename, sagskatalog);
                        t.Wait();
                        line.XbrlDokument = "\\"+ uniktnavn + "\\" + shortname;
                    }
                    Console.WriteLine(regnskab.dokumentUrl);
                }
                if(String.IsNullOrEmpty(line.XbrlDokument))
                {
                    _log.Info(line.CVRNUMMER + "/" + uniktnavn+  " har ingen xbrl-fil tilknyttet");
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
                _log.Error("Kunne ikke hente fil: " + filename);
                //Log this as an error
                throw new FileNotFoundException(response.ReasonPhrase);
            }
            return;
        }
    }
}
