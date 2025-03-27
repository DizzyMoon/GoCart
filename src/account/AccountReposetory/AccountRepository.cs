using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Account.Models;

namespace Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly List<AccountModel> _accounts = new();

        public Task<AccountModel> GetByIdAsync(Guid id)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            return Task.FromResult(account);
        }

        public Task<IEnumerable<AccountModel>> GetAllAsync()
        {
            return Task.FromResult(_accounts.AsEnumerable());
        }

        public Task AddAsync(AccountModel account)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Guid id, AccountModel updatedAccount)
        {
            var index = _accounts.FindIndex(a => a.Id == id);
            if (index != -1)
            {
                _accounts[index] = updatedAccount;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account != null)
            {
                _accounts.Remove(account);
            }
            return Task.CompletedTask;
        }
    }
}