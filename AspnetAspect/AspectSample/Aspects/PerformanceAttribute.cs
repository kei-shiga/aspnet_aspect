using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// 処理時間計測の対象を示す Attribute です。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class PerformanceAttribute : AspectAttribute;
