using Npgsql;
using Account.AccountModels;

namespace Account.AccountRepository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public AccountRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;  
        }

        private async Task<NpgsqlConnection> GetConnectionAsync()
        {
            return await _dataSource.OpenConnectionAsync();
        }

        public async Task<IEnumerable<AccountModel>> GetQueryCollection()
        {
            var accounts = new List<AccountModel>();

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM accounts", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                accounts.Add(new AccountModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Name = reader.GetString(reader.GetOrdinal("name")), 
                    PasswordHash = reader.GetString(reader.GetOrdinal("passwordhash").GetHashCode()),
                    PhoneNumber = reader.GetString(reader.GetOrdinal("phonenumber")),
                });
            }

            return accounts;
        }

        public async Task<AccountModel> Get(int accountId)
        {
            AccountModel account = null!;

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM accounts WHERE id = @accountId", connection);
            command.Parameters.AddWithValue("accountId", accountId);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                account = new AccountModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Name = reader.GetString(reader.GetOrdinal("name")), 
                    PasswordHash = reader.GetString(reader.GetOrdinal("passwordhash").GetHashCode()),
                    PhoneNumber = reader.GetString(reader.GetOrdinal("phonenumber")),
                };
            }

            return account;
        }


        public async Task<AccountModel> Create(CreateAccountModel account)
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(
                "INSERT INTO accounts (email, name, passwordhash, phonenumber) VALUES (@email, @name, @passwordhash, @phonenumber) RETURNING id, email, passwordhash, phonenumber ", connection);
            
            command.Parameters.AddWithValue("@email", account.Email);
            command.Parameters.AddWithValue("@name", account.Name);
            command.Parameters.AddWithValue("@passwordhash", account.PasswordHash);
            command.Parameters.AddWithValue("@phonenumber", account.PhoneNumber);
            
            AccountModel newAccount = null!;
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                newAccount = new AccountModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Name = reader.GetString(reader.GetOrdinal("name")), 
                    PasswordHash = reader.GetString(reader.GetOrdinal("passwordhash")),
                    PhoneNumber = reader.GetString(reader.GetOrdinal("phonenumber")),
                };
            }

            return newAccount;
        }

        public async Task<AccountModel?> Delete(int accountId)
        {
            var accountModelToDelete = await Get(accountId);

            if (accountModelToDelete == null)
            {
                return null;
            }

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("DELETE FROM accounts WHERE id = @accountId", connection);
            command.Parameters.AddWithValue("accountId", accountId);

            await command.ExecuteNonQueryAsync();
            return accountModelToDelete;
        }
    }
}