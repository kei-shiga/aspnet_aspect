using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// 1 つのアスペクトクラスに対する、今回の呼び出しのアドバイスです。
/// </summary>
internal sealed class AspectFrame {
  /// <summary>
  /// アスペクト枠を生成します。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="around">Around と InvokeAsync。</param>
  /// <param name="before">Before。</param>
  /// <param name="afterReturn">AfterReturn。</param>
  /// <param name="afterThrow">AfterThrow。</param>
  /// <param name="after">After。</param>
  public AspectFrame(
    AspectContext context,
    List<AdviceCall> around,
    List<AdviceCall> before,
    List<AdviceCall> afterReturn,
    List<AdviceCall> afterThrow,
    List<AdviceCall> after) {
    Context = context;
    Around = around;
    Before = before;
    AfterReturn = afterReturn;
    AfterThrow = afterThrow;
    After = after;
  }

  /// <summary>
  /// 呼び出しコンテキストです。
  /// </summary>
  public AspectContext Context { get; }

  /// <summary>
  /// Around と InvokeAsync です。
  /// </summary>
  public List<AdviceCall> Around { get; }

  /// <summary>
  /// Before です。
  /// </summary>
  public List<AdviceCall> Before { get; }

  /// <summary>
  /// AfterReturn です。
  /// </summary>
  public List<AdviceCall> AfterReturn { get; }

  /// <summary>
  /// AfterThrow です。
  /// </summary>
  public List<AdviceCall> AfterThrow { get; }

  /// <summary>
  /// After です。
  /// </summary>
  public List<AdviceCall> After { get; }
}
