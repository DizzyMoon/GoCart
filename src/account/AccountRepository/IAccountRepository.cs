using Account.AccountModels;

namespace Account.AccountRepository
{
    public interface IAccountRepository
    {
        Task<IEnumerable<AccountModel>> GetQueryCollection();
        Task<AccountModel?> Get(int accountId);
        Task<AccountModel> Create(AccountModelRequest account);
        Task<AccountModel> Update(int id, AccountUpdateRequest account);
        Task<AccountModel?> Delete(int accountId);
    }
}