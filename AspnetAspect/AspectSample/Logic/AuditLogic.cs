using AspectSample.Aspects;
using AspectSample.Repositories;

namespace AspectSample.Logic;

/// <summary>
/// 監査ログを Repository 経由で記録する Logic 実装です。
/// </summary>
public sealed class AuditLogic : IAuditLogic {
  /// <summary>
  /// 監査ログテーブルへのアクセスです。
  /// </summary>
  private readonly IAuditRepository _auditRepository;

  /// <summary>
  /// AuditLogic を生成します。
  /// </summary>
  /// <param name="auditRepository">監査 Repository。</param>
  public AuditLogic(IAuditRepository auditRepository) {
    _auditRepository = auditRepository;
  }

  /// <summary>
  /// 監査メッセージを記録します。
  /// </summary>
  /// <param name="message">記録するメッセージ。</param>
  [Transactional(Propagation = TransactionPropagation.RequiresNew)]
  public Task RecordAsync(string message) {
    return _auditRepository.InsertAsync(message);
  }
}
