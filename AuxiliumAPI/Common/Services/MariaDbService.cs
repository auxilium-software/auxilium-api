using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using Dapper;
using MySqlConnector;
using System.Data;

namespace AuxiliumAPI.Common.Services
{
    public class MariaDbService : IMariaDbService
    {
        private readonly string _connectionString;

        public MariaDbService()
        {
            string hostname = ConfigurationUtilities.GetString("Databases", "MariaDB", "Host");
            int port = ConfigurationUtilities.GetInteger("Databases", "MariaDB", "Port");
            string username = ConfigurationUtilities.GetString("Databases", "MariaDB", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "MariaDB", "Password");
            string database = ConfigurationUtilities.GetString("Databases", "MariaDB", "Database");

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
