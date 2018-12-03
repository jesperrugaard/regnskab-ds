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
using System.Linq;

namespace RegnskabsHenter
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static HttpClient client = null;
        private static ElasticClient es_client;

        static void Main(string[] args)
        {
            DateTime start = DateTime.Now;
            System.Console.WriteLine("Kørsel starter "+ start.ToShortTimeString());
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("app.config"));

            RunSimpleExtraction(args);
            DateTime slut = DateTime.Now;
            _log.Info("### Kørsel varede " + slut.Subtract(start).TotalSeconds + " sekunder ###");
            System.Console.WriteLine("Kørsel slutter "+ slut.ToShortTimeString() + " ialt: " + slut.Subtract(start).TotalSeconds + " sekunder" );

            
        }

        private static void RunSimpleExtraction(string[] args)
        {
           
            _log.Info("### Regnskabsindlæsning startet.###");
            RegnskabConfig config = new RegnskabConfig();

            if (!config.InitializeProgram(args))
            {
                return;
            };
            _log.Info("Config indlæst.");
            Guid g = Guid.NewGuid();
            var koerselskatalog = Directory.CreateDirectory(config.TempDirectory + "/" + DateTime.Now.ToShortDateString() + "-" + config.RunName + "-" + g);
            _log.Info("Dannet temp-katalog: " + koerselskatalog.Name    );
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler);
            client.BaseAddress = config.RegnskabsUri;
            ConnectionSettings con_settings = new ConnectionSettings(config.ErstDistUri)
                .DefaultIndex("offentliggoerelser")
                .DefaultTypeName("_doc");

            es_client = new ElasticClient(con_settings);
            StreamWriter writer = File.CreateText(path: koerselskatalog.FullName + "/index.csv");
            using (var csv = new CsvWriter(writer))
            {
                List<InfoLine> lines = FetchAndTreatDocuments("", config, koerselskatalog);
                csv.WriteRecords(lines);
            }

            _log.Info("Zipper: " + koerselskatalog.FullName);
            ZipFile.CreateFromDirectory(koerselskatalog.FullName, koerselskatalog.FullName + ".zip");
            _log.Info("Flytter til: " + config.TargetDirectory);
            File.Copy(koerselskatalog.FullName + ".zip", config.TargetDirectory + koerselskatalog.Name + ".zip");
            File.SetAttributes(koerselskatalog.FullName, FileAttributes.Normal);
            Directory.Delete(koerselskatalog.FullName, true);
            File.Delete(koerselskatalog.FullName + ".zip");
            _log.Info("### Slettet temp-filer og afslutter ###");
            
        }


        public static List<InfoLine> FetchAndTreatDocuments(string scrollId, RegnskabConfig config, DirectoryInfo koerselskatalog)
        {
            List<Indberetning> docs = GetAllDocumentsInIndex(config.StartDato, config.SlutDato);
            _log.Info("Modtaget fra ES: " + docs.Count + " dokumenter, for datoer: " + config.StartDato + "/" + config.SlutDato);
            List<InfoLine> resultList = new List<InfoLine>();
           
            for (int i = 0; i < docs.Count; i += config.Chunks)
            {
                int chunk = i + config.Chunks <= docs.Count ? config.Chunks : docs.Count % config.Chunks;
                _log.Info("Downloader næste " + chunk + "/" + i);
                IEnumerable<Task<InfoLine>> downloadTasks =
                from virksomhed in docs.GetRange(i, chunk) select FetchDocument(virksomhed, koerselskatalog) ;

                Task<InfoLine>[] downloads = downloadTasks.ToArray();

                // Await the completion of all the running tasks.  
                List<InfoLine> list = Task.WhenAll(downloads).Result.ToList();
                resultList.AddRange(list);
            }
            return resultList;
        }

        private static async Task<InfoLine> FetchDocument(Indberetning virksomhed, DirectoryInfo koerselskatalog)
        {
            InfoLine line = new InfoLine();
            line.CVRNUMMER = virksomhed.cvrNummer;
            line.PeriodeStart = virksomhed.regnskab.regnskabsperiode.startDato;
            line.PeriodeSlut = virksomhed.regnskab.regnskabsperiode.slutDato;
            line.UID = virksomhed.indlaesningsId;

            string uniktnavn = virksomhed.sagsNummer + "-" + virksomhed.cvrNummer;
            foreach (var regnskab in virksomhed.dokumenter)
            {
                if (regnskab.dokumentMimeType.ToLower().Contains("application/pdf"))
                {
                    line.PDFDokument = regnskab.dokumentUrl;
                }
                else if (regnskab.dokumentMimeType.ToLower().Contains("application/xml"))
                {
                    string shortname = uniktnavn + "-" + regnskab.dokumentType + regnskab.dokumentUrl.Substring(regnskab.dokumentUrl.LastIndexOf("."));
                    String filename = koerselskatalog.FullName + "/" + shortname;
                    await GetFileAsync(regnskab, filename, koerselskatalog);
                    line.XbrlDokument = "\\" + shortname;
                }

            }
            if (String.IsNullOrEmpty(line.XbrlDokument))
            {
                _log.Info(line.CVRNUMMER + "/" + uniktnavn + " har ingen xbrl-fil tilknyttet");
            }
            return line;
        }


        protected static List<Indberetning> GetAllDocumentsInIndex(DateTime from, DateTime to, string scrollTimeout = "2m", int scrollSize = 2000)
        {
            //The thing to know about scrollTimeout is that it resets after each call to the scroll so it only needs to be big enough to stay alive between calls.
            //when it expires, elastic will delete the entire scroll.
            var initialResponse = es_client.Search<Indberetning>
                (scr => scr
                 .Scroll("3m")
                 .From(0)
                 .Take(scrollSize)
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
            List<Indberetning> results = new List<Indberetning>();
            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);
            if (initialResponse.Documents.Any())
                results.AddRange(initialResponse.Documents);
            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<Indberetning> loopingResponse = es_client.Scroll<Indberetning>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    results.AddRange(loopingResponse.Documents);
                    scrollid = loopingResponse.ScrollId;
                }
                isScrollSetHasData = loopingResponse.Documents.Any();
            }
            //This would be garbage collected on it's own after scrollTimeout expired from it's last call but we'll clean up our room when we're done per best practice.
            es_client.ClearScroll(new ClearScrollRequest(scrollid));
            return results;
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
