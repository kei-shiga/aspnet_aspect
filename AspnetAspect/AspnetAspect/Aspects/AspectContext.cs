using System.Reflection;

namespace AspnetAspect.Aspects;

/// <summary>
/// 1 回のアスペクト実行時に渡される呼び出しコンテキストです。
/// </summary>
public sealed class AspectContext {
  /// <summary>
  /// 呼び出しコンテキストを生成します。
  /// </summary>
  /// <param name="interfaceMethod">呼び出されたインタフェース側のメソッド。</param>
  /// <param name="implementationMethod">実際に実行される実装側のメソッド。</param>
  /// <param name="target">プロキシの実体インスタンス。</param>
  /// <param name="arguments">メソッド引数。</param>
  /// <param name="aspectAttribute">このアスペクト実行に使用する Attribute。</param>
  public AspectContext(
    MethodInfo interfaceMethod,
    MethodInfo implementationMethod,
    object target,
    object?[]? arguments,
    AspectAttribute aspectAttribute) {
    InterfaceMethod = interfaceMethod;
    ImplementationMethod = implementationMethod;
    Target = target;
    Arguments = arguments;
    AspectAttribute = aspectAttribute;
  }

  /// <summary>
  /// 呼び出されたインタフェース側のメソッドです。
  /// </summary>
  public MethodInfo InterfaceMethod { get; }

  /// <summary>
  /// 実際に実行される実装側のメソッドです。
  /// </summary>
  public MethodInfo ImplementationMethod { get; }

  /// <summary>
  /// プロキシの実体インスタンスです。
  /// </summary>
  public object Target { get; }

  /// <summary>
  /// メソッド引数です。
  /// </summary>
  public object?[]? Arguments { get; }

  /// <summary>
  /// このアスペクト実行に使用する Attribute です。
  /// </summary>
  public AspectAttribute AspectAttribute { get; }
}
