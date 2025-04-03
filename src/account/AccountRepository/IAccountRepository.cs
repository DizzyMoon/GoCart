using Account.AccountModels;

namespace Account.AccountRepository
{
    public interface IAccountRepository
    {
        Task<IEnumerable<AccountModel>> GetQueryCollection();
        Task<AccountModel?> Get(int accountId);
        Task<AccountModel> Create(CreateAccountModel account);
        Task<AccountModel?> Delete(int accountId);
    }
}