using AspectSample.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AspectSample.Controllers;

public class HomeController : Controller {
  /// <summary>
  /// トップページを表示します。
  /// </summary>
  /// <returns>Index View。</returns>
  public IActionResult Index() {
    return View();
  }

  /// <summary>
  /// プライバシーページを表示します。
  /// </summary>
  /// <returns>Privacy View。</returns>
  public IActionResult Privacy() {
    return View();
  }

  /// <summary>
  /// エラー情報を表示します。
  /// </summary>
  /// <returns>エラー View。</returns>
  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error() {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
