DROP TABLE IF EXISTS products;

CREATE TABLE products (
                          ProductCode VARCHAR(255) PRIMARY KEY,
                          Name VARCHAR(255) NOT NULL,
                          Price NUMERIC,
                          Description TEXT,
                          Variants TEXT[],
                          Discounts NUMERIC,
                          Images TEXT[],
                          Specifications JSONB
);

INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
VALUES (
           'PROD-4567',
           'Premium Organic Coffee Beans - Ethiopian Yirgacheffe',
           19.99,
           'Single-origin, light-bodied coffee with bright, floral notes and a clean finish. Ethically sourced and freshly roasted.',
           '{"Whole Bean", "Ground - Medium", "Ground - Fine"}',
           0.10,
           '{"https://example.com/images/coffee_beans_1.jpg", "https://example.com/images/coffee_beans_2.jpg"}',
           '{
             "Origin": "Ethiopia",
             "Region": "Yirgacheffe",
             "Process": "Washed",
             "Altitude": "1800-2200 meters",
             "Roast Level": "Light",
             "Acidity": "Bright",
             "Body": "Light",
             "Flavor Notes": ["Jasmine", "Lemon", "Bergamot"]
           }'::jsonb
       );

INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
VALUES (
           'PROD-8901',
           'Comfortable Cotton T-Shirt - Heather Grey',
           25.00,
           'Classic crew neck t-shirt made from 100% organic cotton. Soft, breathable, and perfect for everyday wear.',
           '{"Small", "Medium", "Large", "X-Large"}',
           0.05,
           '{"https://example.com/images/tshirt_grey_front.jpg", "https://example.com/images/tshirt_grey_back.jpg", "https://example.com/images/tshirt_colors.jpg"}',
           '{
             "Material": "100% Organic Cotton",
             "Color": "Heather Grey",
             "Fit": "Regular",
             "Neckline": "Crew Neck",
             "Care Instructions": "Machine wash cold, tumble dry low"
           }'::jsonb
       );

INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
VALUES (
           'PROD-2345',
           'Wireless Bluetooth Headphones - Noise Cancelling',
           149.99,
           'Over-ear headphones with active noise cancellation for immersive sound. High-quality audio and comfortable earcups for extended listening.',
           '{"Black", "Silver", "Blue"}',
           0.15,
           '{"https://example.com/images/headphones_black.jpg", "https://example.com/images/headphones_side.jpg", "https://example.com/images/headphones_case.jpg"}',
           '{
             "Connectivity": "Bluetooth 5.0",
             "Noise Cancellation": "Active Noise Cancellation (ANC)",
             "Battery Life": "Up to 30 hours",
             "Charging Time": "2 hours",
             "Driver Size": "40mm",
             "Impedance": "32 Ohms",
             "Features": ["Built-in Microphone", "Touch Controls"]
           }'::jsonb
       );

INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
VALUES (
           'PROD-6789',
           'Artisan Sourdough Bread - Classic Loaf',
           6.50,
           'Hand-crafted sourdough bread with a crispy crust and a chewy, tangy interior. Made with organic flour and a long fermentation process.',
           '{}',
           0.00,
           '{"https://example.com/images/sourdough_loaf.jpg", "https://example.com/images/sourdough_slice.jpg"}',
           '{
             "Ingredients": ["Organic Wheat Flour", "Water", "Salt", "Sourdough Starter"],
             "Weight": "800g",
             "Preparation": "Baked Fresh Daily",
             "Allergens": ["Wheat", "Gluten"]
           }'::jsonb
       );

INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
VALUES (
           'PROD-0123',
           'Stainless Steel Water Bottle - 750ml',
           22.95,
           'Reusable water bottle made from high-quality stainless steel. Keeps drinks cold for 24 hours and hot for 12 hours.',
           '{"Silver", "Black", "Blue", "Green"}',
           0.08,
           '{"https://example.com/images/waterbottle_silver.jpg", "https://example.com/images/waterbottle_colors.jpg", "https://example.com/images/waterbottle_detail.jpg"}',
           '{
             "Material": "18/8 Stainless Steel",
             "Capacity": "750ml",
             "Insulation": "Double-Wall Vacuum Insulated",
             "Lid Type": "Screw-on Lid",
             "Features": ["BPA-Free", "Leak-Proof"]
           }'::jsonb
       );