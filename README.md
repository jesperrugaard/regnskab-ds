# regnskab-ds

19-12-2018 Ny version: 
- Lægger indlæsningstidspunkt med i csv-fil
- Sorterer i elastic search opslaget, således at hver dag er ordnet først efter cvr og derefter indlæsnngstidspunkt 

10-12-2018 Ny version
Kan benytte både offentliggørelsestidspunkt og indlæsningstidspunkt i søgning. Indlæsningstidspunkt er default, da det i den daglige indlæsning vil give det bedste resultat.
Nye parametre:
        <add key="use_max"           value="true"/>
        <add key="use_index_date"    value="true"/>
        <add key="max"               value="50000"/>
use_max og max er for det tlfælde at indekset genindlæses og der kommer et overvældende antal regnskaber ind. Systemet stopper hvis det læser mere end 50.000 regnskaber ind. 
use_index_date=true benytter indlæsningstidspunkt for fremsøgning, hvis use_index_date=false, så benyttes offentliggørelsesdatoen

05-12-2018 Ny version
Ændret navngivning til at følge YYYY-MM-dd standard for zip-fil.

03-12-2018 Ny version

- Opdeling i zipfiler pr dag. De deler dog alle det samme uuid, dvs. man kan genkende en kørsel på dens uuid
- Rettet indberetningsdato til offentliggoerelsesTidspunkt
- Ændret app.config til log4net.config, da den kun indeholder log-information
- Tilføjet lidt ekstra information i kørselsvindue 

02-12-2018 Første version
Versionen kræver .net core installeret: https://dotnet.microsoft.com/download

For at starte programmet går man til udpakket programkatalog og skriver: 
dotnet RegnskabsHenter.dll

Så starter programmet og henter default den sidste dags regnskaber ned (use_yesterday)

Appsettings i RegnskabsHenter.dll.config kan indstilles som I ønsker.
Der hvor jeg mener det er relevant at skifte værdier nu er for temp_directory og target_directory.

Hvis I ønsker at hente andre datoer end for i går, sættes use_yesterday: false og fra og til dato ændres til værdi. 

Hvis chunks har for stor en værdi så lukker programmet ned - blokeret af webserveren, så den bør der ikke pilles ved. 

 <appSettings>
        <add key="temp_directory"    value= "\\localhost\c$\temp\"/>
        <add key="target_directory"  value= "\\localhost\c$\result\"/>
        <add key="name_of_run"       value= "Regnskabskoersel"/>
        <add key="chunks"            value="70"/>
        <add key="page_size"         value="2000"/>
        <add key="base_uri"          value="http://distribution.virk.dk" />
        <add key="start_date"        value="29-11-2018" />
        <add key="end_date"         value="02-12-2018" />
        <add key="use_yesterday"     value="true"/>
    </appSettings>