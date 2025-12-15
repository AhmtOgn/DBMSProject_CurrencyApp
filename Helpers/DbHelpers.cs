using Npgsql;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace CurrencyApp.Helpers
{
    public class DbHelper
    {
        // "string?" yaparak null olabilir dedik veya default değer atadık
        private readonly string _connectionString = "";

        public DbHelper(IConfiguration configuration)
        {
            // Eğer connection string null gelirse boş string ata ki patlamasın
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}