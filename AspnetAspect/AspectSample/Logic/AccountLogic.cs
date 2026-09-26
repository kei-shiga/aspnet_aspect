using AspectSample.Models;
using AspectSample.Repositories;

namespace AspectSample.Logic;

/// <summary>
/// 口座残高の更新と状態取得を Repository 経由で行う Logic 実装です。
/// </summary>
public sealed class AccountLogic : IAccountLogic {
  /// <summary>
  /// 口座テーブルへのアクセスです。
  /// </summary>
  private readonly IAccountRepository _accountRepository;

  /// <summary>
  /// 監査ログテーブルへのアクセス（参照・初期化用）です。
  /// </summary>
  private readonly IAuditRepository _auditRepository;

  /// <summary>
  /// AccountLogic を生成します。
  /// </summary>
  /// <param name="accountRepository">口座 Repository。</param>
  /// <param name="auditRepository">監査 Repository。</param>
  public AccountLogic(IAccountRepository accountRepository, IAuditRepository auditRepository) {
    _accountRepository = accountRepository;
    _auditRepository = auditRepository;
  }

  /// <summary>
  /// 口座残高と監査ログを取得します。
  /// </summary>
  /// <returns>口座一覧と監査ログ。</returns>
  public async Task<AccountStatus> GetStatusAsync() {
    var accounts = await _accountRepository.GetAllAsync().ConfigureAwait(false);
    var audits = await _auditRepository.GetAllAsync().ConfigureAwait(false);
    return new AccountStatus {
      Accounts = accounts,
      AuditLogs = audits
    };
  }

  /// <summary>
  /// 口座 A から口座 B へ残高を移動します。
  /// </summary>
  /// <param name="amount">移動する金額。</param>
  /// <param name="failureMode">デモ用の失敗パターン。</param>
  public async Task TransferAsync(decimal amount, TransferFailureMode failureMode) {
    await _accountRepository.TransferAsync("A", "B", amount).ConfigureAwait(false);

    if (failureMode == TransferFailureMode.FailAfterAccountUpdate) {
      throw new InvalidOperationException("口座更新後に意図的な失敗（ロールバック確認用）");
    }
  }

  /// <summary>
  /// 口座と監査ログを初期状態に戻します。
  /// </summary>
  public async Task ResetAsync() {
    await _accountRepository.ResetAsync().ConfigureAwait(false);
    await _auditRepository.ClearAsync().ConfigureAwait(false);
  }
}
