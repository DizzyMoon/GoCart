namespace product.Messaging.Events
{
    public class AddProductFailedEvent()
    {
        public string Name { get; set; }
        public string Reason { get; set; }
    }
}