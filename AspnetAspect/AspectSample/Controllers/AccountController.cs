using AspectSample.Models;
using AspectSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspectSample.Controllers;

/// <summary>
/// 口座振替デモ画面の Controller です。
/// </summary>
public sealed class AccountController : Controller {
  /// <summary>
  /// 振替処理の Service です。
  /// </summary>
  private readonly IAccountService _accountService;

  /// <summary>
  /// AccountController を生成します。
  /// </summary>
  /// <param name="accountService">振替処理の Service。</param>
  public AccountController(IAccountService accountService) {
    _accountService = accountService;
  }

  /// <summary>
  /// 口座残高と監査ログを表示します。
  /// </summary>
  /// <returns>一覧表示用の View。</returns>
  [HttpGet]
  public async Task<IActionResult> Index() {
    var status = await _accountService.GetStatusAsync().ConfigureAwait(false);
    return View(status);
  }

  /// <summary>
  /// 振替処理を実行します。
  /// </summary>
  /// <param name="amount">振替金額。</param>
  /// <param name="failureMode">デモ用の失敗パターン。</param>
  /// <returns>一覧へリダイレクトする結果。</returns>
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Transfer(decimal amount, TransferFailureMode failureMode) {
    try {
      await _accountService.TransferAsync(amount, failureMode).ConfigureAwait(false);
      TempData["Message"] = failureMode == TransferFailureMode.None
          ? "正常に振替しました。"
          : "操作が完了しました。";
    } catch (Exception ex) {
      TempData["Error"] = ex.Message;
    }

    return RedirectToAction(nameof(Index));
  }

  /// <summary>
  /// 口座と監査ログを初期状態に戻します。
  /// </summary>
  /// <returns>一覧へリダイレクトする結果。</returns>
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Reset() {
    await _accountService.ResetAsync().ConfigureAwait(false);
    TempData["Message"] = "口座残高と監査ログを初期状態に戻しました。";
    return RedirectToAction(nameof(Index));
  }
}
