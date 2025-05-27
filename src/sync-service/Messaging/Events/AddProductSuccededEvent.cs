namespace sync_service.Messaging.Events
{
    public class AddProductSucceededEvent()
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public string[]? Variants { get; set; }
        public double? Discounts { get; set; }
        public string[] Images { get; set; }
        public Dictionary<string, object> Specifications { get; set; }
    }
}