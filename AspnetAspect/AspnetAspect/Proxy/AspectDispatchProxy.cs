using System.Reflection;
using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// インタフェース呼び出しを横取りし、Attribute に一致するアスペクトを連鎖実行する DispatchProxy 実装です。
/// </summary>
internal class AspectDispatchProxy : DispatchProxy {
  /// <summary>
  /// プロキシの実体となる実装インスタンスです。
  /// </summary>
  private object _target = null!;

  /// <summary>
  /// 実装型。Attribute の解決に使用します。
  /// </summary>
  private Type _implementationType = null!;

  /// <summary>
  /// DI から解決されたアスペクト一覧です。
  /// </summary>
  private IReadOnlyList<IAspect> _aspects = null!;

  /// <summary>
  /// プロキシ生成直後に、実体・アスペクト一覧を設定します。
  /// </summary>
  /// <param name="target">実装インスタンス。</param>
  /// <param name="implementationType">実装型。</param>
  /// <param name="aspects">適用候補のアスペクト一覧。</param>
  internal void Initialize(object target, Type implementationType, IReadOnlyList<IAspect> aspects) {
    _target = target;
    _implementationType = implementationType;
    _aspects = aspects;
  }

  /// <summary>
  /// プロキシ経由のメソッド呼び出しを横取りし、一致するアスペクトを適用します。
  /// </summary>
  /// <param name="targetMethod">呼び出されたインタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>メソッドの戻り値。void の場合は null。</returns>
  protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
    if (targetMethod is null) {
      throw new InvalidOperationException("プロキシ対象のメソッドが特定できません。");
    }

    var implementationMethod = ResolveImplementationMethod(targetMethod);
    var applicable = CollectApplicableAspects(implementationMethod);

    if (targetMethod.ReturnType == typeof(void)) {
      var pipeline = BuildVoidPipeline(applicable, implementationMethod, targetMethod, args);
      pipeline().GetAwaiter().GetResult();
      return null;
    }

    if (targetMethod.ReturnType == typeof(Task)) {
      var pipeline = BuildTaskPipeline(applicable, implementationMethod, targetMethod, args);
      return pipeline();
    }

    if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) {
      var resultType = targetMethod.ReturnType.GenericTypeArguments[0];
      return InvokeTaskOfT(resultType, applicable, implementationMethod, targetMethod, args);
    }

    var syncPipeline = BuildSyncPipeline(applicable, implementationMethod, targetMethod, args);
    return syncPipeline().GetAwaiter().GetResult();
  }

  /// <summary>
  /// 戻り値 Task&lt;T&gt; の呼び出しを実行します。
  /// </summary>
  /// <param name="resultType">Task の結果型。</param>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>Task または Task&lt;T&gt; のインスタンス。</returns>
  private object InvokeTaskOfT(
      Type resultType,
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    var method = typeof(AspectDispatchProxy)
        .GetMethod(nameof(ExecuteTaskOfTPipelineAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
        .MakeGenericMethod(resultType);

    return method.Invoke(this, [applicable, implementationMethod, interfaceMethod, args])!;
  }

  /// <summary>
  /// Task&lt;T&gt; 向けのアスペクト付きパイプラインを実行します。
  /// </summary>
  /// <typeparam name="T">結果型。</typeparam>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>アスペクト適用後の Task&lt;T&gt;。</returns>
  private Task<T> ExecuteTaskOfTPipelineAsync<T>(
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    Func<Task<T>> pipeline = () => InvokeImplementationAsTaskOfT<T>(implementationMethod, args);

    for (var i = applicable.Count - 1; i >= 0; i--) {
      var (aspect, attribute) = applicable[i];
      var next = pipeline;
      pipeline = () => WrapAspectForTaskOfT(aspect, CreateContext(interfaceMethod, implementationMethod, args, attribute), next);
    }

    return pipeline();
  }

  /// <summary>
  /// アスペクト実行後に内側の Task&lt;T&gt; を返します。
  /// </summary>
  /// <typeparam name="T">結果型。</typeparam>
  /// <param name="aspect">実行するアスペクト。</param>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側の Task&lt;T&gt; ファクトリ。</param>
  /// <returns>完了後の結果を含む Task&lt;T&gt;。</returns>
  private async Task<T> WrapAspectForTaskOfT<T>(IAspect aspect, AspectContext context, Func<Task<T>> next) {
    Task<T>? downstream = null;
    await aspect.InvokeAsync(context, () => {
      downstream = next();
      return downstream;
    }).ConfigureAwait(false);

    return await downstream!.ConfigureAwait(false);
  }

  /// <summary>
  /// 戻り値 Task のパイプラインを組み立てます。
  /// </summary>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>アスペクト適用後に実行する Task ファクトリ。</returns>
  private Func<Task> BuildTaskPipeline(
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    Func<Task> pipeline = () => InvokeImplementationAsTask(implementationMethod, args);

    for (var i = applicable.Count - 1; i >= 0; i--) {
      var (aspect, attribute) = applicable[i];
      var next = pipeline;
      pipeline = () => WrapAspectForTask(aspect, CreateContext(interfaceMethod, implementationMethod, args, attribute), next);
    }

    return pipeline;
  }

  /// <summary>
  /// アスペクト実行後に内側の Task を完了させます。
  /// </summary>
  /// <param name="aspect">実行するアスペクト。</param>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側の Task ファクトリ。</param>
  /// <returns>完了を表す Task。</returns>
  private async Task WrapAspectForTask(IAspect aspect, AspectContext context, Func<Task> next) {
    Task? downstream = null;
    await aspect.InvokeAsync(context, () => {
      downstream = next();
      return downstream ?? Task.CompletedTask;
    }).ConfigureAwait(false);

    await downstream!.ConfigureAwait(false);
  }

  /// <summary>
  /// 戻り値 void のパイプラインを組み立てます。
  /// </summary>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>アスペクト適用後に実行する Task ファクトリ。</returns>
  private Func<Task> BuildVoidPipeline(
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    return BuildTaskPipeline(applicable, implementationMethod, interfaceMethod, args);
  }

  /// <summary>
  /// 同期戻り値向けの Task パイプラインを組み立てます。
  /// </summary>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>ボックス化された結果を返す Task ファクトリ。</returns>
  private Func<Task<object?>> BuildSyncPipeline(
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    var returnType = implementationMethod.ReturnType;
    var executor = typeof(AspectDispatchProxy)
        .GetMethod(nameof(ExecuteSyncPipelineAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
        .MakeGenericMethod(returnType);

    return () => (Task<object?>)executor.Invoke(this, [applicable, implementationMethod, interfaceMethod, args])!;
  }

  /// <summary>
  /// 同期戻り値メソッドを Task パイプラインで実行します。
  /// </summary>
  /// <typeparam name="T">結果型。</typeparam>
  /// <param name="applicable">適用するアスペクト一覧。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>ボックス化された結果。</returns>
  private async Task<object?> ExecuteSyncPipelineAsync<T>(
      List<(IAspect Aspect, AspectAttribute Attribute)> applicable,
      MethodInfo implementationMethod,
      MethodInfo interfaceMethod,
      object?[]? args) {
    var task = ExecuteTaskOfTPipelineAsync<T>(applicable, implementationMethod, interfaceMethod, args);
    return await task.ConfigureAwait(false);
  }

  /// <summary>
  /// 実装メソッドを呼び出し Task&lt;T&gt; を返します。
  /// </summary>
  /// <typeparam name="T">結果型。</typeparam>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>実装の Task&lt;T&gt; または同期結果を包んだ Task。</returns>
  private Task<T> InvokeImplementationAsTaskOfT<T>(MethodInfo implementationMethod, object?[]? args) {
    var result = implementationMethod.Invoke(_target, args);

    if (result is Task<T> task) {
      return task;
    }

    if (result is T value) {
      return Task.FromResult(value);
    }

    throw new InvalidOperationException(
        $"メソッド {implementationMethod.Name} の戻り値を Task<{typeof(T).Name}> として扱えません。");
  }

  /// <summary>
  /// 実装メソッドを呼び出し Task を返します。
  /// </summary>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>実装の Task、または Task.CompletedTask。</returns>
  private Task InvokeImplementationAsTask(MethodInfo implementationMethod, object?[]? args) {
    var result = implementationMethod.Invoke(_target, args);

    if (result is Task task) {
      return task;
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Attribute が一致するアスペクト一覧を収集します。
  /// </summary>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <returns>Order 順にソートされたアスペクト一覧。</returns>
  private List<(IAspect Aspect, AspectAttribute Attribute)> CollectApplicableAspects(MethodInfo implementationMethod) {
    var applicable = new List<(IAspect Aspect, AspectAttribute Attribute)>();

    foreach (var aspect in _aspects) {
      var attribute = ResolveAttribute(aspect.AttributeType, implementationMethod);
      if (attribute is not null) {
        applicable.Add((aspect, attribute));
      }
    }

    applicable.Sort((a, b) => a.Aspect.Order.CompareTo(b.Aspect.Order));
    return applicable;
  }

  /// <summary>
  /// アスペクト実行用のコンテキストを生成します。
  /// </summary>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <param name="attribute">適用する Attribute。</param>
  /// <returns>生成したコンテキスト。</returns>
  private AspectContext CreateContext(
      MethodInfo interfaceMethod,
      MethodInfo implementationMethod,
      object?[]? args,
      AspectAttribute attribute) {
    return new AspectContext(interfaceMethod, implementationMethod, _target, args, attribute);
  }

  /// <summary>
  /// インタフェースメソッドに対応する実装型のメソッドを取得します。
  /// </summary>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <returns>対応する実装メソッド。</returns>
  private MethodInfo ResolveImplementationMethod(MethodInfo interfaceMethod) {
    var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

    var method = _implementationType.GetMethod(
        interfaceMethod.Name,
        BindingFlags.Public | BindingFlags.Instance,
        binder: null,
        types: parameterTypes,
        modifiers: null);

    if (method is null) {
      throw new InvalidOperationException(
          $"実装型 {_implementationType.Name} にインタフェースメソッド {interfaceMethod.Name} に対応するメソッドがありません。");
    }

    return method;
  }

  /// <summary>
  /// メソッドまたはクラスに付いたアスペクト用 Attribute を取得します。
  /// </summary>
  /// <param name="attributeType">探す Attribute の型。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <returns>見つかった Attribute。無ければ null。</returns>
  private AspectAttribute? ResolveAttribute(Type attributeType, MethodInfo implementationMethod) {
    if (implementationMethod.GetCustomAttributes(attributeType, inherit: true).FirstOrDefault() is AspectAttribute methodAttribute) {
      return methodAttribute;
    }

    if (implementationMethod.DeclaringType?.GetCustomAttributes(attributeType, inherit: true).FirstOrDefault() is AspectAttribute classAttribute) {
      return classAttribute;
    }

    return null;
  }
}
