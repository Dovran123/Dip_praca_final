using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence;
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pacient> Pacienti { get; set; }
    public DbSet<Lekar> Lekari { get; set; }
    public DbSet<PrekricnyZaznam> PreskripcneZaznamy { get; set; }
    public DbSet<Liek> Lieky { get; set; }
    public DbSet<Poistenie> Poistenia {  get; set; }
    public DbSet<KategoriaLiekov> KategoriaLiekov { get; set; }
    public DbSet<Pouzivatel> Pouzivatelia { get; set; }
    public DbSet<PrekricnyZaznamLiek> PrekricnyZaznamLiekov { get; set; }
    public DbSet<Diagnoza> Diagnozy { get; set; }
    public DbSet<Ambulancia> Ambulancie { get; set; }
    public DbSet<Doplnok> Doplnky { get; set; }
    public DbSet<KategoriaDoplnok> KategoriaDoplnkov { get; set; }
    public DbSet<PrekricnyZaznamPomocky> PrekricnyZaznamDoplnok { get; set; }
    public DbSet<OdbornostLekara> OdbornostiLekarov { get; set; }
    public DbSet<Potravina> Potraviny { get; set; }
    public DbSet<KategoriaPotravina> KategoriaPotraviny { get; set; }
    public DbSet<PrekricnyZaznamPotraviny> PrekricnyZaznamPotraviny { get; set; }
    public DbSet<LimitPredpisu> LimityPredpisov { get; set; }
    public DbContext GetDbContext() => this;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrekricnyZaznam>()
            .HasOne(pz => pz.Pacient)
            .WithMany(p => p.Preskripcie)
            .HasForeignKey(pz => pz.PacientId)
            .OnDelete(DeleteBehavior.NoAction); // ❌ Zmena z SET NULL na NO ACTION

        modelBuilder.Entity<PrekricnyZaznam>()
            .HasOne(pz => pz.Lekar)
            .WithMany(l => l.Predpisy)
            .HasForeignKey(pz => pz.LekarId)
            .OnDelete(DeleteBehavior.NoAction); // ❌ Zmena z SET NULL na NO ACTION

        modelBuilder.Entity<PrekricnyZaznam>()
            .HasOne(pz => pz.Ambulancia)
            .WithMany(a => a.PrekricneZaznamy)
            .HasForeignKey(pz => pz.AmbulanciaKod)
            .OnDelete(DeleteBehavior.NoAction); // ❌ ZABRÁNENIE cyklu

        modelBuilder.Entity<PrekricnyZaznam>()
            .HasOne(pz => pz.Diagnoza)
            .WithMany()
            .HasForeignKey(pz => pz.DiagnozaKod)
            .OnDelete(DeleteBehavior.NoAction); // ❌ ZABRÁNENIE cyklu
        modelBuilder.Entity<Pouzivatel>()
            .HasIndex(p => p.RodneCislo)
            .IsUnique(); // Zabezpečí unikátnosť rodného čísla
    }




    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
