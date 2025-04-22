using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class LiekRepository : ILiekRepository
    {
        private readonly IApplicationDbContext _context;

        public LiekRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Liek>> GetAllAsync()
        {
            return await ((DbContext)_context).Set<Liek>().ToListAsync();
            // Alebo, ak máte priamo definovaný DbSet v IApplicationDbContext, 
            // použite: return await _context.Lieky.ToListAsync();
        }

        public async Task<Liek?> GetByIdAsync(Guid id)
        {
            return await ((DbContext)_context).Set<Liek>().FindAsync(id);
            // Alebo, ak máte priamo DbSet: return await _context.Lieky.FindAsync(id);
        }

        public async Task AddAsync(Liek liek)
        {
            ((DbContext)_context).Set<Liek>().Add(liek);
            await _context.SaveChangesAsync(default);
        }

        public async Task UpdateAsync(Liek liek)
        {
            ((DbContext)_context).Set<Liek>().Update(liek);
            await _context.SaveChangesAsync(default);
        }

        public async Task DeleteAsync(Guid id)
        {
            var liek = await GetByIdAsync(id);
            if (liek != null)
            {
                ((DbContext)_context).Set<Liek>().Remove(liek);
                await _context.SaveChangesAsync(default);
            }
        }
    }
}
