DROP TABLE IF EXISTS accounts;

CREATE TABLE accounts (
                          Id SERIAL PRIMARY KEY,
                          Email VARCHAR(255) NOT NULL UNIQUE,
                          Name VARCHAR(255) NOT NULL,
                          PasswordHash VARCHAR(255) NOT NULL,
                          PhoneNumber VARCHAR(20)
);

-- Sample data insertions
INSERT INTO accounts (Email, Name, PasswordHash, PhoneNumber)
VALUES
    ('alice@example.com', 'Alice Johnson', 'hashed_password_123', '+1234567890'),
    ('bob@example.com', 'Bob Smith', 'hashed_password_456', '+1987654321'),
    ('carol@example.com', 'Carol Davis', 'hashed_password_789', '+2342652367');