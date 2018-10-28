namespace RegnskabsHenter
{
    public class Source
    {
        public string CvrNummer { get; set; }
        public string RegNummer { get; set; }
        public bool Omgoerelse { get; set; }
        public string SagsNummer { get; set; }
        public string Offentliggoerelsestype { get; set; }
        public Regnskab Regnskab { get; set; }
        public string offentliggoerelsesTidspunkt { get; set; }
        public string indlaesningsTidspunkt { get; set; }
        public string sidstOpdateret { get; set; }
        public Dokument[] dokumenter { get; set; }
        public string IndlaesningsId { get; set; }
    }
}