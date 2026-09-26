using AspectSample.Models;

namespace AspectSample.Services;

/// <summary>
/// 口座振替デモの Service 層インタフェースです。
/// </summary>
public interface IAccountService {
  /// <summary>
  /// 口座残高と監査ログを取得します。
  /// </summary>
  /// <returns>口座一覧と監査ログ。</returns>
  Task<AccountStatus> GetStatusAsync();

  /// <summary>
  /// 口座 A から口座 B へ振替し、監査を記録します。
  /// </summary>
  /// <param name="amount">振替金額。</param>
  /// <param name="failureMode">デモ用の失敗パターン。</param>
  Task TransferAsync(decimal amount, TransferFailureMode failureMode);

  /// <summary>
  /// 口座残高と監査ログを初期状態に戻します。
  /// </summary>
  Task ResetAsync();
}
