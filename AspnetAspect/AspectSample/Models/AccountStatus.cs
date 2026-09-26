namespace AspectSample.Models;

/// <summary>
/// 口座残高と監査ログの表示用モデルです。
/// </summary>
public sealed class AccountStatus {
  /// <summary>
  /// 口座一覧です。
  /// </summary>
  public required IReadOnlyList<AccountBalance> Accounts { get; init; }

  /// <summary>
  /// 監査ログ一覧です。
  /// </summary>
  public required IReadOnlyList<AuditEntry> AuditLogs { get; init; }
}
