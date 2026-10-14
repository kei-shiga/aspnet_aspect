namespace AspnetAspect.Aspects;

/// <summary>
/// 内側の処理の前後を制御するアドバイスを示す Attribute です。
/// </summary>
public sealed class AroundAttribute : AdviceAttribute {
  /// <summary>
  /// メソッド自身の ApplyTo を使う Around アドバイスを生成します。
  /// </summary>
  public AroundAttribute() : base(null) {
  }

  /// <summary>
  /// 指定したメソッドの ApplyTo を使う Around アドバイスを生成します。
  /// </summary>
  /// <param name="applyToMethod">適用先の式を持つ引数なしメソッドの名前。</param>
  public AroundAttribute(string applyToMethod) : base(applyToMethod) {
  }
}
