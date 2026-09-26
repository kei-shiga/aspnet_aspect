using System.Reflection;
using AspnetAspect.Aspects;
using AspnetAspect.Proxy;
using Microsoft.Extensions.DependencyInjection;

namespace AspnetAspect.DependencyInjection;

/// <summary>
/// アスペクト付きプロキシを DI に登録する拡張メソッドです。
/// </summary>
public static class AspectServiceCollectionExtensions {
  /// <summary>
  /// Scoped 寿命でインタフェースプロキシを登録します。
  /// </summary>
  /// <typeparam name="TInterface">登録するインタフェース型。</typeparam>
  /// <typeparam name="TImplementation">実装型。</typeparam>
  /// <param name="services">サービスコレクション。</param>
  /// <returns>チェーン用のサービスコレクション。</returns>
  public static IServiceCollection AddScopedAspectProxy<TInterface, TImplementation>(this IServiceCollection services)
      where TInterface : class
      where TImplementation : class, TInterface {
    return AddAspectProxy<TInterface, TImplementation>(services, ServiceLifetime.Scoped);
  }

  /// <summary>
  /// Singleton 寿命でインタフェースプロキシを登録します。
  /// </summary>
  /// <typeparam name="TInterface">登録するインタフェース型。</typeparam>
  /// <typeparam name="TImplementation">実装型。</typeparam>
  /// <param name="services">サービスコレクション。</param>
  /// <returns>チェーン用のサービスコレクション。</returns>
  public static IServiceCollection AddSingletonAspectProxy<TInterface, TImplementation>(this IServiceCollection services)
      where TInterface : class
      where TImplementation : class, TInterface {
    return AddAspectProxy<TInterface, TImplementation>(services, ServiceLifetime.Singleton);
  }

  /// <summary>
  /// Transient 寿命でインタフェースプロキシを登録します。
  /// </summary>
  /// <typeparam name="TInterface">登録するインタフェース型。</typeparam>
  /// <typeparam name="TImplementation">実装型。</typeparam>
  /// <param name="services">サービスコレクション。</param>
  /// <returns>チェーン用のサービスコレクション。</returns>
  public static IServiceCollection AddTransientAspectProxy<TInterface, TImplementation>(this IServiceCollection services)
      where TInterface : class
      where TImplementation : class, TInterface {
    return AddAspectProxy<TInterface, TImplementation>(services, ServiceLifetime.Transient);
  }

  /// <summary>
  /// 指定した寿命で実装型とインタフェースプロキシを登録します。
  /// </summary>
  /// <typeparam name="TInterface">登録するインタフェース型。</typeparam>
  /// <typeparam name="TImplementation">実装型。</typeparam>
  /// <param name="services">サービスコレクション。</param>
  /// <param name="lifetime">DI 寿命。</param>
  /// <returns>チェーン用のサービスコレクション。</returns>
  private static IServiceCollection AddAspectProxy<TInterface, TImplementation>(
      IServiceCollection services,
      ServiceLifetime lifetime)
      where TInterface : class
      where TImplementation : class, TInterface {
    services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));

    services.Add(new ServiceDescriptor(
        typeof(TInterface),
        sp => CreateProxy<TInterface, TImplementation>(sp),
        lifetime));

    return services;
  }

  /// <summary>
  /// 実装インスタンスと DI 上の IAspect 一覧からプロキシを生成します。
  /// </summary>
  /// <typeparam name="TInterface">プロキシが実装するインタフェース型。</typeparam>
  /// <typeparam name="TImplementation">実装型。</typeparam>
  /// <param name="serviceProvider">DI コンテナ。</param>
  /// <returns>アスペクト付きプロキシ。</returns>
  private static TInterface CreateProxy<TInterface, TImplementation>(IServiceProvider serviceProvider)
      where TInterface : class
      where TImplementation : class, TInterface {
    var target = serviceProvider.GetRequiredService<TImplementation>();
    var aspects = serviceProvider.GetServices<IAspect>().OrderBy(a => a.Order).ToList();

    var proxy = DispatchProxy.Create<TInterface, AspectDispatchProxy>()
        ?? throw new InvalidOperationException($"プロキシを生成できません: {typeof(TInterface).Name}");

    ((AspectDispatchProxy)(object)proxy).Initialize(target, typeof(TImplementation), aspects);
    return proxy;
  }
}
