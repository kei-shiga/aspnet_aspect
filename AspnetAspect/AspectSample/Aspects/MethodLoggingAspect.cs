using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// LogAttribute が付いたメソッドの開始と終了をログ出力するアスペクトです。
/// </summary>
public sealed class MethodLoggingAspect : IAspect {
  /// <summary>
  /// ログ出力に使用するロガーです。
  /// </summary>
  private readonly ILogger<MethodLoggingAspect> _logger;

  /// <summary>
  /// MethodLoggingAspect を生成します。
  /// </summary>
  /// <param name="logger">ロガー。</param>
  public MethodLoggingAspect(ILogger<MethodLoggingAspect> logger) {
    _logger = logger;
  }

  /// <summary>
  /// このアスペクトが処理する Attribute の型です。
  /// </summary>
  public Type AttributeType => typeof(LogAttribute);

  /// <summary>
  /// 実行順序。値が小さいほど外側（先に開始し、後に終了する側）です。
  /// </summary>
  public int Order => 0;

  /// <summary>
  /// アスペクト処理を実行し、必要に応じて内側の処理へ進みます。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>完了を表す Task。</returns>
  public async Task InvokeAsync(AspectContext context, AspectNext next) {
    var methodName = context.InterfaceMethod.Name;
    _logger.LogInformation("メソッド開始: {MethodName}", methodName);

    try {
      await next().ConfigureAwait(false);
      _logger.LogInformation("メソッド終了: {MethodName}", methodName);
    } catch (Exception ex) {
      _logger.LogInformation(ex, "メソッド終了（例外）: {MethodName}", methodName);
      throw;
    }
  }
}
