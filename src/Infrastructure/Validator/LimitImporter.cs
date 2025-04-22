using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class LimitChecker
{
    public static async Task<bool> MozePredpisatDoplnokAsync(ApplicationDbContext context, string rodneCislo, Guid doplnokId, int pozadovaneMnozstvo)
    {
        // 1️⃣ 🔍 Nájdeme pacienta podľa rodného čísla
        var pacient = await context.Pacienti.FirstOrDefaultAsync(p => p.Pouzivatel.RodneCislo == rodneCislo);
        if (pacient == null) return false;

        // 2️⃣ 🔎 Nájdeme doplnok a jeho kategóriu
        var doplnok = await context.Doplnky
            .Include(d => d.Kategoria)
            .FirstOrDefaultAsync(d => d.Id == doplnokId);

        if (doplnok == null || doplnok.Kategoria == null) return false;

        // 3️⃣ 📜 Získame zoznam všetkých predpisov pacienta
        var predchadzajuceZaznamy = await context.PrekricnyZaznamDoplnok
            .Where(pzp => pzp.PrekricnyZaznam.PacientId == pacient.Id && pzp.DoplnokId == doplnokId)
            .Include(pzp => pzp.PrekricnyZaznam)
            .ToListAsync();

        // 4️⃣ 🌲 Vyhľadáme limity v hierarchii kategórií
        var limit = await NajdiLimitPreKategorii(context, doplnok.Kategoria);

        // Ak neexistuje limit, môžeme predpísať bez obmedzení
        if (limit == null) return true;

        // 5️⃣ 📊 Vypočítame, koľko už bolo predpísané v rôznych časových intervaloch
        var teraz = DateTime.UtcNow;

        int celkoveMnozstvo = predchadzajuceZaznamy.Sum(pzp => pzp.Mnozstvo);
        int mnozstvoZaTyzden = predchadzajuceZaznamy.Where(pzp => pzp.PrekricnyZaznam.DatumPredpisu >= teraz.AddDays(-7)).Sum(pzp => pzp.Mnozstvo);
        int mnozstvoZaMesiac = predchadzajuceZaznamy.Where(pzp => pzp.PrekricnyZaznam.DatumPredpisu >= teraz.AddMonths(-1)).Sum(pzp => pzp.Mnozstvo);
        int mnozstvoZaRok = predchadzajuceZaznamy.Where(pzp => pzp.PrekricnyZaznam.DatumPredpisu >= teraz.AddYears(-1)).Sum(pzp => pzp.Mnozstvo);

        // 6️⃣ 🛑 Kontrola limitov

        // Celkový limit za celú existenciu systému
        if (limit.LimitValue.HasValue && celkoveMnozstvo + pozadovaneMnozstvo > limit.LimitValue.Value)
            return false;

        // Limit za týždeň
        if (limit.WeeksLimit.HasValue && mnozstvoZaTyzden + pozadovaneMnozstvo > limit.WeeksLimit.Value)
            return false;

        // Limit za mesiac
        if (limit.MonthsLimit.HasValue && mnozstvoZaMesiac + pozadovaneMnozstvo > limit.MonthsLimit.Value)
            return false;

        // Limit za rok
        if (limit.YearsLimit.HasValue && mnozstvoZaRok + pozadovaneMnozstvo > limit.YearsLimit.Value)
            return false;

        return true;
    }

    public static async Task<LimitPredpisu?> NajdiLimitPreKategorii(ApplicationDbContext context, KategoriaDoplnok? kategoria)
    {
        while (kategoria != null )
        {
            if (kategoria.Kod == "Default")
            {
                return null;
            }
            var limit = await context.LimityPredpisov.FirstOrDefaultAsync(l => l.Id == kategoria.LimitPredpisuId);
            if (limit != null) return limit;

            // Prejdeme na nadradenú kategóriu
            while (kategoria != null && !string.IsNullOrEmpty(kategoria.NadriadenaKategorieKod))
            {

                if (kategoria.LimitPredpisuId != null)
                {
                    limit = await context.LimityPredpisov.FirstOrDefaultAsync(l => l.Id == kategoria.LimitPredpisuId);
                    if (limit != null) return limit;
                }
                kategoria = await context.KategoriaDoplnkov
                    .FirstOrDefaultAsync(k => k.Kod == kategoria.NadriadenaKategorieKod);
                
            }

        }

        return null; // Žiadny limit sa nenašiel
    }
    public static async Task<bool> MozemPredpisatDoplnokPodlaLimitValue(ApplicationDbContext context, Doplnok doplnok, int mnozstvo)
    {
        // 🔹 Nájdeme kategóriu doplnku
        var kategoria = await context.KategoriaDoplnkov
            .FirstOrDefaultAsync(k => k.Kod == doplnok.KodKategorie);

        if (kategoria == null) return true; // ✅ Ak nemá kategóriu, predpokladáme, že nemá limity

        // 🔹 Nájdeme limit v kategórii alebo nadradenej kategórii
        var limit = await NajdiLimitPreKategorii(context, kategoria);

        if (limit == null) return true; // ✅ Ak žiadny limit neexistuje, môžeme predpísať

        // 🔍 Kontrola iba `LimitValue`
        if (limit.LimitValue.HasValue && limit.LimitValue > 0)
        {
            return mnozstvo <= limit.LimitValue; // ✅ Ak je v limite, vraciame `true`, inak `false`
        }

        return true; // ✅ Ak `LimitValue` neexistuje, predpis je povolený
    }


}
