namespace AspectSample.Models;

/// <summary>
/// 口座残高 1 件分です。
/// </summary>
public sealed class AccountBalance {
  /// <summary>
  /// 口座 ID です。
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// 口座名称です。
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// 残高です。
  /// </summary>
  public required decimal Balance { get; init; }
}
