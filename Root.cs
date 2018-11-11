using System;

public class Rootobject
{
    public int took { get; set; }
    public bool timed_out { get; set; }
    public _Shards _shards { get; set; }
    public Hits hits { get; set; }
}

public class _Shards
{
    public int total { get; set; }
    public int successful { get; set; }
    public int skipped { get; set; }
    public int failed { get; set; }
}

public class Hits
{
    public int total { get; set; }
    public int max_score { get; set; }
    public Hit[] hits { get; set; }
}

public class Hit
{
    public string _index { get; set; }
    public string _type { get; set; }
    public string _id { get; set; }
    public int _score { get; set; }
    public Indberetning _source { get; set; }
}

public class Indberetning
{
    public string cvrNummer { get; set; }
    public string regNummer { get; set; }
    public bool omgoerelse { get; set; }
    public string sagsNummer { get; set; }
    public string offentliggoerelsestype { get; set; }
    public Regnskab regnskab { get; set; }
    public DateTime offentliggoerelsesTidspunkt { get; set; }
    public DateTime indlaesningsTidspunkt { get; set; }
    public DateTime sidstOpdateret { get; set; }
    public Dokumenter[] dokumenter { get; set; }
    public string indlaesningsId { get; set; }
}

public class Regnskab
{
    public Regnskabsperiode regnskabsperiode { get; set; }
}

public class Regnskabsperiode
{
    public string startDato { get; set; }
    public string slutDato { get; set; }
}

public class Dokumenter
{
    public string dokumentUrl { get; set; }
    public string dokumentMimeType { get; set; }
    public string dokumentType { get; set; }
}