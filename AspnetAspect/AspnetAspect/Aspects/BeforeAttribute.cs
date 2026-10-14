namespace AspnetAspect.Aspects;

/// <summary>
/// 対象メソッドの前に実行するアドバイスを示す Attribute です。
/// </summary>
public sealed class BeforeAttribute : AdviceAttribute {
  /// <summary>
  /// メソッド自身の ApplyTo を使う Before アドバイスを生成します。
  /// </summary>
  public BeforeAttribute() : base(null) {
  }

  /// <summary>
  /// 指定したメソッドの ApplyTo を使う Before アドバイスを生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。</param>
  public BeforeAttribute(string applyToMethod) : base(applyToMethod) {
  }
}
