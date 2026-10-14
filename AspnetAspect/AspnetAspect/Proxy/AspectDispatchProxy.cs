using System.Reflection;
using System.Runtime.ExceptionServices;
using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// インタフェース呼び出しを横取りし、Attribute と ApplyTo に一致するアドバイスを入れ子で実行する DispatchProxy 実装です。
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
  /// アスペクト型ごとの ApplyTo とアドバイスの解析結果です。
  /// </summary>
  private Dictionary<Type, AspectShape> _shapes = null!;

  /// <summary>
  /// プロキシ生成直後に、実体・アスペクト一覧を設定します。
  /// </summary>
  /// <param name="target">実装インスタンス。</param>
  /// <param name="implementationType">実装型。</param>
  /// <param name="aspects">適用候補のアスペクト一覧。</param>
  internal void Initialize(object target, Type implementationType, IReadOnlyList<IAspect> aspects) {
    _target = target;
    _implementationType = implementationType;
    // Order が小さいアスペクトほど、後から包むので外側になる。
    _aspects = aspects.OrderBy(aspect => aspect.Order).ToArray();
    _shapes = new Dictionary<Type, AspectShape>();
    foreach (var aspect in _aspects) {
      var aspectType = aspect.GetType();
      // 同じ型は式を一度だけ解析する。
      if (!_shapes.ContainsKey(aspectType)) {
        _shapes.Add(aspectType, new AspectShape(aspectType));
      }
    }
  }

  /// <summary>
  /// プロキシ経由のメソッド呼び出しを横取りし、一致するアドバイスを適用します。
  /// </summary>
  /// <param name="targetMethod">呼び出されたインタフェースメソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>メソッドの戻り値。void の場合は null。</returns>
  protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
    if (targetMethod is null) {
      throw new InvalidOperationException("プロキシ対象のメソッドが特定できません。");
    }

    var implementationMethod = ResolveImplementationMethod(targetMethod);
    var pipeline = BuildPipeline(targetMethod, implementationMethod, args);

    // パイプラインは常に Task<object?>。呼び出し元の戻り値の形に合わせて返す。
    if (targetMethod.ReturnType == typeof(void)) {
      pipeline().GetAwaiter().GetResult();
      return null;
    }

    if (targetMethod.ReturnType == typeof(Task)) {
      return WaitTask(pipeline());
    }

    if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) {
      // Task<T> の T は呼び出しごとに変わるので、ここでジェネリックメソッドを閉じる。
      var resultType = targetMethod.ReturnType.GenericTypeArguments[0];
      return typeof(AspectDispatchProxy)
        .GetMethod(nameof(WaitTaskOfT), BindingFlags.NonPublic | BindingFlags.Instance)!
        .MakeGenericMethod(resultType)
        .Invoke(this, [pipeline()])!;
    }

    return pipeline().GetAwaiter().GetResult();
  }

  /// <summary>
  /// パイプラインの完了を待つ Task を返します。
  /// </summary>
  /// <param name="pipeline">戻り値を持つパイプライン。</param>
  /// <returns>完了を表す Task。</returns>
  private async Task WaitTask(Task<object?> pipeline) {
    await pipeline.ConfigureAwait(false);
  }

  /// <summary>
  /// パイプラインの結果を Task として返します。
  /// </summary>
  /// <typeparam name="T">結果型。</typeparam>
  /// <param name="pipeline">戻り値を持つパイプライン。</param>
  /// <returns>結果を持つ Task。</returns>
  private async Task<T> WaitTaskOfT<T>(Task<object?> pipeline) {
    var result = await pipeline.ConfigureAwait(false);
    if (result is null) {
      return default!;
    }

    return (T)result;
  }

  /// <summary>
  /// 一致したアドバイスを外側から入れ子にした関数を組み立てます。
  /// </summary>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>実装メソッドまで実行する関数。</returns>
  private Func<Task<object?>> BuildPipeline(MethodInfo interfaceMethod, MethodInfo implementationMethod, object?[]? args) {
    var frames = SelectFrames(interfaceMethod, implementationMethod, args);
    // 一番内側は実装メソッド。リストの後ろから包むと、先頭のアスペクトが外側になる。
    Func<Task<object?>> pipeline = () => InvokeImplementation(implementationMethod, args);
    for (var i = frames.Count - 1; i >= 0; i--) {
      var frame = frames[i];
      var inner = pipeline;
      pipeline = () => ExecuteFrame(frame, inner);
    }

    return pipeline;
  }

  /// <summary>
  /// この呼び出しに使うアドバイスをアスペクトごとに集めます。
  /// </summary>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>外側から並んだアスペクト枠。</returns>
  private List<AspectFrame> SelectFrames(MethodInfo interfaceMethod, MethodInfo implementationMethod, object?[]? args) {
    var frames = new List<AspectFrame>();
    foreach (var aspect in _aspects) {
      var shape = _shapes[aspect.GetType()];
      // Attribute は実装メソッドと実装クラス、ApplyTo はインタフェースメソッドを見る。
      var attribute = ResolveAttribute(aspect.AttributeType, implementationMethod);
      var classMatch = shape.ClassPatterns.Any(pattern => pattern.Matches(interfaceMethod));
      var includeInvoke = attribute is not null || classMatch;
      var around = new List<AdviceCall>();
      var before = new List<AdviceCall>();
      var afterReturn = new List<AdviceCall>();
      var afterThrow = new List<AdviceCall>();
      var after = new List<AdviceCall>();

      if (includeInvoke) {
        // メソッドが null の Around が InvokeAsync。宣言順 -1 で、同じ Order の Around より外側になる。
        around.Add(new AdviceCall(aspect, null, 0, -1));
      }

      foreach (var advice in shape.Advices) {
        if (!advice.Patterns.Any(pattern => pattern.Matches(interfaceMethod))) {
          continue;
        }

        var call = new AdviceCall(aspect, advice.Method, advice.Attribute.Order, advice.DeclarationOrder);
        switch (advice.Attribute) {
          case AroundAttribute:
            around.Add(call);
            break;
          case BeforeAttribute:
            before.Add(call);
            break;
          case AfterReturnAttribute:
            afterReturn.Add(call);
            break;
          case AfterThrowAttribute:
            afterThrow.Add(call);
            break;
          case AfterAttribute:
            after.Add(call);
            break;
        }
      }

      if (!includeInvoke && around.Count == 0 && before.Count == 0 && afterReturn.Count == 0 && afterThrow.Count == 0 && after.Count == 0) {
        // この呼び出しに一致するアドバイスがなければ、枠自体を作らない。
        continue;
      }

      around.Sort(CompareAdvice);
      before.Sort(CompareAdvice);
      afterReturn.Sort(CompareAdvice);
      afterThrow.Sort(CompareAdvice);
      after.Sort(CompareAdvice);
      frames.Add(new AspectFrame(
        new AspectContext(interfaceMethod, implementationMethod, _target, args, attribute),
        around,
        before,
        afterReturn,
        afterThrow,
        after));
    }

    return frames;
  }

  /// <summary>
  /// 1 つのアスペクト枠を、Around が外側になるように実行します。
  /// </summary>
  /// <param name="frame">アスペクト枠。</param>
  /// <param name="inner">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>内側の戻り値。</returns>
  private async Task<object?> ExecuteFrame(AspectFrame frame, Func<Task<object?>> inner) {
    Func<Task<object?>> current = () => ExecuteCore(frame, inner);
    // Order が大きい Around から包む。後から包んだものが外側になる。
    for (var i = frame.Around.Count - 1; i >= 0; i--) {
      var around = frame.Around[i];
      var next = current;
      current = () => InvokeAround(around, frame.Context, next);
    }

    return await current().ConfigureAwait(false);
  }

  /// <summary>
  /// Before、内側、AfterReturn または AfterThrow、After の順に実行します。
  /// </summary>
  /// <param name="frame">アスペクト枠。</param>
  /// <param name="inner">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>内側の戻り値。</returns>
  private async Task<object?> ExecuteCore(AspectFrame frame, Func<Task<object?>> inner) {
    Exception? error = null;
    object? result = null;
    var succeeded = false;
    try {
      foreach (var before in frame.Before) {
        await InvokeCall(before, frame.Context, null, null, null).ConfigureAwait(false);
      }

      result = await inner().ConfigureAwait(false);
      succeeded = true;
    } catch (Exception ex) {
      // Before または内側の例外。AfterReturn は実行しない。
      error = ex;
    }

    try {
      if (succeeded) {
        foreach (var afterReturn in frame.AfterReturn) {
          await InvokeCall(afterReturn, frame.Context, result, null, null).ConfigureAwait(false);
        }
      } else if (error is not null) {
        foreach (var afterThrow in frame.AfterThrow) {
          await InvokeCall(afterThrow, frame.Context, null, error, null).ConfigureAwait(false);
        }
      }
    } finally {
      // 正常終了でも例外でも、AfterReturn または AfterThrow のあとに After を実行する。
      foreach (var after in frame.After) {
        await InvokeCall(after, frame.Context, succeeded ? result : null, error, null).ConfigureAwait(false);
      }
    }

    if (error is not null) {
      // After のあとで、Before または内側が出した例外を返す。
      ExceptionDispatchInfo.Capture(error).Throw();
    }

    return result;
  }

  /// <summary>
  /// Around または InvokeAsync を実行し、next が呼ばれたときの戻り値を返します。
  /// </summary>
  /// <param name="around">Around アドバイス。メソッドが null のときは InvokeAsync。</param>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="nextCore">内側の処理。</param>
  /// <returns>next の戻り値。next を呼ばないときは null。</returns>
  private async Task<object?> InvokeAround(AdviceCall around, AspectContext context, Func<Task<object?>> nextCore) {
    object? result = null;
    var called = false;
    AspectNext next = async () => {
      result = await nextCore().ConfigureAwait(false);
      called = true;
    };

    // メソッドが null のときは、アスペクトの InvokeAsync。
    if (around.Method is null) {
      await around.Aspect.InvokeAsync(context, next).ConfigureAwait(false);
    } else {
      await InvokeAdvice(around.Aspect, around.Method, context, null, null, next).ConfigureAwait(false);
    }

    // next を呼ばなかったときは内側が動いていないので、戻り値は null。
    return called ? result : null;
  }

  /// <summary>
  /// Before、After、AfterReturn、AfterThrow を実行します。
  /// </summary>
  /// <param name="call">アドバイス。</param>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="result">対象メソッドの戻り値。</param>
  /// <param name="error">送出された例外。</param>
  /// <param name="next">内側の処理。Around 以外では null。</param>
  private async Task InvokeCall(AdviceCall call, AspectContext context, object? result, Exception? error, AspectNext? next) {
    if (call.Method is null) {
      throw new InvalidOperationException("InvokeAsync は Around として実行します。");
    }

    await InvokeAdvice(call.Aspect, call.Method, context, result, error, next).ConfigureAwait(false);
  }

  /// <summary>
  /// アドバイスメソッドを引数の型に合わせて呼び出します。
  /// </summary>
  /// <param name="aspect">アスペクトインスタンス。</param>
  /// <param name="method">アドバイスメソッド。</param>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="result">対象メソッドの戻り値。</param>
  /// <param name="error">送出された例外。</param>
  /// <param name="next">内側の処理。</param>
  private async Task InvokeAdvice(
    object aspect,
    MethodInfo method,
    AspectContext context,
    object? result,
    Exception? error,
    AspectNext? next) {
    var parameters = method.GetParameters();
    var arguments = new object?[parameters.Length];
    // 引数の並びではなく、型で値を渡す。
    for (var i = 0; i < parameters.Length; i++) {
      var parameterType = parameters[i].ParameterType;
      if (parameterType == typeof(AspectContext)) {
        arguments[i] = context;
      } else if (parameterType == typeof(AspectNext)) {
        arguments[i] = next;
      } else if (typeof(Exception).IsAssignableFrom(parameterType)) {
        arguments[i] = error;
      } else if (parameterType == typeof(object)) {
        arguments[i] = result;
      }
    }

    object? returned;
    try {
      returned = method.Invoke(aspect, arguments);
    } catch (TargetInvocationException ex) {
      // MethodInfo.Invoke は例外を包むので、アドバイスが出した例外を返す。
      ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
      throw;
    }

    if (returned is Task task) {
      await task.ConfigureAwait(false);
    }
  }

  /// <summary>
  /// 実装メソッドを呼び出し、同期結果または Task の結果を返します。
  /// </summary>
  /// <param name="implementationMethod">実装メソッド。</param>
  /// <param name="args">メソッド引数。</param>
  /// <returns>実装メソッドの戻り値。</returns>
  private async Task<object?> InvokeImplementation(MethodInfo implementationMethod, object?[]? args) {
    object? result;
    try {
      result = implementationMethod.Invoke(_target, args);
    } catch (TargetInvocationException ex) {
      // 同期メソッドが出した例外を、包まずに内側の処理へ返す。
      ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
      throw;
    }

    if (result is not Task task) {
      return result;
    }

    // 非同期メソッドは Task の完了を待ち、Task<T> のときだけ結果を取り出す。
    await task.ConfigureAwait(false);
    var taskType = task.GetType();
    if (!taskType.IsGenericType) {
      return null;
    }

    return taskType.GetProperty("Result")!.GetValue(task);
  }

  /// <summary>
  /// インタフェースメソッドに対応する実装型のメソッドを取得します。
  /// </summary>
  /// <param name="interfaceMethod">インタフェースメソッド。</param>
  /// <returns>対応する実装メソッド。</returns>
  private MethodInfo ResolveImplementationMethod(MethodInfo interfaceMethod) {
    var parameterTypes = interfaceMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
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
    // メソッドに付いていればそれを使い、無ければクラスに付いたものを使う。
    if (implementationMethod.GetCustomAttributes(attributeType, inherit: true).FirstOrDefault() is AspectAttribute methodAttribute) {
      return methodAttribute;
    }

    if (implementationMethod.DeclaringType?.GetCustomAttributes(attributeType, inherit: true).FirstOrDefault() is AspectAttribute classAttribute) {
      return classAttribute;
    }

    return null;
  }

  /// <summary>
  /// Order が小さいもの、同じときは宣言順が早いものを先にします。
  /// </summary>
  /// <param name="left">比較元。</param>
  /// <param name="right">比較先。</param>
  /// <returns>並び順。</returns>
  private int CompareAdvice(AdviceCall left, AdviceCall right) {
    var order = left.Order.CompareTo(right.Order);
    if (order != 0) {
      return order;
    }

    return left.DeclarationOrder.CompareTo(right.DeclarationOrder);
  }
}
