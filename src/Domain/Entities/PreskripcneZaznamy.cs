namespace Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public class Pouzivatel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    [MaxLength(100)]
    public string Meno { get; set; } = default!;
    [Required]
    [MaxLength(100)]
    public string Priezvisko { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    public string Email { get; set; } = default!;
    [Required]
    [MaxLength(11)]
    public string RodneCislo { get; set; } = default!;

    [Required]
    public string Heslo { get; set; } = default!; // Hashed password

    [Required]
    public string Typ { get; set; } = default!; // "Pacient" alebo "Lekar alebo "Pouzivatel" "

    public bool JePrimatel { get; set; } = false; // TRUE = dostáva notifikácie, FALSE = nedostáva

    public string? TokenPrimatela { get; set; } = default;

    public virtual Pacient? Pacient { get; set; }
    public virtual Lekar? Lekar { get; set; }
}

// 📌 Pacient
public class Pacient
{
    [Key]
    public Guid Id { get; set; }


    [Required]
    public Guid PouzivatelId { get; set; }

    [ForeignKey("PouzivatelId")]
    public virtual Pouzivatel Pouzivatel { get; set; } = default!;

    [Required]
    public Guid PoistenieId { get; set; }
    [ForeignKey("PoistenieId")]
    public virtual Poistenie Poistenie { get; set; } = default!;

    public virtual ICollection<PrekricnyZaznam> Preskripcie { get; set; } = new List<PrekricnyZaznam>();
}

// 📌 Lekár
public class Lekar
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string LicencneCislo { get; set; } = default!;

    [Required]
    public Guid PouzivatelId { get; set; }

    [ForeignKey("PouzivatelId")]
    public virtual Pouzivatel Pouzivatel { get; set; } = default!;
    [Required]
    public string KodOdbornoti { get; set; } = default!;

    [ForeignKey("KodOdbornoti")]
    public virtual OdbornostLekara Odbornost { get; set; } = default!;

    public virtual ICollection<PrekricnyZaznam> Predpisy { get; set; } = new List<PrekricnyZaznam>();

    public virtual ICollection<Ambulancia> Ambulancie { get; set; } = new List<Ambulancia>();
}
public class OdbornostLekara
{
    [Key]
    [MaxLength(3)]
    public string Identifikator { get; set; } = null!;
    [Required]
    public string PopisOdbornosti { get; set; } = null!;
}
// 📌 Ambulancia
public class Ambulancia
{
    [Key]
    [Required]
    [MaxLength(12)]
    [RegularExpression(@"^[A-Za-z]\d{5}[A-Za-z0-9]{6}$", ErrorMessage = "Kód ambulancie musí mať 12 znakov, začínať písmenom a obsahovať čísla aj písmená.")]
    public string Kod { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Nazov { get; set; } = default!;

    [Required]
    [MaxLength(300)]
    public string Adresa { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string Mesto { get; set; } = default!;

    [Required]
    [MaxLength(10)]
    public string PSC { get; set; } = default!;

    [Required]
    public Guid LekarId { get; set; }

    [ForeignKey("LekarId")]
    public virtual Lekar Lekar { get; set; } = default!;

    public virtual ICollection<PrekricnyZaznam> PrekricneZaznamy { get; set; } = new List<PrekricnyZaznam>();
}

// 📌 Prekričný záznam (Predpis)
public class PrekricnyZaznam
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey("Pacient")]
    public Guid? PacientId { get; set; }

    [ForeignKey("Lekar")]
    public Guid? LekarId { get; set; }

    [ForeignKey("Ambulancia")]
    public string? AmbulanciaKod { get; set; } = default!;

    [Required]
    public DateTime DatumPredpisu { get; set; }

    [Required]
    public int PocetDni { get; set; }

    [Required]
    [MaxLength(50)]
    public string Stav { get; set; } = "Aktívny";

    [ForeignKey("Diagnoza")]
    public string DiagnozaKod { get; set; } = default!;

    public virtual Diagnoza? Diagnoza { get; set; } = default!;
    public virtual Pacient? Pacient { get; set; }
    public virtual Lekar? Lekar { get; set; }
    public virtual Ambulancia? Ambulancia { get; set; } = default!;
}
public abstract class PrekricnyZaznamProdukt
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey("PrekricnyZaznam")]
    public Guid PrekricnyZaznamId { get; set; }
    public virtual PrekricnyZaznam PrekricnyZaznam { get; set; } = default!;
}

public class PrekricnyZaznamLiek : PrekricnyZaznamProdukt
{
    [Required]
    [ForeignKey(nameof(Liek))] // Správna anotácia ForeignKey
    public Guid LiekId { get; set; } // ✅ GUID namiesto stringu
    public virtual Liek Liek { get; set; } = default!;
}

public class PrekricnyZaznamPomocky : PrekricnyZaznamProdukt
{
    [Required]
    [ForeignKey(nameof(Doplnok))]
    public Guid DoplnokId { get; set; } // ✅ GUID namiesto stringu
    public virtual Doplnok Doplnok { get; set; } = default!;
    public int Mnozstvo { get; set; }
}

public class PrekricnyZaznamPotraviny : PrekricnyZaznamProdukt
{
    [Required]
    [ForeignKey(nameof(Potravina))]
    public Guid PotravinaId { get; set; } // ✅ GUID namiesto stringu
    public virtual Potravina Potravina { get; set; } = default!;
}


