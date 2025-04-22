using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models;

[DataContract]
public class InterakciaResponse
{
    [DataMember]
    public string? InterakcieLiekovPacienta { get; set; }

    [DataMember]
    public APIDokument? Dokument { get; set; }
}
[DataContract]
public class UlozPreskripcnyZaznamResponse
{

    [DataMember]
    public Guid? CiarovyKod { get; set; }
    [DataMember]
    public string? PoznamkaZP { get; set; }
    [DataMember]
    public List<APISprava> Spravy { get; set; } = new();
}
[DataContract]
public class ErrorResponse
{
    [DataMember]
    public string KodChyby { get; set; }

    [DataMember]
    public string PopisChyby { get; set; }

    public ErrorResponse(string popisChyby, string kodChyby = "ERR_Response")
    {
        KodChyby = kodChyby;
        PopisChyby = popisChyby;
    }
}
[DataContract]
public class StornujPreskripcnyZaznamResponse
{
    [DataMember]
    public bool Stornovany { get; set; }

    [DataMember]
    public List<APISprava> Spravy { get; set; } = new();
}
[DataContract]
public class VyhladajPreskripcnyZaznamResponse
{
    [DataMember]
    public List<ApiPreskripcnyZaznam> PreskripcneZaznamy { get; set; } = new();

    [DataMember]
    public List<APISprava> Spravy { get; set; } = new();
}

[DataContract]
public class BlokujPreskripcnyZaznamResponse
{
    [DataMember]
    public Guid CiarovyKod { get; set; }

    [DataMember]
    public DateTime DatumCasBlokovaniaDo { get; set; }

    [DataMember]
    public List<APISprava> Spravy { get; set; } = new();
}
[DataContract]
public class ZrusBlokovaniePreskripcnehoZaznamuResponse
{
    [DataMember]
    public Guid CiarovyKod { get; set; }

    [DataMember]
    public List<APISprava> Spravy { get; set; } = new();
}
[DataContract]
public class Vyrobky
{
    [DataMember]
    public string KodLieku { get; set; } = default!;
    [DataMember]
    public string KodKategorie { get; set; } = default!;
    [DataMember]
    public string Nazov { get; set; } = default!;
    [DataMember]
    public string Doplnok { get; set; } = default!;
}
public class APIObmedzeniePredpisu
{
    [DataMember]
    public string Kod { get; set; } = default!; // Kód obmedzenia
    [DataMember]
    public string Nazov { get; set; } = default!; // Názov obmedzenia
    [DataMember]
    public string Zavaznost { get; set; } = default!; // Z-závažné, I – informatívne, P – povinné
    [DataMember]
    public string? Hodnota { get; set; } // Možná hodnota obmedzenia (napr. počet kusov)
}
public class SkontrolujPredpisPomockyResponse
{
    /// <summary>
    /// Indikuje, či je predpis pomôcky povolený (true) alebo zakázaný (false).
    /// </summary>
    [DataMember]
    public bool PovolenyPredpis { get; set; }

    /// <summary>
    /// Pole obmedzení predpisu, ktoré obsahuje všetky pravidlá a limity, ktoré sa aplikovali na daný predpis.
    /// </summary>
    [DataMember]
    public List<APIObmedzeniePredpisu> Obmedzenia { get; set; } = new List<APIObmedzeniePredpisu>();
}

[DataContract]
public class KategoriaDTO
{
    [DataMember]
    public string Kod { get; set; } = default!;

    [DataMember]
    public string Nazov { get; set; } = default!;

    [DataMember]
    public List<KategoriaDTO> Podkategorie { get; set; } = new();
}
