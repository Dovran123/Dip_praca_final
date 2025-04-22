
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace Infrastructure.Services
{
    public class Importers
    {
        /// <summary>
        /// Načíta dáta z Excel súboru a priamo ich uloží do databázového kontextu.
        /// Pre každý riadok vytvorí objekt Liek a odvodí kategórie na základe stĺpca KodKategorie.
        /// </summary>
        /// <param name="context">Inštancia ApplicationDbContext</param>
        /// <param name="filePath">Cesta k Excel súboru a pdf suboru</param>
        public static async Task ImportSeedDiagnos(ApplicationDbContext context, ILogger logger, string filePath)
        {
            var diagnozy = new HashSet<Diagnoza>();

            using (var pdfDocument = PdfDocument.Open(filePath)) // ✅ PdfPig na otváranie PDF
            {
                bool isFirstLineSkipped = false; // 🔹 Skipne iba prvý riadok v celom PDF

                foreach (var page in pdfDocument.GetPages())
                {
                    string text = ContentOrderTextExtractor.GetText(page); // ✅ Extrakcia textu
                    var lines = text.Split("\r\n");

                    foreach (var line in lines)
                    {
                        if (!isFirstLineSkipped) // Skip prvého riadku
                        {
                            isFirstLineSkipped = true;
                            continue;
                        }

                        var parts = line.Split(' ', 2); // ✅ Predpoklad: kód diagnózy + názov
                        if (parts.Length == 2)
                        {
                            string kod = parts[0].Trim();
                            string nazov = parts[1].Trim();
                            if (!string.IsNullOrWhiteSpace(kod) && !string.IsNullOrWhiteSpace(nazov))
                            {
                                diagnozy.Add(new Diagnoza { KodDiagnozy = kod, Nazov = nazov });
                            }
                        }
                    }
                }
            }
            diagnozy = diagnozy
                .GroupBy(d => d.KodDiagnozy)
                .Select(g => g.First()) // Vyberie len prvý unikátny záznam
                 .ToHashSet();

            // 🔥 Odstránenie už existujúcich záznamov

            await context.Diagnozy.AddRangeAsync(diagnozy);
                await context.SaveChangesAsync();
                logger.LogInformation($"📋 Pridaných {diagnozy.Count} nových diagnóz.");
           
        }
        public static async Task ImportLimityZCsv(string csvFilePath, ApplicationDbContext context)
        {
            using (var reader = new StreamReader(csvFilePath, Encoding.UTF8)) // Uistíme sa, že čítame správne kódovanie
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                TrimOptions = TrimOptions.Trim, // Odstráni skryté medzery
                HeaderValidated = null, // Vypne validáciu hlavičiek
                MissingFieldFound = null // Ignoruje chýbajúce polia
            }))
            {
                // 🔍 DEBUG: Výpis hlavičiek zo súboru
                csv.Read();
                csv.ReadHeader();
                string[] headers = csv.HeaderRecord ?? Array.Empty<string>(); // Ak je null, použije prázdne pole

                Console.WriteLine("Načítané hlavičky CSV: " + string.Join(", ", headers));

                csv.Context.RegisterClassMap<LimitCsvMap>(); // Zaregistrujeme mapovanie hlavičiek
                var limity = csv.GetRecords<LimitCsvModel>().ToList();

                if (!limity.Any())
                {
                    Console.WriteLine("⚠️ Upozornenie: CSV neobsahuje žiadne platné záznamy!");
                    return;
                }

                var existujuceKategoriaDoplnky = await context.KategoriaDoplnkov.ToListAsync();
                var noveLimity = new List<LimitPredpisu>();

                string poslednaPodskupina = "";

                foreach (var limit in limity)
                {
                    if (string.IsNullOrEmpty(limit.Trieda))
                    {
                        limit.Trieda = poslednaPodskupina;
                    }
                    else
                    {
                        poslednaPodskupina = limit.Trieda;
                    }

                    var nazvy = RozdelTriedu(limit.Trieda);
                    string kategoriaNazov = nazvy.Item1;
                    string doplnokNazov = nazvy.Item2;

                    var kategoria = existujuceKategoriaDoplnky.FirstOrDefault(k => k.Kod == kategoriaNazov);
                    if (kategoria == null)
                    {
                        continue;
                    }

                    var novyLimit = new LimitPredpisu
                    {
                        Id = Guid.NewGuid(),
                        LimitValue = TryParseInt(limit.LimitValue),
                        WeeksLimit = TryParseInt(limit.WeekLimit),
                        MonthsLimit = TryParseInt(limit.MonthlyLimit),
                        YearsLimit = TryParseInt(limit.YearlyLimit),
                        CasovyOkamih = DateTime.UtcNow
                    };

                    noveLimity.Add(novyLimit);
                    kategoria.LimitPredpisu = novyLimit;
                }

                await context.LimityPredpisov.AddRangeAsync(noveLimity);
                await context.SaveChangesAsync();
            }
        }


        private static int? TryParseInt(string? value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }

        private static (string, string) RozdelTriedu(string trieda)
        {
            if (string.IsNullOrEmpty(trieda))
            {
                return ("Neznáma kategória", "Neznámy doplnok");
            }

            var casti = trieda.Split(new[] { ' ' }, 2); // Rozdelíme na prvú časť a zvyšok
            return casti.Length > 1 ? (casti[0], casti[1]) : (casti[0], "Nešpecifikovaný doplnok");
        }
        public static async Task ImportAndSeedLiekyAsync(ApplicationDbContext context, string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(2) ?? Enumerable.Empty<IXLRangeRow>();
                if (rows == Enumerable.Empty<IXLRangeRow>())
                {
                    throw new Exception("Excel súbor neobsahuje žiadne listy!");
                }

                var lieky = new List<Liek>();
                var noveKategorie = new List<KategoriaLiekov>();

                KategoriaLiekov? aktualnaKategoria = null; // Posledná aktívna kategória

                foreach (var row in rows)
                {
                    var kod = row.Cell(1).GetValue<string>();          // Kód lieku
                    var kodKategorie = row.Cell(11).GetValue<string>(); // Kód kategórie (ak existuje)
                    var nazovLieku = row.Cell(6).GetValue<string>();   // Názov lieku
                    var doplnok = row.Cell(7).GetValue<string>();      // Doplnkové info
                    var odbornost = row.Cell(18).GetString().Trim().Split(',').Select(s => s.Trim()).ToList();

                    if (string.IsNullOrEmpty(kodKategorie))
                    {
                        if (aktualnaKategoria != null)
                        {
                            if (!aktualnaKategoria.Kod.Equals(kod))
                            {
                                // 🏥 Ak kodKategorie je prázdne → vytvoríme novú kategóriu
                                aktualnaKategoria = new KategoriaLiekov
                                {
                                    Kod = kod,
                                    Nazov = nazovLieku
                                };

                                noveKategorie.Add(aktualnaKategoria);
                            }
                        } else
                        {
                            aktualnaKategoria = new KategoriaLiekov
                            {
                                Kod = kod,
                                Nazov = nazovLieku
                            };

                            noveKategorie.Add(aktualnaKategoria);
                        }
                    }
                    else if (aktualnaKategoria != null)
                    {
                        // 💊 Ak kodKategorie existuje → použijeme poslednú kategóriu (NEVYTVÁRAME NOVÚ!)
                        var liek = new Liek
                        {
                            Kod = kod,
                            Nazov = nazovLieku,
                            NazovDoplnku = doplnok,
                            KodKategorie = aktualnaKategoria.Kod,
                            Kategoria = aktualnaKategoria,
                            PO = odbornost
                        };

                        lieky.Add(liek);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Chyba: Liek {kod} nemá platnú kategóriu!");
                    }
                }

                // ✅ Uloženie kategórií
                if (noveKategorie.Count > 0)
                {
                    await context.KategoriaLiekov.AddRangeAsync(noveKategorie);
                    await context.SaveChangesAsync();
                }

                // ✅ Uloženie liekov
                if (lieky.Count > 0)
                {
                    await context.Lieky.AddRangeAsync(lieky);
                    await context.SaveChangesAsync();
                }
            }
        }
        public static async Task ImportOdbornostAsync(ApplicationDbContext context, string filePath)
        {
            var odbornostList = new List<OdbornostLekara>();

            using (var parser = new TextFieldParser(filePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(","); // Nastavíme oddeľovač CSV súboru

                bool isFirstRow = true;

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (isFirstRow ) // Preskočíme prvý riadok (hlavičku)
                    {
                        isFirstRow = false;
                        continue;
                    }

                    if (fields?.Length >= 2) // Overíme, či CSV má minimálne 2 stĺpce
                    {
                        var identifikator = fields[0].Trim();
                        var popis = fields[1].Trim();

                        if (!string.IsNullOrEmpty(identifikator) && !string.IsNullOrEmpty(popis))
                        {
                            odbornostList.Add(new OdbornostLekara
                            {
                                Identifikator = identifikator,
                                PopisOdbornosti = popis
                            });
                        }
                    }
                }
            }

        

            await context.OdbornostiLekarov.AddRangeAsync(odbornostList);
            await context.SaveChangesAsync();
        }
        public static async Task ImportDoplnkyAsync(ApplicationDbContext context, string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Súbor {filePath} neexistuje.");
                return;
            }

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // Prvý sheet

            var kategorie = new Dictionary<string, KategoriaDoplnok>();
            var doplnky = new List<Doplnok>();

            // 🟢 1️⃣ Načítanie existujúcich kategórií z DB (aby sa neduplikovali)
            var existujuceKategorie = await context.KategoriaDoplnkov.ToDictionaryAsync(k => k.Kod);

            // 🟢 2️⃣ Pridanie ROOT kategórie, ak neexistuje
            if (!existujuceKategorie.ContainsKey("Default"))
            {
                var rootKategoria = new KategoriaDoplnok
                {
                    Kod = "Default",
                    Nazov = "Default",
                    NadriadenaKategorieKod = null
                };
                existujuceKategorie["Default"] = rootKategoria;
                context.KategoriaDoplnkov.Add(rootKategoria);
            }

            foreach (var row in worksheet.RowsUsed().Skip(1)) // Preskočíme hlavičku
            {
                var nazov = row.Cell(5).GetString().Trim(); // Stĺpec "Názov"
                var podskupina = row.Cell(2).GetString().Trim(); // Stĺpec "Podskupina"
                var kodDoplnku = row.Cell(4).GetString().Trim(); // Stĺpec "Podskupina"
                var doplnokNazvu = row.Cell(6).GetString().Trim(); // Stĺpec "Doplnok názvu"
                var odbornost = row.Cell(17).GetString().Trim().Split(',').Select(s => s.Trim()).ToList();

                if (!string.IsNullOrEmpty(podskupina) && !string.IsNullOrEmpty(nazov))
                {
                    doplnky.Add(new Doplnok
                    {
                        Kod = kodDoplnku,
                        Nazov = nazov,
                        NazovDoplnku = doplnokNazvu,
                        KodKategorie = podskupina,
                        PO = odbornost
                    });
                    continue;
                }
                // 🔹 Extrahovanie kódu a názvu kategórie
                var parts = nazov.Split(' ', 2);
                if (parts.Length < 2) continue;

                var skupinaKod = parts[0]; // Napr. "A1"
                var skupinaNazov = parts[1]; // Napr. "Obväz hydrofilný"

                // 🟢 3️⃣ Hľadanie nadradenej kategórie
                string nadradenaKod = skupinaKod.Contains('.')
                    ? skupinaKod.Substring(0, skupinaKod.LastIndexOf('.')) // Napr. A1.1 -> A1
                    : "Default"; // Hlavné kategórie idú pod ROOT

                // 🟢 4️⃣ Pridanie kategórie, ak neexistuje
                if (!existujuceKategorie.ContainsKey(skupinaKod))
                {
                    var novaKategoria = new KategoriaDoplnok
                    {
                        Kod = skupinaKod,
                        Nazov = skupinaNazov,
                        NadriadenaKategorieKod = nadradenaKod
                    };

                    existujuceKategorie[skupinaKod] = novaKategoria;
                    context.KategoriaDoplnkov.Add(novaKategoria);
                }

                // 🟢 5️⃣ Pridanie doplnku pod správnu podkategóriu
             
            }

            // 🟢 6️⃣ Uloženie kategórií do databázy
            await context.SaveChangesAsync();

            // 🟢 7️⃣ Uloženie doplnkov do databázy
            if (doplnky.Count > 0)
            {
                await context.Doplnky.AddRangeAsync(doplnky);
                await context.SaveChangesAsync();
            }
        }
        public static async Task ImportPotravinyAsync(ApplicationDbContext context, string filePath)
        {
             if (!File.Exists(filePath))
        {
            Console.WriteLine($"Súbor {filePath} neexistuje.");
            return;
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1); // Prvý sheet

        var kategorie = new Dictionary<string, KategoriaPotravina>();
        var potraviny = new List<Potravina>();
        string poslednaPodskupina = "Default"; // Uchováva poslednú podskupinu pre null hodnoty

        // Načítanie existujúcich kategórií z DB
        var existujuceKategorie = await context.KategoriaPotraviny.ToDictionaryAsync(k => k.Kod);

        // Pridanie ROOT kategórie, ak neexistuje
        if (!existujuceKategorie.ContainsKey("Default2"))
        {
            var rootKategoria = new KategoriaPotravina
            {
                Kod = "Default2",
                Nazov = "Root",
                NadriadenaKategorieKod = null
            };
            existujuceKategorie["Default"] = rootKategoria;
            context.KategoriaPotraviny.Add(rootKategoria);
        }

        foreach (var row in worksheet.RowsUsed().Skip(2)) // Preskočíme hlavičku
        {
            var nazov = row.Cell(4).GetString().Trim(); // Stĺpec "Názov"
            var skupinaKod = row.Cell(1).GetString().Trim(); // Stĺpec "Podskupina"
            var kodPotraviny = row.Cell(2).GetString().Trim(); // Stĺpec "Kód potraviny"
            var doplnokNazvu = row.Cell(5).GetString().Trim(); // Stĺpec "Doplnok názvu"
            var odbornost = row.Cell(15).GetString().Trim().Split(',').Select(s => s.Trim()).ToList();

                if (string.IsNullOrEmpty(skupinaKod))
            {
                skupinaKod = poslednaPodskupina; // Použitie poslednej podskupiny ak je null
            }
            else
            {
                poslednaPodskupina = skupinaKod; // Aktualizácia poslednej podskupiny
            }

            if (!string.IsNullOrEmpty(kodPotraviny))
            {
                potraviny.Add(new Potravina
                {
                    Kod = kodPotraviny,
                    Nazov = nazov,
                    NazovDoplnku = doplnokNazvu,
                    KodKategorie = skupinaKod,
                    PO = odbornost
                });
                continue;
            }

            string nadradenaKod = skupinaKod.Length > 2 ? skupinaKod.Substring(0, skupinaKod.Length - 1) : "Default";
            if (!existujuceKategorie.ContainsKey(nadradenaKod))
                {
                    nadradenaKod = "Default";
                }

                // Pridanie kategórie, ak neexistuje
             if (!existujuceKategorie.ContainsKey(skupinaKod))
                {
                    var novaKategoria = new KategoriaPotravina
                    {
                        Kod = skupinaKod,
                        Nazov = nazov,
                        NadriadenaKategorieKod = nadradenaKod
                    };
                    existujuceKategorie[skupinaKod] = novaKategoria;
                    context.KategoriaPotraviny.Add(novaKategoria);
                }
            }

        // Uloženie kategórií do databázy
        await context.SaveChangesAsync();

        // Uloženie potravín do databázy
        if (potraviny.Count > 0)
        {
            await context.Potraviny.AddRangeAsync(potraviny);
            await context.SaveChangesAsync();
        }
        }
    }
}
public class LimitCsvMap : ClassMap<LimitCsvModel>
{
    public LimitCsvMap()
    {
        Map(m => m.Podskupina).Name("PODSKUPINA");
        Map(m => m.Trieda).Name("Trieda");
        Map(m => m.MnozstvovyLimit).Name("Množstvový limit");
        Map(m => m.LimitValue).Name("Limit Value");
        Map(m => m.WeekLimit).Name("Limit Unit"); // Ak máš iný stĺpec, uprav
        Map(m => m.MonthlyLimit).Name("Monthly Limit");
        Map(m => m.YearlyLimit).Name("Yearly Limit");
    }
}

public class LimitCsvModel
{
    public string? Podskupina { get; set; } = default!;
    public string? Trieda { get; set; } =default!;
    public string? MnozstvovyLimit { get; set; } = default!;
    public string? LimitValue { get; set; } = default!;
    public string? WeekLimit { get; set; } = default!;
    public string? MonthlyLimit { get; set; } = default!;
    public string? YearlyLimit { get; set; } = default!;
}
