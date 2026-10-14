using System.Reflection;
using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// 実行する 1 つのアドバイスです。
/// </summary>
internal sealed class AdviceCall {
  /// <summary>
  /// アドバイス呼び出しを生成します。
  /// </summary>
  /// <param name="aspect">アスペクトインスタンス。</param>
  /// <param name="method">アドバイスメソッド。InvokeAsync のときは null。</param>
  /// <param name="order">同じタイミング内の順序。</param>
  /// <param name="declarationOrder">宣言順。</param>
  public AdviceCall(IAspect aspect, MethodInfo? method, int order, int declarationOrder) {
    Aspect = aspect;
    Method = method;
    Order = order;
    DeclarationOrder = declarationOrder;
  }

  /// <summary>
  /// アスペクトインスタンスです。
  /// </summary>
  public IAspect Aspect { get; }

  /// <summary>
  /// アドバイスメソッドです。InvokeAsync のときは null です。
  /// </summary>
  public MethodInfo? Method { get; }

  /// <summary>
  /// 同じタイミング内の順序です。
  /// </summary>
  public int Order { get; }

  /// <summary>
  /// 宣言順です。
  /// </summary>
  public int DeclarationOrder { get; }
}
