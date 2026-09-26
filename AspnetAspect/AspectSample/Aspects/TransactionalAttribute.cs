using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// トランザクション境界のアスペクト対象を示す Attribute です。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class TransactionalAttribute : AspectAttribute {
  /// <summary>
  /// トランザクション伝播の指定です。既定値は Required です。
  /// </summary>
  public TransactionPropagation Propagation { get; init; } = TransactionPropagation.Required;
}
