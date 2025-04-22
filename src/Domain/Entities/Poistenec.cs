using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Poistenie
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(10)]
        public string? ICP { get; set; } // Identifikačné číslo poistenca (voliteľné)

        public DateTime? Datum { get; set; } // Dátum overenia (ak nie je, použije sa aktuálny)

        [MaxLength(2)]
        [RegularExpression(@"^(24|25|27)$", ErrorMessage = "Kód poisťovne musí byť 24, 25 alebo 27.")]
        public string? KodPoistovne { get; set; } = default!; // 24 - Dôvera, 25 - VšZP, 27 - Union

        [Required]
        public PoistnyVztahPoistencaEnum PoistnyVztahPoistenca { get; set; } // Poistný vzťah

        [Required]
        public bool JeNeplatic { get; set; } // TRUE = dlžník, FALSE = nie je dlžník

        public DateTime? ZaciatokEuPoistenia { get; set; } // Vyplnené iba pri `JePoistenecEU`

        public bool? MaNarokNaOdkladnuZS { get; set; } // Vyplnené iba ak `JeNeplatic = TRUE`
    }

    // 📌 Enum pre Poistný Vzťah Poistenca
    public enum PoistnyVztahPoistencaEnum
    {
        NieJePoistencom, // Poistenec nie je evidovaný
        JePoistencom, // Poistenec je v poisťovni
        JePoistencomEU // Poistenec je v poisťovni na formulár EÚ
    }


}
