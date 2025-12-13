using MySqlConnector;
using System.Data;

namespace AuxiliumAPI.Common.Services
{
    internal class MariaDbTransaction : Interfaces.IDbTransaction
    {
        public MySqlTransaction DbTransaction { get; }
        public MySqlConnection DbConnection { get; }

        public MariaDbTransaction(MySqlTransaction transaction, MySqlConnection connection)
        {
            DbTransaction = transaction;
            DbConnection = connection;
        }

        public async Task CommitAsync()
        {
            await DbTransaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await DbTransaction.RollbackAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (DbTransaction != null)
                await DbTransaction.DisposeAsync();
            if (DbConnection != null)
                await DbConnection.DisposeAsync();
        }
    }
}
