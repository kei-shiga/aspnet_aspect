namespace AspnetAspect.Aspects;

/// <summary>
/// 対象メソッドまたは Before が例外を送出したあとに実行するアドバイスを示す Attribute です。
/// </summary>
public sealed class AfterThrowAttribute : AdviceAttribute {
  /// <summary>
  /// メソッド自身の ApplyTo を使う AfterThrow アドバイスを生成します。
  /// </summary>
  public AfterThrowAttribute() : base(null) {
  }

  /// <summary>
  /// 指定したメソッドの ApplyTo を使う AfterThrow アドバイスを生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。</param>
  public AfterThrowAttribute(string applyToMethod) : base(applyToMethod) {
  }
}
