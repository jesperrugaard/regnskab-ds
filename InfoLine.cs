namespace RegnskabsHenter
{
    /* 
Efterfølgende linier indeholder data - (h er Hits[i]):
isn=1
ssn=0
uid=h.Source.LeveranceId;
publicering=TIL
kode=R
cvrNummer=h.Source.CvrNummer
periodeStart=h.Source.Regnskab.Regnskabsperiode.StartDato
periodeSlut=h.Source.Regnskab.Regnskabsperiode.SlutDato
xbrlDokument=navnet på xbrldokumentet i zip-filen
pdfDokument=permanent link til det tilhørende regnskab i PDF-format
indlaesningstidspunkt er tidspunktet for indlæsning i indeks

*/
   public class InfoLine
   {
    public string ISN {get{ return "1"; }}
    public string SSN {get{return "0"; }}
    public string UID {get;set;}
    public string Publicering {get {return "TIL";}}
    public string Kode {get {return "R";}}
    public string CVRNUMMER {get;set;}
    public string PeriodeStart {get;set;}
    public string PeriodeSlut {get;set;}
    public string XbrlDokument {get;set;}
    public string PDFDokument {get;set;}

    public string Tidspunkt {get; set;}
    
   } 
}




 
