using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces.Persistence;
public interface IApplicationDbContext
{
    DbSet<Pacient> Pacienti { get; set; }
    DbSet<Lekar> Lekari { get; set; }
    DbSet<PrekricnyZaznam> PreskripcneZaznamy { get; set; }
    DbSet<Liek> Lieky { get; set; }
    DbSet<Poistenie> Poistenia {  get; set; }
    DbSet<KategoriaLiekov> KategoriaLiekov { get; set; } 
    DbSet<Pouzivatel> Pouzivatelia { get; set; }
    DbSet<PrekricnyZaznamLiek> PrekricnyZaznamLiekov { get; set; }
    DbSet<Diagnoza> Diagnozy { get; set; }
    DbSet<Ambulancia> Ambulancie { get; set; }
    DbSet<Doplnok> Doplnky { get; set; }
    DbSet<OdbornostLekara> OdbornostiLekarov { get; set; }
    DbSet<KategoriaDoplnok> KategoriaDoplnkov { get; set; }
    DbSet<PrekricnyZaznamPomocky> PrekricnyZaznamDoplnok { get; set; }
    DbSet<Potravina> Potraviny { get; set; }
    DbSet<KategoriaPotravina> KategoriaPotraviny { get; set; }
    DbSet<PrekricnyZaznamPotraviny> PrekricnyZaznamPotraviny { get; set; }
    DbSet<LimitPredpisu> LimityPredpisov { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbContext? GetDbContext(); // Pridaj túto metódu

}
