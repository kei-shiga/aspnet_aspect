using AspectSample.Models;

namespace AspectSample.Repositories;

/// <summary>
/// 口座テーブルへのアクセスを提供する Repository です。
/// </summary>
public interface IAccountRepository {
  /// <summary>
  /// 全口座を取得します。
  /// </summary>
  /// <returns>口座残高一覧。</returns>
  Task<IReadOnlyList<AccountBalance>> GetAllAsync();

  /// <summary>
  /// 指定口座間で残高を移動します。
  /// </summary>
  /// <param name="fromId">送金元口座 ID。</param>
  /// <param name="toId">送金先口座 ID。</param>
  /// <param name="amount">移動する金額。</param>
  Task TransferAsync(string fromId, string toId, decimal amount);

  /// <summary>
  /// 口座残高を初期値に戻します。
  /// </summary>
  Task ResetAsync();
}
