namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IMariaDbService
    {
        Task<IDbTransaction> BeginTransactionAsync();
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
        Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null);
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
    }
}
