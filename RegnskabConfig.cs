using System;
using System.Collections.Generic;
using System.Configuration;

namespace RegnskabsHenter
{

    public class RegnskabConfig
    {

        public string Offentliggoerelse { get { return confValues["base_uri"]; } }
        public Uri RegnskabsUri { get; set; }
        public Uri ErstDistUri { get; set; }
        public string TempDirectory { get { return confValues["temp_directory"]; } }
        public string TargetDirectory { get { return confValues["target_directory"]; } }
        public string RunName { get { return confValues["name_of_run"]; } }
        public DateTime StartDato { get;set; }
        public DateTime SlutDato { get;set; }
        public int Threads { get; set; }
        public int Chunks { get;set; }
        public int PageSize { get;set; }
        public bool UseYesterDay { get;set; }

        private static Dictionary<String, String> confValues = new Dictionary<String, String>()
        {
            { "temp_directory","./" },
            { "target_directory","./" },
            { "name_of_run", "Koersel"},
            { "threads","4" },
            { "chunks","10" },
            { "page_size","2000" },
            { "base_uri","http://distribution.virk.dk" },
            { "start_date","01-01-2018" },
            { "end_date","02-01-2018" },
            { "use_yesterday", "true"}
        };

        public bool InitializeProgram(string[] args)
        {
            try {
            Dictionary<String, String> tempDict = new Dictionary<String, String>();
            var appSettings = ConfigurationManager.AppSettings;
            foreach (var confKey in confValues.Keys)
            {
                if (appSettings[confKey] != null)
                {
                    tempDict[confKey] = appSettings[confKey];
                }
            }
            //Overwrite with values from arguments
            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    var key = arg.Remove(arg.IndexOf('='));
                    var value = arg.Substring(arg.IndexOf('=')+1);
                    if (confValues.ContainsKey(key))
                    {
                        tempDict[key] = value;
                    }

                }
            }

            foreach (var key in tempDict.Keys)
            {
                confValues[key] = tempDict[key];
            }

           
            RegnskabsUri = new Uri(confValues["base_uri"]);
            ErstDistUri = new Uri(Offentliggoerelse);
            Threads = int.Parse( confValues["threads"]);
            PageSize = int.Parse(confValues["page_size"]);
            UseYesterDay = bool.Parse(confValues["use_yesterday"]);
            Chunks = int.Parse(confValues["chunks"]);


            if(UseYesterDay)
            {
                SlutDato = DateTime.Now.Date;
                StartDato =SlutDato.AddDays(-1);

            } else {
                SlutDato = DateTime.Parse(confValues["end_date"]);
                StartDato =  DateTime.Parse(confValues["start_date"]);
                if(!(SlutDato.CompareTo(StartDato) >= 0)) 
                {
                    throw new ArgumentOutOfRangeException("Start date must be before end date");
                }

            }
            return true;

            } 
            catch(Exception e) {
                System.Console.WriteLine(e);
                WriteUsage();
                return false;
            }

        }

        public void WriteUsage()
        {
            System.Console.WriteLine("Application cannot start. Please configuration values");
            foreach (var confValue in confValues)
            {
                System.Console.WriteLine(confValue.Key + " = " + confValue.Value);
            }
        }



    }

}