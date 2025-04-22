using Application.Common.Interfaces.Persistence;
using Bogus;
using Microsoft.EntityFrameworkCore;

public class RodneCisloGenerator
{
    private readonly IApplicationDbContext _context;

    public RodneCisloGenerator(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateUniqueRodneCislo(Faker faker, bool isFemale)
    {
        string rodneCislo;
        bool exists;

        do
        {
            rodneCislo = GenerateValidRodneCislo(faker, isFemale);

            // ✅ Kontrola v databáze
            exists = await _context.Pouzivatelia
            .FirstOrDefaultAsync(p => p.RodneCislo == rodneCislo) != null;


        } while (exists); // Generuje nové, ak už existuje

        return rodneCislo;
    }

    private static string GenerateValidRodneCislo(Faker faker, bool isFemale)
    {
        while (true)
        {
            // Generovanie dátumu narodenia
            DateTime birthDate = faker.Date.Between(new DateTime(1900, 1, 1), new DateTime(2099, 12, 31));
            int year = birthDate.Year % 100;
            int month = birthDate.Month;
            int day = birthDate.Day;

            // Ak je žena, pridáme k mesiacu 50
            if (isFemale) month += 50;

            // Generovanie posledných štyroch čísel
            int lastFour = faker.Random.Int(0, 9999);

            // Skombinovanie čísel do formátu RRMMDDXXXX
            string fullNumber = $"{year:D2}{month:D2}{day:D2}{lastFour:D4}";

            // Overenie deliteľnosti 11
            if (long.Parse(fullNumber) % 11 == 0)
            {
                return $"{year:D2}{month:D2}{day:D2}/{lastFour:D4}";
            }
        }
    }
}
