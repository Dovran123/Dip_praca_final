using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Infrastructure.Services;
using System.Security.Cryptography;
using Infrastructure.Config;
using Application.Common.Interfaces.Persistence;
namespace Infrastructure.Persistence
{
    public static class ApplicationDbContextSeed
    {

        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("🔄 Začíname seeding dát...");
            await SeedOdbornosti(context, logger);
            await SeedPouzivatelov(context, logger);
            await SeedDiagnozy(context, logger);
            await SeedLieky(context, logger);
            await SeedDoplnky(context, logger);
            await SeedLimitNaDoplnky(context, logger); 
            await SeedPotraviny(context, logger);
            await SeedAmbulancie(context, logger);
            await SeedPreskripcneZaznamy(context, logger);
           

            logger.LogInformation("✅ Seeding úspešne dokončený.");
        }



        private static async Task SeedOdbornosti(ApplicationDbContext context, ILogger logger)
        {
            string cesta = GlobalConfig.CestaKuSuboru;

            if (await context.OdbornostiLekarov.AnyAsync()) return;

            var csvFilePath = $"{cesta}zoznam_skratiek_lekari.csv"; // 🔹 Nastav správnu cestu
            await Importers.ImportOdbornostAsync(context, csvFilePath);
            logger.LogInformation("✅ Odbornosti lekárov boli úspešne načítané z CSV.");
        }
        private static async Task SeedPouzivatelov(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Pacienti.AnyAsync()) return;

            var faker = new Faker("sk");
            var pouzivatelia = new List<Pouzivatel>();
            var pacienti = new List<Pacient>();
            var lekari = new List<Lekar>();
            var poistenia = new List<Poistenie>();
            var odbornosti = await context.OdbornostiLekarov.ToListAsync();
            var generator = new RodneCisloGenerator(context);

            // 🛡 Admin
            var pouzivatelAdmin = new Pouzivatel
            {
                Id = Guid.NewGuid(),
                Meno = "Admin",
                Priezvisko = "Admin",
                RodneCislo = "0001010000",
                Email = "admin@admin.com",
                Heslo = BCrypt.Net.BCrypt.HashPassword("Heslo123!"),
                JePrimatel = true,
                Typ = "Admin",
                TokenPrimatela = GenerateSecureToken()
            };
            pouzivatelia.Add(pouzivatelAdmin);

            // 🔄 Nastavenie pre rozdelenie odborností
            var odbornostiPocty = odbornosti.ToDictionary(o => o.Identifikator, o => 0);
            int minLekarovNaOdbornost = 1;
            int pocetOdbornosti = odbornosti.Count;
            int celkovyPocetLekarov = 400;
            int pocetLekarovVytvorenych = 0;

            for (int i = 0; i < 5000; i++)
            {
                bool isFemale = i % 2 == 0;
                string meno = isFemale ? faker.Name.FirstName(Bogus.DataSets.Name.Gender.Female) : faker.Name.FirstName(Bogus.DataSets.Name.Gender.Male);
                string priezvisko = isFemale ? faker.Name.LastName(Bogus.DataSets.Name.Gender.Female) : faker.Name.LastName(Bogus.DataSets.Name.Gender.Male);
                string rodneCislo = await generator.GenerateUniqueRodneCislo(faker, isFemale);

                var pouzivatel = new Pouzivatel
                {
                    Id = Guid.NewGuid(),
                    Meno = meno,
                    Priezvisko = priezvisko,
                    RodneCislo = rodneCislo,
                    Email = faker.Internet.Email(),
                    Heslo = BCrypt.Net.BCrypt.HashPassword("Heslo123!"),
                    Typ = "Pacient"
                };

                var poistnyVztah = faker.PickRandom<PoistnyVztahPoistencaEnum>();
                var jeNeplatic = poistnyVztah == PoistnyVztahPoistencaEnum.NieJePoistencom;
                var kodPoistovne = jeNeplatic ? null : faker.PickRandom(new[] { "24", "25", "27" });
                var icp = jeNeplatic ? null : faker.Random.String2(10, "0123456789");

                var poistenie = new Poistenie
                {
                    Id = Guid.NewGuid(),
                    Datum = DateTime.UtcNow,
                    PoistnyVztahPoistenca = poistnyVztah,
                    JeNeplatic = jeNeplatic,
                    KodPoistovne = kodPoistovne,
                    ICP = icp,
                    ZaciatokEuPoistenia = poistnyVztah == PoistnyVztahPoistencaEnum.JePoistencomEU ? faker.Date.Past(2) : null,
                    MaNarokNaOdkladnuZS = jeNeplatic ? faker.Random.Bool() : null
                };

                bool budeLekar = false;
                string kodOdbornosti = "";

                if (pocetLekarovVytvorenych < pocetOdbornosti * minLekarovNaOdbornost)
                {
                    var odbornostNaDoplnenie = odbornostiPocty
                        .Where(op => op.Value < minLekarovNaOdbornost)
                        .Select(op => op.Key)
                        .FirstOrDefault();

                    if (odbornostNaDoplnenie != null)
                    {
                        budeLekar = true;
                        kodOdbornosti = odbornostNaDoplnenie;
                        odbornostiPocty[kodOdbornosti]++;
                    }
                }
                else if (faker.Random.Bool(0.15f) && pocetLekarovVytvorenych < celkovyPocetLekarov)
                {
                    budeLekar = true;
                    kodOdbornosti = faker.PickRandom(odbornosti).Identifikator;
                    odbornostiPocty[kodOdbornosti]++;
                }

                if (budeLekar)
                {
                    var lekar = new Lekar
                    {
                        Id = pouzivatel.Id,
                        Pouzivatel = pouzivatel,
                        LicencneCislo = faker.Random.AlphaNumeric(20),
                        KodOdbornoti = kodOdbornosti
                    };
                    pouzivatel.Typ = "Lekar";
                    lekari.Add(lekar);
                    pocetLekarovVytvorenych++;
                }
                else
                {
                    var pacient = new Pacient
                    {
                        Id = pouzivatel.Id,
                        Pouzivatel = pouzivatel,
                        PoistenieId = poistenie.Id,
                        Poistenie = poistenie
                    };
                    pacienti.Add(pacient);
                    poistenia.Add(poistenie);
                }

                pouzivatelia.Add(pouzivatel);
            }

            await context.Pouzivatelia.AddRangeAsync(pouzivatelia);
            await context.Pacienti.AddRangeAsync(pacienti);
            await context.Lekari.AddRangeAsync(lekari);
            await context.Poistenia.AddRangeAsync(poistenia);
            await context.SaveChangesAsync();

            logger.LogInformation($"Vygenerovaných {pouzivatelia.Count} používateľov, z toho {lekari.Count} lekárov s pokrytím všetkých odborností.");
        }

        private static async Task SeedDiagnozy(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Diagnozy.AnyAsync()) return;
            string cesta = GlobalConfig.CestaKuSuboru;

            string pdfPath = $"{cesta}Zoznam_diagnoz_sumar.pdf"; // 🔹 Nastav správnu cestu k PDF súboru

                await Importers.ImportSeedDiagnos(context,logger, pdfPath);
   
        }
        private static async Task SeedDoplnky(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Doplnky.AnyAsync()) return;
            string cesta = GlobalConfig.CestaKuSuboru;

            string pdfPath = $"{cesta}Zoznam_ZP_202501.xlsx"; // 🔹 Nastav správnu cestu k PDF súboru

            await Importers.ImportDoplnkyAsync(context, pdfPath);

        }
        private static async Task SeedLimitNaDoplnky(ApplicationDbContext context, ILogger logger)
        {
            if (await context.LimityPredpisov.AnyAsync()) return;
            string cesta = GlobalConfig.CestaKuSuboru;

            string csvPath = $"{cesta}Limits.csv"; // 🔹 Nastav správnu cestu k PDF súboru
            await Importers.ImportLimityZCsv(csvPath, context);


        }
        private static async Task SeedPotraviny(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Potraviny.AnyAsync()) return;
            string cesta = GlobalConfig.CestaKuSuboru;

            string pdfPath = $"{cesta}cast_A_zoznam_potravin.xlsx"; // 🔹 Nastav správnu cestu k PDF súboru
            await Importers.ImportPotravinyAsync(context, pdfPath);
        }
        // ... pokračovanie s ďalšími diagnózami z PDF

        private static async Task SeedAmbulancie(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Ambulancie.AnyAsync()) return;
            var faker = new Faker("sk");
            var ambulancie = new List<Ambulancia>();
            var lekari = await context.Lekari.ToListAsync();

            foreach (var lekar in lekari)
            {
                if (faker.Random.Double() <= 0.9) // 🔹 90% lekárov má ambulanciu
                {
                    int pocetAmbulancii = faker.Random.Int(1, 3); // Každý vybraný lekár môže mať 1 až 3 ambulancie
                    for (int i = 0; i < pocetAmbulancii; i++)
                    {
                        var kod = faker.Random.String2(1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ") +
                                  faker.Random.Number(10000, 99999).ToString("D5") +
                                  faker.Random.String2(6, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

                        if (kod.Length != 12)
                        {
                            kod = kod.Substring(0, 12); // Uistíme sa, že kód má presne 12 znakov
                        }

                        var ambulancia = new Ambulancia
                        {
                            Kod = kod,
                            Nazov = faker.Company.CompanyName(),  
                            Adresa = faker.Address.StreetAddress(),
                            Mesto = faker.Address.City(),
                            PSC = faker.Address.ZipCode(),
                            LekarId = lekar.Id
                        };
                        ambulancie.Add(ambulancia);
                    }
                }
            }

            await context.Ambulancie.AddRangeAsync(ambulancie);
            await context.SaveChangesAsync();
            logger.LogInformation($"🏥 Vygenerovaných {ambulancie.Count} ambulancií pre 60% lekárov.");
        }
            // Funkcia na generovanie validného rodného čísla
            
    

        private static async Task SeedPreskripcneZaznamy(ApplicationDbContext context, ILogger logger)
        {
            if (await context.PreskripcneZaznamy.AnyAsync()) return;

            var pacienti = await context.Pacienti.ToListAsync();
            var lekari = await context.Lekari.Include(l => l.Ambulancie).ToListAsync();
            var lieky = await context.Lieky.ToListAsync();
            var diagnozy = await context.Diagnozy.Select(d => d.KodDiagnozy).ToListAsync();
            var ambulancie = await context.Ambulancie.Select(a => a.Kod).ToHashSetAsync();
            var doplnky = await context.Doplnky.ToListAsync();
            var potraviny = await context.Potraviny.ToListAsync();

            if (!lekari.Any() || (!lieky.Any() && !doplnky.Any() && !potraviny.Any()) || !diagnozy.Any())
            {
                logger.LogWarning("⚠️ Chýbajú lekári, lieky, doplnky, potraviny alebo diagnózy. Preskakujem generovanie predpisov.");
                return;
            }

            var faker = new Faker("sk");
            var predpisy = new List<PrekricnyZaznam>();
            var predpisaneLieky = new List<PrekricnyZaznamLiek>();
            var predpisanePomocky = new List<PrekricnyZaznamPomocky>();
            var predpisanePotraviny = new List<PrekricnyZaznamPotraviny>();

            foreach (var pacient in pacienti.Take(2000)) // Zvýšenie počtu generovaných záznamov
            {
                int pocetPredpisov = faker.Random.Int(2, 5); // Zvýšený počet predpisov na pacienta

                for (int i = 0; i < pocetPredpisov; i++)
                {
                    var lekar = faker.PickRandom(lekari);
                    var diagnozaKod = faker.PickRandom(diagnozy);

                    string? ambulanciaKod = lekar.Ambulancie.Any()
                        ? faker.PickRandom(lekar.Ambulancie).Kod
                        : null;

                    if (ambulanciaKod != null && !ambulancie.Contains(ambulanciaKod))
                    {
                        ambulanciaKod = null;
                    }

                    var predpis = new PrekricnyZaznam
                    {
                        Id = Guid.NewGuid(),
                        PacientId = pacient.Id,
                        LekarId = lekar.Id,
                        AmbulanciaKod = ambulanciaKod,
                        DatumPredpisu = faker.Date.Past(1),
                        PocetDni = faker.Random.Int(5, 30),
                        Stav = "Aktívny",
                        DiagnozaKod = diagnozaKod
                    };
                    predpisy.Add(predpis);

                    // ✅ Generovanie liekov pre predpis
                    var pocetLieky = faker.Random.Int(1, 5);
                    var vybrateLieky = faker.Random.ListItems(lieky, pocetLieky);

                    foreach (var liek in vybrateLieky)
                    {
                        predpisaneLieky.Add(new PrekricnyZaznamLiek
                        {
                            Id = Guid.NewGuid(),
                            PrekricnyZaznamId = predpis.Id,
                            LiekId = liek.Id
                        });
                    }

                    // ✅ 50% šanca, že pridáme doplnky k predpisu
                    bool pridatDoplnky = faker.Random.Bool();
                    if (pridatDoplnky)
                    {
                        var pocetDoplnky = faker.Random.Int(1, 3); // 1 až 3 doplnky, ak sa pridajú
                        var vybrateDoplnky = faker.Random.ListItems(doplnky, pocetDoplnky);

                        foreach (var doplnok in vybrateDoplnky)
                        {
                            // 🔹 Random množstvo v rozmedzí 1 - 10 (alebo podľa limitov)
                            var mnozstvo = faker.Random.Int(1, 10);

                            // ✅ Skontrolujeme, či môžeme predpísať daný doplnok
                            bool mozePredpisat = await LimitChecker.MozemPredpisatDoplnokPodlaLimitValue(context, doplnok, mnozstvo);

                            if (mozePredpisat)
                            {
                                predpisanePomocky.Add(new PrekricnyZaznamPomocky
                                {
                                    Id = Guid.NewGuid(),
                                    PrekricnyZaznamId = predpis.Id,
                                    DoplnokId = doplnok.Id,
                                    Mnozstvo = mnozstvo
                                });
                            }
                        }
                    }
                    // ✅ Pridanie potravín pri každom 100. predpise
                    if (i % 100 == 0 && potraviny.Any())
                    {
                        var vybrataPotravina = faker.PickRandom(potraviny);
                        predpisanePotraviny.Add(new PrekricnyZaznamPotraviny
                        {
                            Id = Guid.NewGuid(),
                            PrekricnyZaznamId = predpis.Id,
                            PotravinaId = vybrataPotravina.Id
                        });
                    }
                }
            }

            await context.PreskripcneZaznamy.AddRangeAsync(predpisy);
            await context.PrekricnyZaznamLiekov.AddRangeAsync(predpisaneLieky);
            await context.PrekricnyZaznamDoplnok.AddRangeAsync(predpisanePomocky);
            await context.PrekricnyZaznamPotraviny.AddRangeAsync(predpisanePotraviny);

            await context.SaveChangesAsync();
        }

        private static async Task SeedLieky(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Lieky.AnyAsync()) return;
            string cesta = GlobalConfig.CestaKuSuboru;

            var excelFilePath = $"{cesta}cast_A_zoznam_liekov.xlsx";
            await Importers.ImportAndSeedLiekyAsync(context, excelFilePath);

            logger.LogInformation("💊 Lieky načítané z Excel súboru.");
        }

        private static string GenerateSecureToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData).TrimEnd('=');
            }
        }
    }
}
