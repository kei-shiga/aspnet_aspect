namespace AspectSample.Models;

/// <summary>
/// 振替デモで意図的に失敗させるパターンです。
/// </summary>
public enum TransferFailureMode {
  /// <summary>
  /// 失敗させません。
  /// </summary>
  None,

  /// <summary>
  /// 口座更新後、監査記録前に例外を送出します。
  /// </summary>
  FailAfterAccountUpdate,

  /// <summary>
  /// 監査記録後、外側トランザクションをロールバックする例外を送出します。
  /// </summary>
  FailAfterAudit
}
