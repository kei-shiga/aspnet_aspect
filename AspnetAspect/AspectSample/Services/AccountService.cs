using AspectSample.Aspects;
using AspectSample.Logic;
using AspectSample.Models;

namespace AspectSample.Services;

/// <summary>
/// 振替処理を Logic 層へ委譲する Service 実装です。
/// </summary>
[Log]
[Transactional]
public sealed class AccountService : IAccountService {
  /// <summary>
  /// 口座更新用 Logic です。
  /// </summary>
  private readonly IAccountLogic _accountLogic;

  /// <summary>
  /// 監査記録用 Logic です。
  /// </summary>
  private readonly IAuditLogic _auditLogic;

  /// <summary>
  /// AccountService を生成します。
  /// </summary>
  /// <param name="accountLogic">口座 Logic。</param>
  /// <param name="auditLogic">監査 Logic。</param>
  public AccountService(IAccountLogic accountLogic, IAuditLogic auditLogic) {
    _accountLogic = accountLogic;
    _auditLogic = auditLogic;
  }

  /// <summary>
  /// 口座残高と監査ログを取得します。
  /// </summary>
  /// <returns>口座一覧と監査ログ。</returns>
  public Task<AccountStatus> GetStatusAsync() {
    return _accountLogic.GetStatusAsync();
  }

  /// <summary>
  /// 口座 A から口座 B へ振替し、監査を記録します。
  /// </summary>
  /// <param name="amount">振替金額。</param>
  /// <param name="failureMode">デモ用の失敗パターン。</param>
  public async Task TransferAsync(decimal amount, TransferFailureMode failureMode) {
    await _accountLogic.TransferAsync(amount, failureMode).ConfigureAwait(false);
    await _auditLogic.RecordAsync($"口座Aから口座Bへ {amount} 振替").ConfigureAwait(false);

    if (failureMode == TransferFailureMode.FailAfterAudit) {
      throw new InvalidOperationException("振替後に意図的な失敗（外側トランザクションのロールバック確認用）");
    }
  }

  /// <summary>
  /// 口座残高と監査ログを初期状態に戻します。
  /// </summary>
  public Task ResetAsync() {
    return _accountLogic.ResetAsync();
  }
}
