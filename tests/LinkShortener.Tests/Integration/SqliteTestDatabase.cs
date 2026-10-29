using Microsoft.Data.Sqlite;

namespace LinkShortener.Tests.Integration;

// Arquivo SQLite temporário, apagado ao fim do teste.
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"links-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_path}";

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }
}
