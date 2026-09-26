using AspectSample.Models;

namespace AspectSample.Repositories;

/// <summary>
/// 監査ログテーブルへのアクセスを提供する Repository です。
/// </summary>
public interface IAuditRepository {
  /// <summary>
  /// 監査ログをすべて取得します。
  /// </summary>
  /// <returns>監査ログ一覧。</returns>
  Task<IReadOnlyList<AuditEntry>> GetAllAsync();

  /// <summary>
  /// 監査ログを 1 件挿入します。
  /// </summary>
  /// <param name="message">挿入するメッセージ。</param>
  Task InsertAsync(string message);

  /// <summary>
  /// 監査ログをすべて削除します。
  /// </summary>
  Task ClearAsync();
}
