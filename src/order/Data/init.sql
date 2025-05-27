DROP TABLE IF EXISTS orders;

CREATE TABLE orders (
                        ID SERIAL PRIMARY KEY,
                        OrderNumber TEXT NOT NULL,
                        OrderDate TIMESTAMP WITH TIME ZONE NOT NULL,
                        PaymentIntentId TEXT NOT NULL,
                        Status TEXT NOT NULL
);

INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('AE309AE3-A38D-43B1-914F-3E464D5C3718', '2025-04-09T15:39:06.783475+00', 'pi_3RRDLNbK6zY8g00ndCosdK', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('A791562A-F15F-46E3-9E5A-CF378F79965C', '2025-04-10T13:25:17.942351+00', 'pi_3RRCyuLNbk6zY8g01X08koL3', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('575413E1-7E17-40E2-B3FC-5BD9FCEDB715', '2025-04-10T13:25:19.412712+00', 'pi_3RRAV2LNbk6zY8g01f7WKx2gX', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('2B973E33-AADA-4AF9-9072-9CF69EFD997C', '2025-04-10T13:25:20.315657+00', 'pi_3RRASbLNbk6zY8g00Luedh3x', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('DEF7D292-1CFD-4FB1-8C6F-4C0C96F841A1', '2025-04-10T13:25:21.103632+00', 'pi_3RRAN8LNbk6zY8g00R25dPFvW', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('65E0F3AD-6A3E-4208-AF24-E7CEAEF3414C', '2025-04-10T13:25:21.907473+00', 'pi_3RR4KmLNbK6zY8g00TJxalM9X', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('163D6731-F7CE-463E-A46D-517A886204DF', '2025-04-10T13:25:22.591793+00', 'pi_3RR8ZruLNbk6zY8g005Jp4tL', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('A6EB7DDB-F3E7-40B8-A8DA-16A2CEE2B6AF', '2025-04-10T13:25:23.301992+00', 'pi_3RR8NFLNbK6zY8g00JH6tBeAE', 'Confirmed');
INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES
    ('CEAB0971-F3A4-44F5-A4E5-9814B2351E35', '2025-04-10T13:25:23.984242+00', 'pi_3RR8J1LNbk6zY8g00KIubsRaO', 'Confirmed');