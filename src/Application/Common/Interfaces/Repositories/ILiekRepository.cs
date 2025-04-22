using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.Repositories;

public interface ILiekRepository
{
    Task<IEnumerable<Liek>> GetAllAsync();
    Task<Liek?> GetByIdAsync(Guid id);
    Task AddAsync(Liek liek);
    Task UpdateAsync(Liek liek);
    Task DeleteAsync(Guid id);
}
