using System.Reflection;
using AspnetAspect.Aspects;

namespace AspnetAspect.Proxy;

/// <summary>
/// アスペクト型に付いた ApplyTo とアドバイスメソッドの解析結果です。
/// </summary>
internal sealed class AspectShape {
  /// <summary>
  /// アスペクト型を読み、クラスの ApplyTo とアドバイスメソッドを集めます。
  /// </summary>
  /// <param name="aspectType">アスペクトの型。</param>
  public AspectShape(Type aspectType) {
    try {
      // クラスに付いた ApplyTo は、InvokeAsync を選ぶときの適用先になる。
      ClassPatterns = aspectType
        .GetCustomAttributes<ApplyToAttribute>(inherit: true)
        .Select(attribute => new ApplyToPattern(attribute.Expression))
        .ToArray();
      Advices = ReadAdvices(aspectType);
    } catch (FormatException ex) {
      // 式の誤りは、呼び出しの途中ではなくプロキシ生成時に分かるようにする。
      throw new InvalidOperationException($"アスペクト {aspectType.Name} の ApplyTo を解釈できません。{ex.Message}", ex);
    }
  }

  /// <summary>
  /// アスペクトクラスに付いた ApplyTo です。
  /// </summary>
  public IReadOnlyList<ApplyToPattern> ClassPatterns { get; }

  /// <summary>
  /// アドバイスメソッドの定義です。
  /// </summary>
  public IReadOnlyList<AdviceDefinition> Advices { get; }

  /// <summary>
  /// アスペクト型と基底型からアドバイスメソッドを集めます。
  /// </summary>
  /// <param name="aspectType">アスペクトの型。</param>
  /// <returns>アドバイスメソッドの定義。</returns>
  private List<AdviceDefinition> ReadAdvices(Type aspectType) {
    var advices = new List<AdviceDefinition>();
    for (var current = aspectType; current is not null && current != typeof(object); current = current.BaseType) {
      var methods = current.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
      foreach (var method in methods) {
        // プロパティのアクセサはアドバイスにしない。
        if (method.IsSpecialName) {
          continue;
        }

        var advice = method.GetCustomAttributes<AdviceAttribute>(inherit: true).ToArray();
        // ApplyTo だけのメソッドは共有の式なので、ここでは集めない。
        if (advice.Length == 0) {
          continue;
        }

        if (advice.Length > 1) {
          throw new FormatException($"メソッド {method.Name} にアドバイス Attribute が複数付いています。");
        }

        var attribute = advice[0];
        ValidateSignature(method, attribute);
        // 宣言順はリストへ足した順。同じ Order のときの並び順に使う。
        advices.Add(new AdviceDefinition(method, attribute, ResolvePatterns(aspectType, method, attribute), advices.Count));
      }
    }

    return advices;
  }

  /// <summary>
  /// アドバイスメソッドの ApplyTo を集めます。
  /// </summary>
  /// <param name="aspectType">アスペクトの型。</param>
  /// <param name="method">アドバイスメソッド。</param>
  /// <param name="attribute">タイミング Attribute。</param>
  /// <returns>適用先の式。複数あるときはどれか 1 つに一致すれば対象です。</returns>
  private IReadOnlyList<ApplyToPattern> ResolvePatterns(Type aspectType, MethodInfo method, AdviceAttribute attribute) {
    var own = method.GetCustomAttributes<ApplyToAttribute>(inherit: true).ToArray();
    // メソッド自身に ApplyTo があるときは、参照先よりこちらを使う。
    if (own.Length > 0) {
      return own.Select(item => new ApplyToPattern(item.Expression)).ToArray();
    }

    if (string.IsNullOrWhiteSpace(attribute.ApplyToMethod)) {
      throw new FormatException($"メソッド {method.Name} に ApplyTo または参照先メソッド名がありません。");
    }

    MethodInfo? marker = null;
    // 参照先は引数のないメソッド。派生型から基底型の順に探す。
    for (var current = aspectType; current is not null && current != typeof(object); current = current.BaseType) {
      marker = current.GetMethod(
        attribute.ApplyToMethod,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
        binder: null,
        Type.EmptyTypes,
        modifiers: null);
      if (marker is not null) {
        break;
      }
    }

    if (marker is null) {
      throw new FormatException($"メソッド {attribute.ApplyToMethod} が見つかりません。");
    }

    var patterns = marker.GetCustomAttributes<ApplyToAttribute>(inherit: true).ToArray();
    if (patterns.Length == 0) {
      throw new FormatException($"メソッド {attribute.ApplyToMethod} に ApplyTo がありません。");
    }

    return patterns.Select(item => new ApplyToPattern(item.Expression)).ToArray();
  }

  /// <summary>
  /// アドバイスメソッドの引数が、そのタイミングで受け取れる型かを確認します。
  /// </summary>
  /// <param name="method">アドバイスメソッド。</param>
  /// <param name="attribute">タイミング Attribute。</param>
  private void ValidateSignature(MethodInfo method, AdviceAttribute attribute) {
    var hasNext = false;
    var hasResult = false;
    foreach (var parameter in method.GetParameters()) {
      var parameterType = parameter.ParameterType;
      // AspectContext はどのタイミングでも受け取れる。
      if (parameterType == typeof(AspectContext)) {
        continue;
      }

      if (parameterType == typeof(AspectNext)) {
        if (attribute is not AroundAttribute) {
          throw new FormatException($"メソッド {method.Name} の AspectNext は Around だけが受け取れます。");
        }

        hasNext = true;
        continue;
      }

      if (typeof(Exception).IsAssignableFrom(parameterType)) {
        if (attribute is not AfterThrowAttribute) {
          throw new FormatException($"メソッド {method.Name} の Exception は AfterThrow だけが受け取れます。");
        }

        continue;
      }

      if (parameterType == typeof(object)) {
        // object は AfterReturn が受け取る対象メソッドの戻り値。
        if (attribute is not AfterReturnAttribute) {
          throw new FormatException($"メソッド {method.Name} の戻り値引数は AfterReturn だけが受け取れます。");
        }

        if (hasResult) {
          throw new FormatException($"メソッド {method.Name} の戻り値引数が複数あります。");
        }

        hasResult = true;
        continue;
      }

      throw new FormatException($"メソッド {method.Name} の引数 {parameter.Name} は受け取れません。");
    }

    if (attribute is AroundAttribute && !hasNext) {
      // Around は next を呼ばないと内側へ進めない。
      throw new FormatException($"メソッド {method.Name} には AspectNext が必要です。");
    }
  }
}
