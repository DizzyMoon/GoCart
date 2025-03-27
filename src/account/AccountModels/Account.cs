namespace Account.Models
{
    public class AccountModel
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }

        public ShippingAddress Address { get; set; }
        public Customer? Customer { get; set; }
    }

    public class ShippingAddress { }

    public class Customer { }
}