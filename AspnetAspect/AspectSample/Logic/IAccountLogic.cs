using AspectSample.Models;

namespace AspectSample.Logic;

/// <summary>
/// 口座関連の Logic 層インタフェースです。
/// </summary>
public interface IAccountLogic {
  /// <summary>
  /// 口座残高と監査ログを取得します。
  /// </summary>
  /// <returns>口座一覧と監査ログ。</returns>
  Task<AccountStatus> GetStatusAsync();

  /// <summary>
  /// 口座 A から口座 B へ残高を移動します。
  /// </summary>
  /// <param name="amount">移動する金額。</param>
  /// <param name="failureMode">デモ用の失敗パターン。</param>
  Task TransferAsync(decimal amount, TransferFailureMode failureMode);

  /// <summary>
  /// 口座と監査ログを初期状態に戻します。
  /// </summary>
  Task ResetAsync();
}
