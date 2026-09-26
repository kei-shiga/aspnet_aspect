namespace AspectSample.Logic;

/// <summary>
/// 監査記録の Logic 層インタフェースです。
/// </summary>
public interface IAuditLogic {
  /// <summary>
  /// 監査メッセージを記録します。
  /// </summary>
  /// <param name="message">記録するメッセージ。</param>
  Task RecordAsync(string message);
}
