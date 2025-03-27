using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Account.Models;

namespace Service
{
    public interface IAccountService
    {
        Task<AccountModel> GetByIdAsync(Guid id);
        Task<IEnumerable<AccountModel>> GetAllAsync();
        Task CreateAsync(AccountModel account);
        Task UpdateAsync(Guid id, AccountModel updatedAccount);
        Task DeleteAsync(Guid id);
    }
}