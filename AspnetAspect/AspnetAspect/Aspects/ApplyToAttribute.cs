namespace AspnetAspect.Aspects;

/// <summary>
/// アスペクトを適用するインタフェースメソッドを文字列で指定する Attribute です。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ApplyToAttribute : Attribute {
  /// <summary>
  /// 適用先の式を保持する Attribute を生成します。
  /// </summary>
  /// <param name="expression">method または type で始まる適用先の式。</param>
  public ApplyToAttribute(string expression) {
    Expression = expression;
  }

  /// <summary>
  /// method または type で始まる適用先の式です。
  /// </summary>
  public string Expression { get; }
}
