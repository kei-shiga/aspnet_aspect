namespace AspectSample.Models;

/// <summary>
/// エラー画面の表示用モデルです。
/// </summary>
public class ErrorViewModel {
  /// <summary>
  /// リクエスト追跡 ID です。
  /// </summary>
  public string? RequestId { get; set; }

  /// <summary>
  /// リクエスト ID を画面に表示するかどうかです。
  /// </summary>
  public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
