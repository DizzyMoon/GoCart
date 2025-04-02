using Npgsql;
using System;
using System.Data;

namespace Order.Infrastructure
{
  public class DatabaseConnection
  {
    public NpgsqlConnection GetConnection()
    {
      string host = System.Environment.GetEnvironmentVariable("POSTGRES_HOST");
      string port = System.Environment.GetEnvironmentVariable("POSTGRES_PORT");
      string database = System.Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
      string user = System.Environment.GetEnvironmentVariable("POSTGRES_USER");
      string password = System.Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

      Console.WriteLine($"--- Debug Connection Info ---");
      Console.WriteLine($"POSTGRES_HOST: {host}");
      Console.WriteLine($"POSTGRES_PORT: {port}");
      Console.WriteLine($"POSTGRES_DATABASE: {database}");
      Console.WriteLine($"POSTGRES_USER: {user}");
      Console.WriteLine($"--- End Debug ---");

      if (string.IsNullOrEmpty(host) ||
          string.IsNullOrEmpty(port) ||
          string.IsNullOrEmpty(database) ||
          string.IsNullOrEmpty(user) ||
          string.IsNullOrEmpty(password))
      {
        throw new InvalidOperationException("Postgres connection details not found in environment variables.");
      }

      string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";

      return new NpgsqlConnection(connectionString);
    }

    public bool TestInsert()
    {
      NpgsqlConnection connection = GetConnection();
      try
      {
        Console.WriteLine("Attempting TestInsert...");
        connection = GetConnection();
        connection.Open();
        Console.WriteLine("--> Database connection opened successfully.");

        string sql = "INSERT INTO orders (orderNumber) VALUES ('Test Insert From API');";
        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
        {
          int rowsAffected = command.ExecuteNonQuery();
          Console.WriteLine($"--> Test insert command executed. Rows affected: {rowsAffected}");
          return true; // Indicate success
        }
      }
      catch (NpgsqlException pgEx)
      {
        Console.WriteLine($"!!! PostgreSQL Error during TestInsert: {pgEx.Message}");
        Console.WriteLine($"   SQL State: {pgEx.SqlState}");
        return false; // Indicate failure due to PostgreSQL error
      }
      catch (Exception ex)
      {
        Console.WriteLine($"!!! General Error during TestInsert: {ex.Message}");
        return false; // Indicate failure due to a general error
      }
      finally
      {
        if (connection?.State == ConnectionState.Open)
        {
          connection.Close();
          Console.WriteLine("--> Database connection closed.");
        }
      }
    }
  }
}