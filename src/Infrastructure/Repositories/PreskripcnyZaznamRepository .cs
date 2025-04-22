using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using System.Text;
using Application.Common.Models;
using System.Globalization;
using Infrastructure.Repositories;
using System.ServiceModel;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Infrastructure.Persistence;
using Bogus;

public class PreskripciaService : IPreskripciaService
{
    private readonly IApplicationDbContext _context;

    public PreskripciaService(IApplicationDbContext context)
    {
        _context = context;
    }
    // Diazgnózy
    public async Task<List<Diagnoza>> GetDiagnozyAsync()
    => await _context.Diagnozy.ToListAsync();

    // 💊 Lieky
    public async Task<List<Liek>> GetLiekyAsync()
        => await _context.Lieky.ToListAsync();

    public async Task<Liek?> GetLiekByKodAsync(string kod)
        => await _context.Lieky.FirstOrDefaultAsync(l => l.Kod == kod);

    // 📦 Kategórie liekov
    public async Task<List<KategoriaLiekov>> GetKategoriaLiekovAsync()
        => await _context.KategoriaLiekov.ToListAsync();

    public async Task<List<Vyrobky>> GetPotravinyByKategoriaAsync(string kodKategorie)
    {
        // 1️⃣ Získame všetky podkategórie v rámci podstromu
        var kategoriePodstrom = await GetPodstromKategoriiPotravinAsync(kodKategorie);

        // 2️⃣ Extrahujeme všetky kódy kategórií z podstromu
        var kodyKategorii = kategoriePodstrom.Select(k => k.Kod).ToList();
        kodyKategorii.Add(kodKategorie); // Pridáme aj aktuálnu kategóriu

        // 3️⃣ Vyhľadáme všetky potraviny patriace do týchto kategórií
        var zaznamy = await _context.Potraviny
            .Where(p => kodyKategorii.Contains(p.KodKategorie.Trim()))
            .ToListAsync();

        // 4️⃣ Mapujeme z `Potravina` na `Potraviny`
        var potravinyDto = zaznamy.Select(p => new Vyrobky
        {
            KodLieku = p.Kod,
            KodKategorie = p.KodKategorie,
            Nazov = p.Nazov,
            Doplnok = p.NazovDoplnku  // Ošetrenie možnej hodnoty NULL
        }).ToList();

        return potravinyDto;
    }

    public async Task<List<KategoriaDTO>> GetKategoriaDoplnkovAsync()
    {
        var kategorie = await _context.KategoriaDoplnkov
            .AsNoTracking() // Zabraňuje cyklickým referenciám
            .Include(k => k.Podkategorie) // Načítanie podkategórií
            .ToListAsync();

        // Transformácia do DTO (bez cyklických referencií)
        var dtoList = kategorie.Select(k => new KategoriaDTO
        {
            Kod = k.Kod,
            Nazov = k.Nazov,
            Podkategorie = k.Podkategorie.Select(pk => new KategoriaDTO
            {
                Kod = pk.Kod,
                Nazov = pk.Nazov
            }).ToList()
        }).ToList();

        return dtoList;
    }

    public async Task<List<Vyrobky>> GetDoplnkyByKategoriaAsync(string kodKategorie)
    {
        // 1️⃣ Získame všetky podkategórie v rámci podstromu
        var kategoriePodstrom = await GetPodstromKategoriiAsync(kodKategorie);

        // 2️⃣ Extrahujeme všetky kódy kategórií z podstromu
        var kodyKategorii = kategoriePodstrom.Select(k => k.Kod).ToList();
        kodyKategorii.Add(kodKategorie); // Pridáme aj aktuálnu kategóriu
        // 3️⃣ Vyhľadáme všetky doplnky patriace do týchto kategórií
        var zaznamy = await _context.Doplnky
            .Where(d => kodyKategorii.Contains(d.KodKategorie.Trim()))
            .ToListAsync();

        // 4️⃣ Mapujeme z `Doplnok` na `Doplnoky`
        var doplnkyDto = zaznamy.Select(d => new Vyrobky
        {
            KodLieku = d.Kod,   // Ak sa nejedná o liek, môže byť premenované
            KodKategorie = d.KodKategorie,
            Nazov = d.Nazov,
            Doplnok = d.NazovDoplnku   // Ošetrenie možnej hodnoty NULL
        }).ToList();

        return doplnkyDto;
    }


    public async Task<List<Doplnok>> GetDoplnkyAsync()
    {
        return await _context.Doplnky.ToListAsync();
    }

    // 🏥 Lekári
    public async Task<List<Lekar>> GetLekariAsync()
        => await _context.Lekari.ToListAsync();


    // 📜 Predpisy
    public async Task<List<PrekricnyZaznam>> GetPredpisyAsync()
        => await _context.PreskripcneZaznamy
                        .Include(p => p.Pacient)
                        .Include(p => p.Lekar)
                        .ToListAsync();

    public async Task AddPredpisAsync(PrekricnyZaznam predpis)
    {
        await _context.PreskripcneZaznamy.AddAsync(predpis);
        await _context.SaveChangesAsync(default);
    }

    // 🔍 Overenie interakcií liekov pacienta (Volá IInterakciaService)
    public async Task<InterakciaResponse> OverInterakciePacientaAsync(
      string? rC, string? iCP, string[] kodyLiekov,
      string obdobieOd, string obdobieDo)
    {
        var pacient = OverPacientaAsync(rC, iCP).Result;
        if (pacient == null)
        {
            return new InterakciaResponse
            {
                InterakcieLiekovPacienta = null,
                Dokument = new APIDokument
                {
                    NazovDokumentu = "InterakcieLiekovPacienta.html",
                    Typ = "text/html",
                    Obsah = null,
                    Komprimovany = false,
                    Poznamka = "Pacient neexistuje."
                }
            };
        }

        var existujuceLieky = await _context.PrekricnyZaznamLiekov
            .Include(pl => pl.Liek)
            .Where(pl => _context.PreskripcneZaznamy
                .Any(p => p.Id == pl.PrekricnyZaznamId && p.Pacient != null && p.Pacient.Pouzivatel.RodneCislo == rC))
            .Select(pl => pl.Liek.Kod)
            .Distinct()
            .ToListAsync();
        if (existujuceLieky.Count > 0)
        {
            return new InterakciaResponse
            {
                InterakcieLiekovPacienta = null,
                Dokument = new APIDokument
                {
                    NazovDokumentu = "InterakcieLiekovPacienta.html",
                    Typ = "text/html",
                    Obsah = null,
                    Komprimovany = false,
                    Poznamka = "Lieky name predpisane."
                }
            };
        }

        var interakcieLieky = kodyLiekov.Intersect(existujuceLieky).ToList();


        // 📌 **Vytvorenie HTML obsahu**
        var reportContent = new StringBuilder();
        reportContent.AppendLine("<!DOCTYPE html>");
        reportContent.AppendLine("<html lang='sk'>");
        reportContent.AppendLine("<head>");
        reportContent.AppendLine("<meta charset='UTF-8'>");
        reportContent.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        reportContent.AppendLine("<title>Overenie Interakcií</title>");
        reportContent.AppendLine("<style> body { font-family: Arial, sans-serif; padding: 20px; } h2 { color: #d9534f; } p { font-size: 16px; } </style>");
        reportContent.AppendLine("</head>");
        reportContent.AppendLine("<body>");
        reportContent.AppendLine("<h2>🔍 Výsledky overenia interakcií</h2>");

        if (interakcieLieky.Any())
        {
            reportContent.AppendLine("<h3>Interakcie s liekmi</h3>");
            foreach (var interakcia in interakcieLieky)
            {
                reportContent.AppendLine($"<p>⚠️ Možná interakcia s liekom: {interakcia}</p>");
            }
        }
       
        if (!interakcieLieky.Any())
        {
            reportContent.AppendLine("<p>✅ Neboli zistené žiadne interakcie.</p>");
        }

        reportContent.AppendLine("</body></html>");

        // 📌 **Uloženie HTML súboru**
        string htmlFilePath = Path.Combine(Path.GetTempPath(), "InterakcieLiekovPacienta.html");
        await File.WriteAllTextAsync(htmlFilePath, reportContent.ToString());

        byte[] fileContent = await File.ReadAllBytesAsync(htmlFilePath);
        string fileType = "text/html";
        string fileExtension = ".html";

        // 📌 **Konverzia na Base64**
        var base64Result = Convert.ToBase64String(fileContent);

        return new InterakciaResponse
        {
            InterakcieLiekovPacienta = base64Result,
            Dokument = new APIDokument
            {
                NazovDokumentu = "InterakcieLiekovPacienta" + fileExtension,
                Typ = fileType,
                Obsah = fileContent,
                Komprimovany = false,
                Poznamka = "Generované automaticky"
            }
        };

    }
    // 🏥 Overenie Preskripčného záznamu
    public async Task<Poistenie> OverPoistencaAsync(string? rC, string? iCP, DateTime? datum)
    {
        var pacient = await _context.Pacienti.Include(p => p.Poistenie)
        .FirstOrDefaultAsync(p => p.Pouzivatel.RodneCislo == rC);

        Poistenie poistenie;

        if (pacient == null || pacient.Poistenie == null)
        {
            poistenie = new Poistenie
            {
                ICP = iCP,
                Datum = datum,
                PoistnyVztahPoistenca = PoistnyVztahPoistencaEnum.NieJePoistencom,
                JeNeplatic = true,
                ZaciatokEuPoistenia = null,
                MaNarokNaOdkladnuZS = false

            };

            _context.Poistenia.Add(poistenie);
            await _context.SaveChangesAsync(default);
        }
        else
        {
            poistenie = pacient.Poistenie;
        }

        return poistenie;
    }

    public async Task<UlozPreskripcnyZaznamResponse> UlozPreskripcnyZaznamAsync(string tokenPrimatela, ApiPreskripcnyZaznam preskripcny)
    {
        var response = new UlozPreskripcnyZaznamResponse();
        var spravy = new List<APISprava>();

        if (string.IsNullOrEmpty(tokenPrimatela) || preskripcny == null)
            return new UlozPreskripcnyZaznamResponse { Spravy = Response.VytvorChybovuOdpoved("ERR001", "Neplatné vstupné údaje.") };

        var primatel = await OverPrimatelaAsync(tokenPrimatela);
        if (primatel == null)
            return new UlozPreskripcnyZaznamResponse { Spravy = Response.VytvorChybovuOdpoved("ERR002", "Primatel neexistuje alebo nemá oprávnenie predpisovať.") };

        var lekar = await OverLekaraAsync(preskripcny.LekarId);
        if (lekar?.KodOdbornoti == null)
            return new UlozPreskripcnyZaznamResponse { Spravy = new List<APISprava> { new APISprava { Zavaznost = ZavaznostSpravy.CHYBA, Kod = "ERR007", Text = "Lekár nemá priradenú odbornosť." } }, PoznamkaZP = "Lekár nemá priradenú odbornosť." };

        var pacient = await OverPacientaAsync(preskripcny.RC, preskripcny.ICP);
        if (pacient == null || !_context.Poistenia.Any(p => p.Id == pacient.Pacient.PoistenieId && p.PoistnyVztahPoistenca != PoistnyVztahPoistencaEnum.NieJePoistencom))
            return new UlozPreskripcnyZaznamResponse { Spravy = Response.VytvorOdmietnutieOdpoved("ODM001", "Pacient nemá platné poistenie.") };

        if (!await _context.Ambulancie.AnyAsync(a => a.Kod == preskripcny.AmbulanciaKod))
            return new UlozPreskripcnyZaznamResponse { Spravy = Response.VytvorChybovuOdpoved("ERR005", "Neplatný kód ambulancie.") };

        if (!await _context.Diagnozy.AnyAsync(d => d.KodDiagnozy == preskripcny.DiagnozaKod))
            return new UlozPreskripcnyZaznamResponse { Spravy = Response.VytvorChybovuOdpoved("ERR006", "Neplatný kód Diagnozy.") };

        // ✅ Overenie doplnkov cez SkontrolujPredpisPomocky
        var platneDoplnky = new List<APIDoplnky>();
        foreach (var doplnok in preskripcny.Doplnky!)
        {
            var vysledok = await SkontrolujPredpisPomocky(preskripcny.RC ?? "", doplnok.Kod, doplnok.Mnozstvo , DateTime.UtcNow, lekar.KodOdbornoti, preskripcny.DiagnozaKod);
            if (vysledok.PovolenyPredpis)
            {
                platneDoplnky.Add(doplnok);
            }
            else
            {
                spravy.Add(new APISprava
                {
                    Zavaznost = ZavaznostSpravy.CHYBA,
                    Kod = "ERR013",
                    Text = $"Doplnok {doplnok.Kod} nie je možné predpísať: {string.Join("; ", vysledok.Obmedzenia.Select(o => o.Nazov))}"
                });
            }
        }

        var lieky = await _context.Lieky.Where(l => preskripcny.Lieky!.Contains(l.Kod)).ToListAsync();
        var potraviny = await _context.Potraviny.Where(p => preskripcny.Potraviny!.Contains(p.Kod)).ToListAsync();

        bool maAspoňJedenProdukt = lieky.Any() || platneDoplnky.Any() || potraviny.Any();
        if (!maAspoňJedenProdukt)
        {
            response.Spravy.Add(new APISprava
            {
                Zavaznost = ZavaznostSpravy.CHYBA,
                Kod = "ERR012",
                Text = "Aspoň jeden produkt (liek, doplnok alebo potravina) musí byť predpísaný."
            });
            return response;
        }

        // ✅ Overenie oprávnenia lekára predpisovať jednotlivé produkty
        bool OverOpravnenie(IEnumerable<dynamic>? produkty, string kodOdbornosti, string errKod, string errMsg)
        {
            if (produkty == null || !produkty.Any()) return true;

            var neopravnene = produkty
                .Where(p => p.PO is List<string> poList && poList.Where(po => !string.IsNullOrWhiteSpace(po)).Any() && !poList.Contains(kodOdbornosti))
                .Select(p => p.Kod)
                .ToList();

            if (neopravnene.Any())
            {
                spravy.Add(new APISprava
                {
                    Zavaznost = ZavaznostSpravy.CHYBA,
                    Kod = errKod,
                    Text = $"{errMsg}: {string.Join(", ", neopravnene)}"
                });
                return false;
            }

            return true;
        }
        var doplnky = await _context.Doplnky
        .Where(d => preskripcny.Doplnky.Select(pd => pd.Kod).Contains(d.Kod))
        .ToListAsync();
        if (!OverOpravnenie(lieky, lekar.KodOdbornoti, "ERR008", "Lekár nemá oprávnenie predpísať tieto lieky") ||
            !OverOpravnenie(doplnky, lekar.KodOdbornoti, "ERR009", "Lekár nemá oprávnenie predpísať tieto doplnky") ||
            !OverOpravnenie(potraviny, lekar.KodOdbornoti, "ERR011", "Lekár nemá oprávnenie predpísať tieto potraviny"))
        {
            response.Spravy = spravy;
            return response;
        }

        // ✅ Vytvorenie preskripčného záznamu
        var novyZaznam = new PrekricnyZaznam
        {
            Id = Guid.NewGuid(),
            PacientId = pacient.Pacient.Id,
            LekarId = lekar.Id,
            DatumPredpisu = DateTime.UtcNow,
            PocetDni = preskripcny.PocetDni,
            Stav = "Aktívny",
            AmbulanciaKod = preskripcny.AmbulanciaKod,
            DiagnozaKod = preskripcny.DiagnozaKod
        };
        var doplnkyIds = await _context.Doplnky
        .Where(d => platneDoplnky.Select(pd => pd.Kod).Contains(d.Kod))
        .ToDictionaryAsync(d => d.Kod, d => d.Id);

     
        await _context.PreskripcneZaznamy.AddAsync(novyZaznam);
        // Ukladanie do PrekricnyZaznamDoplnok s korektným GUID Id a správnym množstvom
        await _context.PrekricnyZaznamDoplnok.AddRangeAsync(
            platneDoplnky
                .Where(d => doplnkyIds.ContainsKey(d.Kod)) // Kontrola, či kód existuje v databáze
                .Select(d => new PrekricnyZaznamPomocky
                {
                    Id = Guid.NewGuid(),
                    PrekricnyZaznamId = novyZaznam.Id,
                    DoplnokId = doplnkyIds[d.Kod], // Použitie správneho GUID Id z databázy 
                    Mnozstvo = d.Mnozstvo // Použitie množstva z platneDoplnky
                })
        );
        // ✅ Ukladanie dát do databázy
        await _context.PrekricnyZaznamLiekov.AddRangeAsync(lieky.Select(l => new PrekricnyZaznamLiek { Id = Guid.NewGuid(), PrekricnyZaznamId = novyZaznam.Id, LiekId = l.Id }));
        await _context.PrekricnyZaznamPotraviny.AddRangeAsync(potraviny.Select(p => new PrekricnyZaznamPotraviny { Id = Guid.NewGuid(), PrekricnyZaznamId = novyZaznam.Id, PotravinaId = p.Id }));

        await _context.SaveChangesAsync();

        return new UlozPreskripcnyZaznamResponse { CiarovyKod = novyZaznam.Id, PoznamkaZP = "Preskripčný záznam bol úspešne uložený.", Spravy = spravy };
    }


    public async Task<StornujPreskripcnyZaznamResponse> StornujPreskripcnyZaznamAsync(Guid idPreskripcnehoZaznamu, Guid idLekara)
    {
        var response = new StornujPreskripcnyZaznamResponse();
        var spravy = new List<APISprava>();
        // Nájsť preskripčný záznam
        var preskripcnyZaznam = await _context.PreskripcneZaznamy
            .FirstOrDefaultAsync(pz => pz.Id == idPreskripcnehoZaznamu);

        if (preskripcnyZaznam == null)
        {
            spravy.Add(new APISprava
            {
                Zavaznost = ZavaznostSpravy.CHYBA,
                Kod = "ERR003",
                Text = "Preskripčný záznam neexistuje."
            });

            response.Stornovany = false;
            response.Spravy = spravy;
            return response;
        }

        // Overenie, či je záznam stále aktívny
        if (preskripcnyZaznam.Stav != "Aktívny")
        {
            spravy = Response.VytvorOdmietnutieOdpoved("ODM004", "Tento preskripčný záznam už je neaktivy.");
            response.Stornovany = false;
            response.Spravy = spravy;
            return response;
        }
        if (preskripcnyZaznam.LekarId != idLekara)
        {
            spravy = Response.VytvorOdmietnutieOdpoved("ODM005", "Nemáte oprávnenie stornovať tento preskripčný záznam.");
            response.Stornovany = false;
            response.Spravy = spravy;
            return response;
        }
        // Odstránenie preskripčného záznamu z databázy
        var liekyNaOdstranenie = _context.PrekricnyZaznamLiekov
            .Where(pzl => pzl.PrekricnyZaznamId == preskripcnyZaznam.Id)
            .ToList();

        if (liekyNaOdstranenie.Any())
        {
            _context.PrekricnyZaznamLiekov.RemoveRange(liekyNaOdstranenie);
            await _context.SaveChangesAsync(default);
        }
        _context.PreskripcneZaznamy.Remove(preskripcnyZaznam);
        await _context.SaveChangesAsync(default);

        spravy.Add(new APISprava
        {
            Zavaznost = ZavaznostSpravy.INFO,
            Kod = "SUCCESS",
            Text = $"Preskripčný záznam {preskripcnyZaznam.Id} bol úspešne stornovaný."
        });

        response.Spravy = spravy;
        return response;
    }
    public async Task<VyhladajPreskripcnyZaznamResponse> VyhladajPreskripcnyZaznamAsync(string tokenPrijimatelZS, string? rC, string? iCP)
    {
        var response = new VyhladajPreskripcnyZaznamResponse();
        var spravy = new List<APISprava>();

        if (string.IsNullOrEmpty(tokenPrijimatelZS))
        {
            spravy = Response.VytvorChybovuOdpoved("ERR001", "Neplatné vstupné údaje.");
            response.Spravy = spravy;
            return response;
        }

        var primatel = await OverPrimatelaAsync(tokenPrijimatelZS);

        if (primatel == null)
        {
            spravy = Response.VytvorChybovuOdpoved("ERR002", "Primatel neexistuje alebo nemá oprávnenie predpisovať.");
            response.Spravy = spravy;
            return response;
        }

        // ✅ Hľadanie pacienta podľa RC alebo ICP
        var pacient = OverPacientaAsync(rC, iCP).Result;
        if (pacient == null)
        {
            response.Spravy = Response.VytvorInfoOdpoved("ERR003", "Pacient neexistuje alebo nemá platné poistenie.", new List<string>());
            return response;
        }

        var preskripnyZaznmam = await _context.PreskripcneZaznamy
            .Where(pz => pz.PacientId == pacient.Pacient.Id && pz.Stav == "Aktivny")
            .Include(pz => pz.Lekar)
            .Include(pz => pz.Pacient)
            .ToListAsync();

        // ✅ Načítanie platných preskripčných záznamov s liekmi a doplnkami
        var platneZaznamy = await _context.PrekricnyZaznamLiekov
             .Where(pzp => preskripnyZaznmam.Select(pz => pz.Id).Contains(pzp.PrekricnyZaznamId))
             .Include(pzl => pzl.Liek) // Načítanie liekov
            .ToListAsync();

        // ✅ Načítanie doplnkov pre tieto preskripčné záznamy
        var platnePomocky = await _context.PrekricnyZaznamDoplnok
            .Where(pzp => preskripnyZaznmam.Select(pz => pz.Id).Contains(pzp.PrekricnyZaznamId))
            .Include(pzp => pzp.Doplnok) // Načítanie doplnkov
            .ToListAsync();
        var platnePotraviny = await _context.PrekricnyZaznamPotraviny
            .Where(pzp => preskripnyZaznmam.Select(pz => pz.Id).Contains(pzp.PrekricnyZaznamId))
            .Include(pzp => pzp.Potravina) // Načítanie doplnkov
            .ToListAsync();

        if (!platneZaznamy.Any() && !platnePomocky.Any() && !platnePotraviny.Any())
        {
            response.Spravy = Response.VytvorInfoOdpoved("INFO004", "Pacient nemá žiadne platné a nevydané recepty.", new List<string>());
            return response;
        }

        // ✅ Transformácia dát do `ApiPreskripcnyZaznam`
        var apiZaznamy = preskripnyZaznmam
            .GroupBy(pzl => pzl)
            .Select(grp => new ApiPreskripcnyZaznam
            {
                CiarovyKod = grp.Key.Id,
                RC = pacient.RodneCislo,
                LekarId = grp.Key.LekarId,
                AmbulanciaKod = grp.Key.AmbulanciaKod,
                PocetDni = grp.Key.PocetDni,
                DiagnozaKod = grp.Key.DiagnozaKod,
                DatumVydania = grp.Key.DatumPredpisu,
                Lieky = platneZaznamy
                    .Where(pzp => pzp.PrekricnyZaznamId == grp.Key.Id)
                    .Select(pzp => pzp.Liek.Kod)
                    .ToList(),
                // ✅ Pridanie doplnkov k preskripčnému záznamu
                Doplnky = platnePomocky
                .Where(pzp => pzp.PrekricnyZaznamId == grp.Key.Id)
                .Select(pzp => new APIDoplnky
                {
                    Kod = pzp.Doplnok.Kod,
                    Mnozstvo = pzp.Mnozstvo
                 })
                .ToList(),
                Potraviny = platnePotraviny.Where(pzp => pzp.PrekricnyZaznamId == grp.Key.Id)
                    .Select(pzp => pzp.Potravina.Kod)
                    .ToList(),
            })
            .ToList();

        response.PreskripcneZaznamy = apiZaznamy;
        response.Spravy = Response.VytvorInfoOdpoved("SUCCESS", "Načítanie platných a nevydaných receptov bolo úspešné.", new List<string>());

        return response;
    }

    public async Task<ApiPreskripcnyZaznam> DajPreskripcnyZaznam(Guid idPreskripcnehoZaznamu)
    {
        var response = new ApiPreskripcnyZaznam();
        var spravy = new List<APISprava>();



        // Nájsť preskripčný záznam
        var preskripcnyZaznam = await _context.PreskripcneZaznamy
            .FirstOrDefaultAsync(pz => pz.Id == idPreskripcnehoZaznamu);

        if (preskripcnyZaznam == null)
        {
            return response;
        }

        // Načítať lieky spojené s preskripčným záznamom
        var lieky = await _context.PrekricnyZaznamLiekov
            .Where(pzl => pzl.PrekricnyZaznamId == idPreskripcnehoZaznamu)
            .Include(pzl => pzl.Liek) // Načítanie liekov
            .Select(pzl => pzl.Liek.Kod) // Vraciame len kódy liekov
            .ToListAsync();
        var platnePomocky = await _context.PrekricnyZaznamDoplnok
            .Where(pzl => pzl.PrekricnyZaznamId == idPreskripcnehoZaznamu)
            .Include(pzp => pzp.Doplnok) // Načítanie doplnkov
            .Select(pzl => new APIDoplnky
                {
                    Kod = pzl.Doplnok.Kod,
                    Mnozstvo = pzl.Mnozstvo
                })
            .ToListAsync();
           
        // Vrátiť požadovanú štruktúru
        return new ApiPreskripcnyZaznam
        {
            CiarovyKod = preskripcnyZaznam.Id,
            PacientId = preskripcnyZaznam.PacientId,
            Stav = preskripcnyZaznam.Stav,
            LekarId = preskripcnyZaznam.LekarId,
            AmbulanciaKod = preskripcnyZaznam.AmbulanciaKod,
            PocetDni = preskripcnyZaznam.PocetDni,
            DiagnozaKod = preskripcnyZaznam.DiagnozaKod,
            Lieky = lieky, // Kódy liekov,
            Doplnky = platnePomocky
        };


    }
    public async Task<BlokujPreskripcnyZaznamResponse> BlokujPreskripcnyZaznamAsync(APIUdajePreBlokovanie udajePreBlokovanie)
    {
        var response = new BlokujPreskripcnyZaznamResponse();
        var spravy = new List<APISprava>();

        // ** Overenie existencie lekárne alebo výdajne **
        var ambulancia = await _context.Ambulancie
            .FirstOrDefaultAsync(l => l.Kod == udajePreBlokovanie.IdAmbulancie);

        if (ambulancia == null)
        {

            spravy.Add(new APISprava
            {
                Zavaznost = ZavaznostSpravy.CHYBA,
                Kod = "ERR002",
                Text = "Ambulancia neexistuje"
            });

            response.CiarovyKod = udajePreBlokovanie.CiarovyKod;
            response.DatumCasBlokovaniaDo = DateTime.Now;
            response.Spravy = spravy;
            return response;

        }

        // ** Overenie existencie preskripčného záznamu **
        var preskripcnyZaznam = await _context.PreskripcneZaznamy
            .FirstOrDefaultAsync(pz => pz.Id == udajePreBlokovanie.CiarovyKod);

        if (preskripcnyZaznam == null)
        {
            spravy.Add(new APISprava
            {
                Zavaznost = ZavaznostSpravy.CHYBA,
                Kod = "ERR003",
                Text = "Preskripčný záznam neexistuje."
            });

            response.CiarovyKod = udajePreBlokovanie.CiarovyKod;
            response.DatumCasBlokovaniaDo = DateTime.Now;
            response.Spravy = spravy;
            return response;

        }

        // ** Overenie, či už nie je blokovaný **
        if (preskripcnyZaznam.AmbulanciaKod != ambulancia.Kod)
        {
            throw new FaultException<APISprava>(new APISprava
            {
                Zavaznost = ZavaznostSpravy.ODMIETNUTIE,
                Kod = "ERR004",
                Text = "Preskripčný záznam je už blokovaný inou ambulanciou."
            });
        }

        // ** Aktualizácia záznamu a blokovanie na určitý čas **
        preskripcnyZaznam.Stav = "Blocnute";
        await _context.SaveChangesAsync(default);

        // ** Odpoveď so správou **
        response.CiarovyKod = preskripcnyZaznam.Id;
        response.DatumCasBlokovaniaDo = DateTime.Now;
        response.Spravy.Add(new APISprava
        {
            Zavaznost = ZavaznostSpravy.INFO,
            Kod = "SUCCESS",
            Text = $"Preskripčný záznam bol úspešne blokovaný od {DateTime.Now:O}."
        });

        return response;
    }
    public async Task<ZrusBlokovaniePreskripcnehoZaznamuResponse> ZrusBlokovaniePreskripcnehoZaznamuAsync(APIUdajePreOdBlocovanie udajePreOdblokovanie)
    {
        var response = new ZrusBlokovaniePreskripcnehoZaznamuResponse();
        var spravy = new List<APISprava>();
        response.CiarovyKod = udajePreOdblokovanie.CiarovyKod;

        // Overenie existencie záznamu
        var preskripcnyZaznam = await _context.PreskripcneZaznamy
            .FirstOrDefaultAsync(pz => pz.Id == udajePreOdblokovanie.CiarovyKod);

        if (preskripcnyZaznam == null)
        {
            spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.CHYBA, Kod = "ERR002", Text = "Preskripčný záznam neexistuje." });
            response.Spravy = spravy;
            return response;
        }

        // Overenie, či je záznam blokovaný
        if (preskripcnyZaznam.Stav != "Blokovaný")
        {
            spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.CHYBA, Kod = "ERR003", Text = "Tento preskripčný záznam nie je blokovaný." });
            response.Spravy = spravy;
            return response;
        }

        // Overenie, či má používateľ oprávnenie na odblokovanie (musí to byť ten istý subjekt, ktorý blokoval)
        if (preskripcnyZaznam.AmbulanciaKod != udajePreOdblokovanie.IdAmbulancie)
        {
            spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.ODMIETNUTIE, Kod = "ERR004", Text = "Ambulancia nemá oprávnenie na odblokovanie tohto záznamu." });
            response.Spravy = spravy;
            return response;
        }

        // Odblokovanie preskripčného záznamu
        preskripcnyZaznam.Stav = "Aktívny";
        await _context.SaveChangesAsync(default);

        spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.INFO, Kod = "SUCCESS", Text = $"Blokovanie preskripčného záznamu {preskripcnyZaznam.Id} bolo úspešne zrušené." });

        response.Spravy = spravy;
        return response;
    }
    //public async Task<ZneplatniPreskripcnyZaznamResponse> ZneplatniPreskripcnyZaznamAsync(Guid udaje)
    //{
    //    var response = new ZneplatniPreskripcnyZaznamResponse();
    //    var spravy = new List<APISprava>();

    //    // Overenie vstupných údajov
    //    var preskripcnyZaznam = await _context.PreskripcneZaznamy
    //        .FirstOrDefaultAsync(pz => pz.Id == udaje);

    //    if (preskripcnyZaznam == null) return response;

    //    // Overenie, či je opakovaný predpis
    //    if (!preskripcnyZaznam.JeOpakovany)
    //    {
    //        spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.CHYBA, Kod = "ERR005", Text = "Tento predpis nie je opakovaný, nie je možné ho zneplatniť." });
    //        response.Spravy = spravy;
    //        return response;
    //    }

    //    // Zneplatnenie predpisu
    //    preskripcnyZaznam.DatumPlatnostiDo = DateTime.UtcNow;

    //    await _context.SaveChangesAsync();

    //    response.IdPreskripcnehoZaznamu = preskripcnyZaznam.Id;
    //    response. = preskripcnyZaznam.DatumPlatnostiDo;
    //    spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.INFO, Kod = "SUCCESS", Text = "Predpis bol úspešne zneplatnený." });
    //    response.Spravy = spravy;

    //    return response;
    //}

    private async Task<Pouzivatel?> OverPrimatelaAsync(string tokenPrimatela)
    {
        return await _context.Pouzivatelia.Where(p => p.JePrimatel == true && p.TokenPrimatela == tokenPrimatela).FirstOrDefaultAsync();

    }

    private async Task<Lekar?> OverLekaraAsync(Guid? lekarId)
    {
        return await _context.Lekari.Include(p => p.Pouzivatel)
             .FirstOrDefaultAsync(p => p.Id == lekarId);
    }
    private async Task<List<KategoriaDoplnok>> GetPodstromKategoriiAsync(string kodKategorie)
    {
        // 1️⃣ Načítame všetky kategórie z databázy
        var vsetkyKategorie = await _context.KategoriaDoplnkov.ToListAsync();

        // 2️⃣ Rekurzívne nájdeme všetky podkategórie
        List<KategoriaDoplnok> podstrom = new();
        GetPodkategorieRekurzivne(vsetkyKategorie, kodKategorie, podstrom);

        return podstrom;
    }
    private void GetPodkategorieRekurzivne(List<KategoriaDoplnok> vsetkyKategorie, string nadradenaKod, List<KategoriaDoplnok> podstrom)
    {
        var podkategorie = vsetkyKategorie.Where(k => k.NadriadenaKategorieKod == nadradenaKod).ToList();

        foreach (var podkategoria in podkategorie)
        {
            podstrom.Add(podkategoria);
            GetPodkategorieRekurzivne(vsetkyKategorie, podkategoria.Kod, podstrom);
        }
    }
    private async Task<PacientDto?> OverPacientaAsync(string? RC, string? ICP)
    {
        var pouzivatel = await _context.Pouzivatelia
            .Where(p => p.RodneCislo == RC || (RC == null && p.Pacient != null && p.Pacient.Poistenie != null && p.Pacient.Poistenie.ICP == ICP))
            .Include(p => p.Pacient)
            .Include(p => p.Pacient!.Poistenie)
            .FirstOrDefaultAsync();

        if (pouzivatel == null || pouzivatel.Pacient == null)
            return null; // Ak používateľ neexistuje alebo nemá pacienta, vráť null

        return new PacientDto
        {
            RodneCislo = pouzivatel.RodneCislo,
            ICP = pouzivatel.Pacient.Poistenie?.ICP, // Ak Poistenie existuje, vráti ICP, inak null
            Pacient = pouzivatel.Pacient
        };
    }


    public async Task<string> SpravPrimatelov(string supertokenPrimatela, List<Guid> idPouzivatelov)
    {
        // 1. Overí Admina – nájde prvého používateľa s Typ == "Admin"
        var adminUser = _context.Pouzivatelia
            .Where(u => u.Typ == "Admin")
            .FirstOrDefault();

        // Overí, či existuje a či token sedí
        if (adminUser == null || adminUser.TokenPrimatela != supertokenPrimatela)
        {
            return "Chyba: Admin neexistuje alebo nesprávny token.";
        }

        // 2. Skontroluje používateľov – načíta všetkých podľa zadaných ID
        var ziadaniPouzivatelia = _context.Pouzivatelia
            .Where(u => idPouzivatelov.Contains(u.Id))
            .ToList();

        // Overí, či počet nájdených používateľov zodpovedá počtu zadaných ID
        if (ziadaniPouzivatelia.Count != idPouzivatelov.Count())
        {
            return "Chyba: Niektorý z uvedených používateľov neexistuje.";
        }

        // 3. Nastaví primateľstvo – nastaví príznak a vygeneruje nový token pre každého používateľa
        foreach (var pouzivatel in ziadaniPouzivatelia)
        {
            pouzivatel.JePrimatel = true;
            pouzivatel.TokenPrimatela = GenerateSecureToken();
        }

        // Uloží zmeny do databázy
        await _context.SaveChangesAsync();

        // 4. Vráti výsledok – úspešnú hlášku
        return "Úspešne nastavení primatelia.";
    }
    public async Task<Pouzivatel> VytvorPouzivatela(string meno, string priezvisko, string email, string heslo, string RC)
    {
        string rodneCislo = RC;

        // ✅ Kontrola duplicity
        if (await _context.Pouzivatelia.AnyAsync(u => u.RodneCislo == rodneCislo))
        {
            return new Pouzivatel();
        }

        // ✅ Vytvorenie používateľa
        var novyPouzivatel = new Pouzivatel
        {
            Id = Guid.NewGuid(),
            Meno = meno,
            Priezvisko = priezvisko,
            Email = email,
            RodneCislo = rodneCislo,
            Heslo = BCrypt.Net.BCrypt.HashPassword(heslo),
            Typ = "Pouzivatel",
            JePrimatel = false,
            TokenPrimatela = null
        };

        _context.Pouzivatelia.Add(novyPouzivatel);

        // ✅ Vytvorenie poistenia
        var novePoistenie = new Poistenie
        {
            Id = Guid.NewGuid(),
            ICP = "ICP-" + Guid.NewGuid().ToString().Substring(0, 8),
            Datum = DateTime.UtcNow,
            PoistnyVztahPoistenca = PoistnyVztahPoistencaEnum.JePoistencom,
            JeNeplatic = false,
            ZaciatokEuPoistenia = null,
            MaNarokNaOdkladnuZS = true
        };
        _context.Poistenia.Add(novePoistenie);

        // ✅ Vytvorenie pacienta a priradenie k používateľovi
        var novyPacient = new Pacient
        {
            Id = Guid.NewGuid(),
            PoistenieId = novePoistenie.Id,
            Poistenie = novePoistenie,
            PouzivatelId = novyPouzivatel.Id,
            Pouzivatel = novyPouzivatel
        };
        _context.Pacienti.Add(novyPacient);

        await _context.SaveChangesAsync();

        return novyPouzivatel;
    }

    public async Task<string> DajTokenPrimatela(string rc, string heslo)
    {
        // 1. Nájdeme používateľa podľa rodného čísla
        var pouzivatel = await _context.Pouzivatelia
            .FirstOrDefaultAsync(u => u.RodneCislo == rc);

        if (pouzivatel == null)
            return null; // Používateľ neexistuje

        // 2. Overíme heslo
        if (!BCrypt.Net.BCrypt.Verify(heslo, pouzivatel.Heslo))
            return null; // Nesprávne heslo

        // 3. Overíme, či je primateľ
        if (!pouzivatel.JePrimatel)
            return null; // Nie je oprávnený na token

        // 4. Vrátime existujúci token alebo vytvoríme nový
        if (string.IsNullOrEmpty(pouzivatel.TokenPrimatela))
        {
            pouzivatel.TokenPrimatela = GenerateSecureToken();
            await _context.SaveChangesAsync();
        }

        return pouzivatel.TokenPrimatela;
    }

    // Pomocná metóda na generovanie bezpečného 32-bajtového tokenu
    private string GenerateSecureToken()
    {
        // Vygeneruje 32 náhodných bajtov pomocou kryptograficky bezpečného generátora
        byte[] tokenData = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenData);
        }
        // Prevedie bajty na reťazec (napr. v base64 formáte) a vráti
        return Convert.ToBase64String(tokenData);
    }
    // primatel
    public async Task<string> GetPrimatelToken(Guid idUser, string password)
    {
        // 🔍 1️⃣ Overenie, či používateľ existuje
        var user = await _context.Pouzivatelia
            .FirstOrDefaultAsync(u => u.Id == idUser);

        if (user == null)
        {
            return "❌ Chyba: Používateľ neexistuje.";
        }

        // 🔍 2️⃣ Overenie hesla (simulovaná hashovaná kontrola)
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Heslo);
        if (!isPasswordValid)
        {
            return "❌ Chyba: Nesprávne heslo.";
        }

        // 🔍 3️⃣ Overenie, či je používateľ "Primatel"
        if (user.JePrimatel != false)
        {
            return "❌ Chyba: Používateľ nemá oprávnenie.";
        }

        // 🔥 4️⃣ Vygenerovanie JWT tokenu
        var token = user.TokenPrimatela;
        if (token != null)
        {
            return token;
        }
        else
        {
            return "nemaš priradeny Token";
        }
    }
    // Potraviny
    public async Task<List<Potravina>> GetPotravinyAsync()
    {
        return await _context.Potraviny.ToListAsync();
    }

 

    public async Task<List<KategoriaDTO>> GetKategoriePotravinAsync()
    {
        var kategorie = await _context.KategoriaPotraviny
       .AsNoTracking() // Zabraňuje cyklickým referenciám
       .Include(k => k.Podkategorie) // Načítanie podkategórií
       .ToListAsync();

        // Transformácia do DTO (bez cyklických referencií)
        var dtoList = kategorie.Select(k => new KategoriaDTO
        {
            Kod = k.Kod,
            Nazov = k.Nazov,
            Podkategorie = k.Podkategorie.Select(pk => new KategoriaDTO
            {
                Kod = pk.Kod,
                Nazov = pk.Nazov
            }).ToList()
        }).ToList();

        return dtoList;
    }
    private async Task<List<KategoriaPotravina>> GetPodstromKategoriiPotravinAsync(string kodKategorie)
    {
        var vsetkyKategorie = await _context.KategoriaPotraviny.ToListAsync();
        var podstrom = new List<KategoriaPotravina>();

        void NajdiPodkategorie(string parentKod)
        {
            var podkategorie = vsetkyKategorie.Where(k => k.NadriadenaKategorieKod == parentKod).ToList();
            foreach (var kat in podkategorie)
            {
                podstrom.Add(kat);
                NajdiPodkategorie(kat.Kod); // Rekurzívne vyhľadávanie podkategórií
            }
        }

        NajdiPodkategorie(kodKategorie);
        return podstrom;
    }
    public async Task<SkontrolujPredpisPomockyResponse> SkontrolujPredpisPomocky(
     string rodneCislo, string kodPomocky, decimal mnozstvo, DateTime? datumPredpisu,
     string kodOdbLekara, string? diagnoza)
    {
        var response = new SkontrolujPredpisPomockyResponse();
        var obmedzenia = new List<APIObmedzeniePredpisu>();

        var doplnok = await _context.Doplnky.FirstOrDefaultAsync(d => d.Kod == kodPomocky);
        if (doplnok == null)
        {
            obmedzenia.Add(new APIObmedzeniePredpisu
            {
                Kod = "ERR001",
                Nazov = "Neexistujúca pomôcka",
                Zavaznost = "Z"
            });
            response.PovolenyPredpis = false;
            response.Obmedzenia = obmedzenia;
            return response;
        }

        var kategoria = await _context.KategoriaDoplnkov.FirstOrDefaultAsync(k => k.Kod == doplnok.KodKategorie);
        if (_context == null)
        {
            throw new InvalidOperationException("Database context is not initialized.");
        }


        var dbContext = _context.GetDbContext() as ApplicationDbContext
            ?? throw new InvalidOperationException("Database context conversion failed.");

        var limit = await LimitChecker.NajdiLimitPreKategorii(dbContext, kategoria);

        if (limit == null)
        {
            response.PovolenyPredpis = true;
            response.Obmedzenia = obmedzenia;
            return response;
        }

        if (limit.LimitValue.HasValue && mnozstvo > limit.LimitValue.Value)
        {
            obmedzenia.Add(new APIObmedzeniePredpisu
            {
                Kod = "LIM001",
                Nazov = "Maximálne povolené množstvo prekročené",
                Zavaznost = "Z",
                Hodnota = limit.LimitValue.ToString()
            });
            response.PovolenyPredpis = false;
        }

        var obdobie = datumPredpisu ?? DateTime.UtcNow;

        // Najskôr zistíme, ktoré preskripčné záznamy obsahujú tento doplnok
        var predpisyDoplnky = await _context.PrekricnyZaznamDoplnok
            .Where(pd => pd.DoplnokId == doplnok.Id)
            .Include(pd => pd.PrekricnyZaznam)
            .ToListAsync();

        // Potom získame relevantné preskripčné záznamy pre pacienta
        var predpisy = predpisyDoplnky
            .Where(pd => pd.PrekricnyZaznam.Pacient!= null && pd.PrekricnyZaznam.Pacient.Pouzivatel.RodneCislo == rodneCislo &&
                         pd.PrekricnyZaznam.DatumPredpisu >= obdobie.AddMonths(-limit.MonthsLimit.GetValueOrDefault(0)))
            .Select(pd => pd.PrekricnyZaznam)
            .Distinct()
            .ToList();

        var celkoveMnozstvo = predpisyDoplnky
            .Where(pd => predpisy.Contains(pd.PrekricnyZaznam))
            .Sum(pd => pd.Mnozstvo);

        if (limit.MonthsLimit.HasValue && celkoveMnozstvo + mnozstvo > limit.LimitValue)
        {
            obmedzenia.Add(new APIObmedzeniePredpisu
            {
                Kod = "LIM002",
                Nazov = "Mesačný limit prekročený",
                Zavaznost = "Z",
                Hodnota = limit.MonthsLimit.ToString()
            });
            response.PovolenyPredpis = false;
        }

        response.Obmedzenia = obmedzenia;
        response.PovolenyPredpis = !obmedzenia.Any(o => o.Zavaznost == "Z");
        return response;
    }

    private static async Task<LimitPredpisu?> NajdiLimitPreKategorii(ApplicationDbContext context, KategoriaDoplnok? kategoria)
    {
        while (kategoria != null)
        {
            var limit = await context.LimityPredpisov.FirstOrDefaultAsync(l => l.Id == kategoria.LimitPredpisuId);
            if (limit != null) return limit;

            if (kategoria.Kod == "Default")
            {
                break;
            }

            kategoria = await context.KategoriaDoplnkov.FirstOrDefaultAsync(k => k.Kod == kategoria.NadriadenaKategorieKod);
        }
        return null;
    }
    /// <summary>
    ///  Pacienti s rodnim cislom a ICP
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<List<PacientDto>> GetPacientiAsync()
    {
        return await _context.Pacienti
            .Include(p => p.Pouzivatel)  // ✅ Načítanie RodneCislo
            .Include(p => p.Poistenie)   // ✅ Načítanie ICP
            .Select(p => new PacientDto
            {
                RodneCislo = p.Pouzivatel.RodneCislo,
                ICP = p.Poistenie != null ? p.Poistenie.ICP : null, // ✅ Bezpečná kontrola
                Pacient = p
            })
            .ToListAsync();
    }

}
