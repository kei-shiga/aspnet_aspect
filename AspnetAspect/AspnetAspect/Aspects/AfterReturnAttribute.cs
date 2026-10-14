namespace AspnetAspect.Aspects;

/// <summary>
/// 対象メソッドが正常終了したあとに実行するアドバイスを示す Attribute です。
/// </summary>
public sealed class AfterReturnAttribute : AdviceAttribute {
  /// <summary>
  /// メソッド自身の ApplyTo を使う AfterReturn アドバイスを生成します。
  /// </summary>
  public AfterReturnAttribute() : base(null) {
  }

  /// <summary>
  /// 指定したメソッドの ApplyTo を使う AfterReturn アドバイスを生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。</param>
  public AfterReturnAttribute(string applyToMethod) : base(applyToMethod) {
  }
}
