namespace AspnetAspect.Aspects;

/// <summary>
/// 正常終了でも例外でも、最後に実行するアドバイスを示す Attribute です。
/// </summary>
public sealed class AfterAttribute : AdviceAttribute {
  /// <summary>
  /// メソッド自身の ApplyTo を使う After アドバイスを生成します。
  /// </summary>
  public AfterAttribute() : base(null) {
  }

  /// <summary>
  /// 指定したメソッドの ApplyTo を使う After アドバイスを生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。</param>
  public AfterAttribute(string applyToMethod) : base(applyToMethod) {
  }
}
