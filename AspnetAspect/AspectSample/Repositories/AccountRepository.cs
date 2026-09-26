using AspectSample.Models;
using MySqlConnector;

namespace AspectSample.Repositories;

/// <summary>
/// ADO.NET で口座テーブルにアクセスする Repository 実装です。
/// </summary>
public sealed class AccountRepository : IAccountRepository {
  /// <summary>
  /// 接続文字列の取得に使用します。
  /// </summary>
  private readonly IConfiguration _configuration;

  /// <summary>
  /// AccountRepository を生成します。
  /// </summary>
  /// <param name="configuration">アプリケーション設定。</param>
  public AccountRepository(IConfiguration configuration) {
    _configuration = configuration;
  }

  /// <summary>
  /// 全口座を取得します。
  /// </summary>
  /// <returns>口座残高一覧。</returns>
  public async Task<IReadOnlyList<AccountBalance>> GetAllAsync() {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT id, name, balance FROM account ORDER BY id";

    var list = new List<AccountBalance>();
    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    while (await reader.ReadAsync().ConfigureAwait(false)) {
      list.Add(new AccountBalance {
        Id = reader.GetString("id"),
        Name = reader.GetString("name"),
        Balance = reader.GetDecimal("balance")
      });
    }

    return list;
  }

  /// <summary>
  /// 指定口座間で残高を移動します。
  /// </summary>
  /// <param name="fromId">送金元口座 ID。</param>
  /// <param name="toId">送金先口座 ID。</param>
  /// <param name="amount">移動する金額。</param>
  public async Task TransferAsync(string fromId, string toId, decimal amount) {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var debit = connection.CreateCommand();
    debit.CommandText = "UPDATE account SET balance = balance - @amount WHERE id = @id";
    debit.Parameters.AddWithValue("@amount", amount);
    debit.Parameters.AddWithValue("@id", fromId);
    await debit.ExecuteNonQueryAsync().ConfigureAwait(false);

    await using var credit = connection.CreateCommand();
    credit.CommandText = "UPDATE account SET balance = balance + @amount WHERE id = @id";
    credit.Parameters.AddWithValue("@amount", amount);
    credit.Parameters.AddWithValue("@id", toId);
    await credit.ExecuteNonQueryAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// 口座残高を初期値に戻します。
  /// </summary>
  public async Task ResetAsync() {
    await using var connection = CreateConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = """
        UPDATE account SET balance = 1000.00 WHERE id IN ('A', 'B')
        """;
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
