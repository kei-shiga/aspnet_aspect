namespace AspnetAspect.Aspects;

/// <summary>
/// アスペクト適用対象を示す Attribute の基底クラスです。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public abstract class AspectAttribute : Attribute;
