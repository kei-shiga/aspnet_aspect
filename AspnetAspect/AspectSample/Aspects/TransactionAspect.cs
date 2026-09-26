using System.Transactions;
using AspnetAspect.Aspects;

namespace AspectSample.Aspects;

/// <summary>
/// TransactionalAttribute に従い TransactionScope でトランザクション境界を張るアスペクトです。
/// </summary>
public sealed class TransactionAspect : IAspect {
  /// <summary>
  /// このアスペクトが処理する Attribute の型です。
  /// </summary>
  public Type AttributeType => typeof(TransactionalAttribute);

  /// <summary>
  /// 実行順序。値が小さいほど外側（先に開始し、後に終了する側）です。
  /// </summary>
  public int Order => 1;

  /// <summary>
  /// アスペクト処理を実行し、必要に応じて内側の処理へ進みます。
  /// </summary>
  /// <param name="context">呼び出しコンテキスト。</param>
  /// <param name="next">内側のアスペクトまたは実装メソッド。</param>
  /// <returns>完了を表す Task。</returns>
  public async Task InvokeAsync(AspectContext context, AspectNext next) {
    var transactional = (TransactionalAttribute)context.AspectAttribute;
    var scopeOption = transactional.Propagation switch {
      TransactionPropagation.RequiresNew => TransactionScopeOption.RequiresNew,
      _ => TransactionScopeOption.Required
    };

    using var scope = new TransactionScope(
      scopeOption,
      TransactionScopeAsyncFlowOption.Enabled);

    await next().ConfigureAwait(false);
    scope.Complete();
  }
}
