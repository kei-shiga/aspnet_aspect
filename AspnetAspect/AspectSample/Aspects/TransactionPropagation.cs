namespace AspectSample.Aspects;

/// <summary>
/// トランザクション伝播の種別です。
/// </summary>
public enum TransactionPropagation {
  /// <summary>
  /// 既存トランザクションがあれば参加し、なければ新規に開始します。
  /// </summary>
  Required,

  /// <summary>
  /// 常に新しいトランザクションを開始します。
  /// </summary>
  RequiresNew
}
