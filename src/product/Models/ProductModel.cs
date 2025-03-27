public class Product {
    public string ProductCode {get; set;}
    public string Name {get; set;}
    public double Price {get; set;}
    public string Description {get;set;}
    public string[] Variants {get; set;}
    public double Discounts {get; set;}
    public string[] Images {get; set;}
    public Dictionary<string, object> Specifications {get; set;}

    public Product(){
        ProductCode = "";
        Name = "";
        Price = 0;
        Description = "";
        Variants = [];
        Discounts = 0;
        Images = [];
        Specifications = new Dictionary<string, object>();
    }

    public Product(
        string productCode,
        string name,
        double price,
        string description,
        string[] variants,
        double discounts,
        string[] images,
        Dictionary<string, object> specifications){
            ProductCode = productCode;
            Name = name;
            Price = price;
            Description = description;
            Variants = variants;
            Discounts = discounts;
            Images = images;
            Specifications = specifications;
        }
}