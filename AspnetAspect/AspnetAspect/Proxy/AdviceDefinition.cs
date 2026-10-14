using System.Reflection;
using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// 1 つのアドバイスメソッドです。
/// </summary>
internal sealed class AdviceDefinition {
  /// <summary>
  /// アドバイスメソッドの定義を生成します。
  /// </summary>
  /// <param name="method">アドバイスメソッド。</param>
  /// <param name="attribute">タイミング Attribute。</param>
  /// <param name="patterns">適用先の式。</param>
  /// <param name="declarationOrder">宣言順。</param>
  public AdviceDefinition(
    MethodInfo method,
    AdviceAttribute attribute,
    IReadOnlyList<ApplyToPattern> patterns,
    int declarationOrder) {
    Method = method;
    Attribute = attribute;
    Patterns = patterns;
    DeclarationOrder = declarationOrder;
  }

  /// <summary>
  /// アドバイスメソッドです。
  /// </summary>
  public MethodInfo Method { get; }

  /// <summary>
  /// タイミング Attribute です。
  /// </summary>
  public AdviceAttribute Attribute { get; }

  /// <summary>
  /// 適用先の式です。どれか 1 つに一致すれば対象です。
  /// </summary>
  public IReadOnlyList<ApplyToPattern> Patterns { get; }

  /// <summary>
  /// 宣言順です。同じ Order のときはこの値が小さいものから実行します。
  /// </summary>
  public int DeclarationOrder { get; }
}
