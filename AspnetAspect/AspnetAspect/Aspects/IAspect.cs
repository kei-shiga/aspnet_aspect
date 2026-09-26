namespace AspnetAspect.Aspects;

/// <summary>
/// メソッド呼び出しに介入するアスペクトの契約です。
/// </summary>
public interface IAspect {
  /// <summary>
  /// このアスペクトが処理する Attribute の型です。
  /// </summary>
  Type AttributeType { get; }

  /// <summary>
  /// 実行順序。値が小さいほど外側（先に開始し、後に終了する側）です。
  /// </summary>
  int Order { get; }

  /// <summary>
  /// アスペクト処理を実行し、必要に応じて内側の処理へ進みます。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>完了を表す Task。</returns>
  Task InvokeAsync(AspectContext context, AspectNext next);
}
