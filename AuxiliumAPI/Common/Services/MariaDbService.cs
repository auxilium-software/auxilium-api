using AuxiliumAPI.Common.Services.Interfaces;
using Dapper;
using MySqlConnector;

namespace AuxiliumAPI.Common.Services
{
    public class MariaDbService : IMariaDbService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public MariaDbService(
            IConfiguration configuration
            )
        {
            this._configuration = configuration;

            string hostname = this._configuration!["Databases:MariaDB:Host"]!;
            int port = this._configuration!.GetValue<int>("Databases:MariaDB:Port");
            string username = this._configuration!["Databases:MariaDB:Username"]!;
            string password = this._configuration!["Databases:MariaDB:Password"]!;
            string database = this._configuration!["Databases:MariaDB:Database"]!;

            _connectionString = $"Server={hostname};Port={port};Database={database};User ID={username};Password={password};";
        }

        public async Task<Interfaces.IDbTransaction> BeginTransactionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync();
            return new MariaDbTransaction(transaction, connection);
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, Interfaces.IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                var mariaDbTransaction = (MariaDbTransaction)transaction;
                return await mariaDbTransaction.DbConnection.ExecuteScalarAsync<T>(
                    sql,
                    parameters,
                    mariaDbTransaction.DbTransaction
                );
            }

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null, Interfaces.IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                var mariaDbTransaction = (MariaDbTransaction)transaction;
                return await mariaDbTransaction.DbConnection.ExecuteAsync(
                    sql,
                    parameters,
                    mariaDbTransaction.DbTransaction
                );
            }

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, Interfaces.IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                var mariaDbTransaction = (MariaDbTransaction)transaction;
                return await mariaDbTransaction.DbConnection.QuerySingleOrDefaultAsync<T>(
                    sql,
                    parameters,
                    mariaDbTransaction.DbTransaction
                );
            }

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, Interfaces.IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                var mariaDbTransaction = (MariaDbTransaction)transaction;
                return await mariaDbTransaction.DbConnection.QueryAsync<T>(
                    sql,
                    parameters,
                    mariaDbTransaction.DbTransaction
                );
            }

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<T>(sql, parameters);
        }
    }
}
