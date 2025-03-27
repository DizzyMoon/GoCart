using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Account.Models;
using Repository;

namespace Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<AccountModel> GetByIdAsync(Guid id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                throw new Exception("Account not found.");
            }
            return account;
        }

        public Task<IEnumerable<AccountModel>> GetAllAsync()
        {
            return _accountRepository.GetAllAsync();
        }

        public async Task CreateAsync(AccountModel account)
        {
            await _accountRepository.AddAsync(account);
        }

        public async Task UpdateAsync(Guid id, AccountModel updatedAccount)
        {
            var existing = await _accountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new Exception("Account not found.");
            }

            await _accountRepository.UpdateAsync(id, updatedAccount);
        }

        public async Task DeleteAsync(Guid id)
        {
            var existing = await _accountRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new Exception("Account not found.");
            }

            await _accountRepository.DeleteAsync(id);
        }
    }
}