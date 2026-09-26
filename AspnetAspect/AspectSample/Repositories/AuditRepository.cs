using AspectSample.Models;
using MySqlConnector;

namespace AspectSample.Repositories;

/// <summary>
/// ADO.NET で監査ログテーブルにアクセスする Repository 実装です。
/// </summary>
public sealed class AuditRepository : IAuditRepository {
  /// <summary>
  /// 接続文字列の取得に使用します。
  /// </summary>
  private readonly IConfiguration _configuration;

  /// <summary>
  /// AuditRepository を生成します。
  /// </summary>
  /// <param name="configuration">アプリケーション設定。</param>
  public AuditRepository(IConfiguration configuration) {
    _configuration = configuration;
  }

  /// <summary>
  /// 監査ログをすべて取得します。
  /// </summary>
  /// <returns>監査ログ一覧。</returns>
  public async Task<IReadOnlyList<AuditEntry>> GetAllAsync() {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT id, message, created_at FROM audit_log ORDER BY id";

    var list = new List<AuditEntry>();
    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    while (await reader.ReadAsync().ConfigureAwait(false)) {
      list.Add(new AuditEntry {
        Id = reader.GetInt64("id"),
        Message = reader.GetString("message"),
        CreatedAt = reader.GetDateTime("created_at")
      });
    }

    return list;
  }

  /// <summary>
  /// 監査ログを 1 件挿入します。
  /// </summary>
  /// <param name="message">挿入するメッセージ。</param>
  public async Task InsertAsync(string message) {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO audit_log (message, created_at) VALUES (@message, @createdAt)";
    command.Parameters.AddWithValue("@message", message);
    command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// 監査ログをすべて削除します。
  /// </summary>
  public async Task ClearAsync() {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = "DELETE FROM audit_log";
    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// 設定から MySQL 接続を生成します。
  /// </summary>
  /// <returns>未オープンの MySQL 接続。</returns>
  private MySqlConnection CreateConnection() {
    var connectionString = _configuration.GetConnectionString("AspectSample")
        ?? throw new InvalidOperationException("ConnectionStrings:AspectSample が設定されていません。");
    return new MySqlConnection(connectionString);
  }
}
