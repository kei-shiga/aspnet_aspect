using System.Diagnostics;
using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// IAccountService のメソッド実行時間をログ出力するアスペクトです。
/// </summary>
public sealed class PerformanceAspect : IAspect {
  /// <summary>
  /// ログ出力に使用するロガーです。
  /// </summary>
  private readonly ILogger<PerformanceAspect> _logger;

  /// <summary>
  /// PerformanceAspect を生成します。
  /// </summary>
  /// <param name="logger">ロガー。</param>
  public PerformanceAspect(ILogger<PerformanceAspect> logger) {
    _logger = logger;
  }

  /// <summary>
  /// このアスペクトが処理する Attribute の型です。
  /// </summary>
  public Type AttributeType => typeof(PerformanceAttribute);

  /// <summary>
  /// 実行順序。ログとトランザクションの内側で、サービス本体の時間を測ります。
  /// </summary>
  public int Order => 2;

  /// <summary>
  /// Attribute で選ばれたときに内側の処理へ進みます。時間計測は Around が行います。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>完了を表す Task。</returns>
  public async Task InvokeAsync(AspectContext context, AspectNext next) {
    await next().ConfigureAwait(false);
  }

  /// <summary>
  /// IAccountService の全メソッドを選ぶ共有の適用先です。
  /// </summary>
  [ApplyTo("type(AspectSample.Services.IAccountService)")]
  public void ServiceCalls() {
  }

  /// <summary>
  /// 対象メソッドの開始から終了までの時間をミリ秒でログ出力します。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側の処理。</param>
  /// <returns>完了を表す Task。</returns>
  [Around(nameof(ServiceCalls))]
  public async Task MeasureAsync(AspectContext context, AspectNext next) {
    var start = Stopwatch.GetTimestamp();
    try {
      await next().ConfigureAwait(false);
    } finally {
      var elapsed = Stopwatch.GetElapsedTime(start);
      _logger.LogInformation(
        "実行時間: {MethodName} {ElapsedMilliseconds} ms",
        context.InterfaceMethod.Name,
        elapsed.TotalMilliseconds);
    }
  }
}
