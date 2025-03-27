using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Account.Models;

namespace Repository
{
    public interface IAccountRepository
    {
        Task<AccountModel> GetByIdAsync(Guid id);
        Task<IEnumerable<AccountModel>> GetAllAsync();
        Task AddAsync(AccountModel account);
        Task UpdateAsync(Guid id, AccountModel updatedAccount);
        Task DeleteAsync(Guid id);
    }
}