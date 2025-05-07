namespace Account.AccountModels
{
    public class AccountModel
    {

        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class AccountModelRequest
    {
        public string Email { get; set; }
        public string Name {get; set;}
        public string Password { get; set;}
        public string PhoneNumber { get; set; }
    }
    
    public class AccountUpdateRequest
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? PasswordHash { get; set; }
        public string? PhoneNumber { get; set; }
    }

}