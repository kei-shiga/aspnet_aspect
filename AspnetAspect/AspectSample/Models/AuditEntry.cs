namespace AspectSample.Models;

/// <summary>
/// 監査ログ 1 件分です。
/// </summary>
public sealed class AuditEntry {
  /// <summary>
  /// ログ ID です。
  /// </summary>
  public required long Id { get; init; }

  /// <summary>
  /// メッセージです。
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// 記録日時（UTC）です。
  /// </summary>
  public required DateTime CreatedAt { get; init; }
}
