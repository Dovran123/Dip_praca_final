using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Domain.Entities;

namespace Application.Common.Models
{
    [DataContract]
    public class APIDokument
    {
        [DataMember]
        public string? NazovDokumentu { get; set; }

        [DataMember]
        public string? Typ { get; set; }

        [DataMember]
        public byte[]? Obsah { get; set; }

        [DataMember]
        public bool Komprimovany { get; set; }

        [DataMember]
        public string? Poznamka { get; set; }
    }

   
    // Response
    [DataContract]
    public class APISprava
    {
        [DataMember]
        public ZavaznostSpravy Zavaznost { get; set; } // Závažnosť správy (INFO, VAROVANIE, CHYBA, ODMIETNUTIE)

        [DataMember]
        [MaxLength(10)]
        public string Kod { get; set; } = default!; // Kód výsledku spracovania

        [DataMember]
        public string Text { get; set; } = default!; // Textová správa
    }

    // Enumerácia pre závažnosť správ
    public enum ZavaznostSpravy
    {
        INFO,
        VAROVANIE,
        CHYBA,
        ODMIETNUTIE
    }
    // Predpisy
    [DataContract]
    public class ApiPreskripcnyZaznam
    {
        [DataMember]
        public Guid? CiarovyKod { get; set; } = default!;
        [DataMember]
        public string? RC { get; set; } = default!;
        [DataMember]
        public string? ICP { get; set; } = default!;
        [DataMember]
        public Guid? LekarId { get; set; } = default!;
        [DataMember]
        public Guid? PacientId { get; set; } = default!;
        [DataMember]
        public DateTime? DatumVydania { get; set; } = default!;
        [DataMember]
        public string? AmbulanciaKod { get; set; } = default!;
        [DataMember]
        public int PocetDni { get; set; } = default!;
        [DataMember]
        public string? Stav { get; set; } = default!;
        [DataMember]
        public string DiagnozaKod { get; set; } = default!;
        [DataMember]
        public List<string>? Lieky { get; set; } = new();
        [DataMember]
        public List<APIDoplnky>? Doplnky { get; set; } = new();
        [DataMember]
        public List<string>? Potraviny { get; set; } = new();
    }
    [DataContract]
    public class PacientDto
    {
        [DataMember]
        public string RodneCislo { get; set; } = default!;
        [DataMember]
        public string? ICP { get; set; } // ICP môže byť null

        [DataMember]
        public Pacient Pacient { get; set; } = default!;
    }

    // Blokovanie
    [DataContract]
    public class APIDoplnky
    {
        [DataMember]
        public string Kod { get; set; } = default!;
        [DataMember]
        public int Mnozstvo { get; set; }
    }

    [DataContract]
    public class APIUdajePreBlokovanie
    {
        [DataMember]
        public Guid CiarovyKod { get; set; }
        //Na Buduce rozsirenie sluzby
        // TODO: Doplniť dáta pre blokovanie
        // [DataMember]
        // public string? DovodBlokovania { get; set; } = default!; // Dôvod blokovania
        // [DataMember]
        // public string? PoznamkaKDovodu { get; set; } = default!; // Dôvod blokovania
        [DataMember]
        public string IdAmbulancie { get; set; } = default!; // Id ambulancie, ktorá blokuje
    }
    [DataContract]
    public class APIUdajePreOdBlocovanie
    {
        [DataMember]
        public Guid CiarovyKod { get; set; }
        [DataMember]
        public string IdAmbulancie { get; set; } = default!; // Id ambulancie, ktorá blokuje
    }
}
