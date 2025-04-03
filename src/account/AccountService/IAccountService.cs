using Account.AccountModels;

namespace Account.AccountService
{
    public interface IAccountService
    {
        Task<IEnumerable<AccountModel>> GetQueryCollection();
        Task<AccountModel> Get(int accountId);
        Task<AccountModel> Create(CreateAccountModel account);
        Task<AccountModel> Delete(int accountId);
    }
}