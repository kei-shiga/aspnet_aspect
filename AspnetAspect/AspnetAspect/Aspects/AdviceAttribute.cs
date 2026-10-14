namespace AspnetAspect.Aspects;

/// <summary>
/// アドバイスメソッドの実行タイミングを示す Attribute の基底クラスです。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public abstract class AdviceAttribute : Attribute {
  /// <summary>
  /// アドバイス Attribute を生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。メソッド自身に ApplyTo を付けるときは null。</param>
  private protected AdviceAttribute(string? applyToMethod) {
    ApplyToMethod = applyToMethod;
  }

  /// <summary>
  /// 適用先の式を持つ引数なしメソッドの名前です。
  /// </summary>
  public string? ApplyToMethod { get; }

  /// <summary>
  /// 同じタイミングの中での実行順序です。値が小さいものから実行します。
  /// </summary>
  public int Order { get; init; }
}
