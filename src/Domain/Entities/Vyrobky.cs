using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace Domain.Entities;

// 📌 Diagnoza

public class Diagnoza
{
    [Key]
    public string KodDiagnozy { get; set; } = default!;

    [Required]
    public string Nazov { get; set; } = default!;
}
// Dedicost
public abstract class Produkt
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Kod { get; set; } = null!;

    [Required]
    public string Nazov { get; set; } = null!;

    [Required]
    public string NazovDoplnku { get; set; } = default!;

    [Required]
    public List<string>? PO { get; set; } = new();
}

public class Liek : Produkt
{
    [Required]
    public string KodKategorie { get; set; } = default!;

    [ForeignKey("KodKategorie")]
    public virtual KategoriaLiekov Kategoria { get; set; } = default!;
}

public class Doplnok : Produkt
{
    [Required]
    public string KodKategorie { get; set; } = default!;

    [ForeignKey("KodKategorie")]
    public virtual KategoriaDoplnok Kategoria { get; set; } = default!;
}

public class Potravina : Produkt
{
    [Required]
    public string KodKategorie { get; set; } = default!;

    [ForeignKey("KodKategorie")]
    public virtual KategoriaPotravina Kategoria { get; set; } = default!;
}

public class Kategoria
{
    [Key]
    public string Kod { get; set; } = default!;

    [Required]
    [MaxLength(800)]
    public string Nazov { get; set; } = default!;

    public string? NadriadenaKategorieKod { get; set; }

    [ForeignKey("NadriadenaKategorieKod")]
    public virtual Kategoria? NadriadenaKategoria { get; set; }

    public virtual ICollection<Kategoria> Podkategorie { get; set; } = new List<Kategoria>();
}

public class KategoriaDoplnok : Kategoria
{
    public string TypDoplnku { get; set; } = default!;

    public virtual ICollection<Doplnok> Doplnky { get; set; } = new List<Doplnok>();
    public Guid? LimitPredpisuId { get; set; }
    [ForeignKey("LimitPredpisuId")]
    public virtual LimitPredpisu? LimitPredpisu { get; set; }
}

public class LimitPredpisu
{ 
    [Key]
    public Guid Id { get; set; }

    public int? YearsLimit { get; set; }  // Maximálny počet kusov za rok
    public int? MonthsLimit { get; set; } // Maximálny počet kusov za mesiac
    public int? WeeksLimit { get; set; }  // Maximálny počet kusov za týždeň
    public int? LimitValue { get; set; }  // Celkový limit za fungovanie systému

    public DateTime? CasovyOkamih { get; set; } // Kedy bol limit naposledy aktualizovaný
}


public class KategoriaPotravina : Kategoria
{
    public string TypPotraviny { get; set; } = default!;

    public virtual ICollection<Potravina> Potraviny { get; set; } = new List<Potravina>();
}

public class KategoriaLiekov
{
    [Key]
    public string Kod { get; set; } = default!;

    [Required]
    [MaxLength(800)]
    public string Nazov { get; set; } = default!;

    public virtual ICollection<Liek> Lieky { get; set; } = new List<Liek>();
}
