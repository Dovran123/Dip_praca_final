using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContextInitialiser
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationDbContextInitialiser> _logger;

        public ApplicationDbContextInitialiser(ApplicationDbContext context, ILogger<ApplicationDbContextInitialiser> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                // Ak používate SQL Server, aplikujte migrácie
                if (_context.Database.IsSqlServer())
                {
                    await _context.Database.MigrateAsync();
                }
                // Pre iné databázy môžete pridať podmienky
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba pri migrácii databázy.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                // Zavolajte seed metódu, ktorú ste definovali (napr. ApplicationDbContextSeed.SeedAsync)
                await ApplicationDbContextSeed.SeedAsync(_context, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba pri seeding-u databázy.");
                throw;
            }
        }
    }
}
