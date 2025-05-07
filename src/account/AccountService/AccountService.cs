using Account.AccountModels;
using Account.AccountRepository;
using Account.AccountService;
using System.Security.Cryptography;
using System.Text;

namespace account.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public Task<IEnumerable<AccountModel>> GetQueryCollection()
        {
            return _accountRepository.GetQueryCollection();
        }
        
        public async Task<AccountModel> Get(int accountId)
        {
            var account = await _accountRepository.Get(accountId);

            if (account == null)
            {
                throw new KeyNotFoundException($"Account with ID {accountId} not found.");
            }

            return account;
        }

        public async Task<AccountModel> Create(AccountModelRequest dto)
        {
            var hashedPassword = HashPassword(dto.Password);

            var accountWithHashedPassword = new AccountModelRequest
            {
                Email = dto.Email,
                Name = dto.Name,
                Password = hashedPassword,
                PhoneNumber = dto.PhoneNumber
            };

            return await _accountRepository.Create(accountWithHashedPassword);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<AccountModel> Update(int id, AccountUpdateRequest dto)
        {
            return await _accountRepository.Update(id, dto);
        }

        public async Task<AccountModel> Delete(int accountId)
        {
            var accountToDelete = await _accountRepository.Delete(accountId);

            if (accountToDelete == null)
            {
                throw new KeyNotFoundException($"Account with ID {accountId} not found.");
            }

            return accountToDelete;
        }
    }
}