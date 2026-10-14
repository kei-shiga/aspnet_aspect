using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// LogAttribute が付いたメソッドと、ApplyTo で選んだ Repository メソッドの開始と終了をログ出力するアスペクトです。
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

  /// <summary>
  /// Repository インタフェースの全メソッドを選ぶ共有の適用先です。
  /// </summary>
  [ApplyTo("type(AspectSample.Repositories.*)")]
  public void RepositoryCalls() {
  }

  /// <summary>
  /// Repository メソッドの開始をログ出力します。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  [Before(nameof(RepositoryCalls))]
  public void LogRepositoryStart(AspectContext context) {
    _logger.LogInformation("メソッド開始: {MethodName}", context.InterfaceMethod.Name);
  }

  /// <summary>
  /// Repository メソッドの終了をログ出力します。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  [After(nameof(RepositoryCalls))]
  public void LogRepositoryEnd(AspectContext context) {
    _logger.LogInformation("メソッド終了: {MethodName}", context.InterfaceMethod.Name);
  }

  /// <summary>
  /// Repository メソッドが例外で終わったことをログ出力します。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="exception">送出された例外。</param>
  [AfterThrow(nameof(RepositoryCalls))]
  public void LogRepositoryError(AspectContext context, Exception exception) {
    _logger.LogInformation(exception, "メソッド終了（例外）: {MethodName}", context.InterfaceMethod.Name);
  }
}
