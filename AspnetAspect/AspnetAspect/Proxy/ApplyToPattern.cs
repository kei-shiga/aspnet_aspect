using System.Reflection;

namespace AspnetAspect.Proxy;

/// <summary>
/// ApplyTo の method または type 式を、インタフェースメソッドと照合します。
/// </summary>
internal sealed class ApplyToPattern {
  /// <summary>
  /// method 式の戻り値の型です。type 式のときは * です。
  /// </summary>
  private readonly string _returnType;

  /// <summary>
  /// インタフェースの完全名、または名前空間です。
  /// </summary>
  private readonly string _typeName;

  /// <summary>
  /// メソッド名です。* を含められます。type 式のときは * です。
  /// </summary>
  private readonly string _methodName;

  /// <summary>
  /// 引数の数と型を問わないとき true です。
  /// </summary>
  private readonly bool _anyArguments;

  /// <summary>
  /// type 式のとき true です。
  /// </summary>
  private readonly bool _isTypePattern;

  /// <summary>
  /// 名前空間直下のインタフェースだけを選ぶとき true です。
  /// </summary>
  private readonly bool _namespaceOnly;

  /// <summary>
  /// 名前空間とその配下を選ぶとき true です。
  /// </summary>
  private readonly bool _namespaceRecursive;

  /// <summary>
  /// 式を解析して照合条件を生成します。
  /// </summary>
  /// <param name="expression">method または type で始まる式。</param>
  public ApplyToPattern(string expression) {
    var text = expression.Trim();
    // method(...) は戻り値、型名、メソッド名、引数で照合する。
    if (text.StartsWith("method(", StringComparison.Ordinal) && text.EndsWith(')')) {
      var method = ReadMethod(text["method(".Length..^1].Trim());
      _returnType = method.ReturnType;
      _typeName = method.TypeName;
      _methodName = method.MethodName;
      _anyArguments = method.AnyArguments;
      _isTypePattern = false;
      _namespaceOnly = false;
      _namespaceRecursive = false;
      return;
    }

    // type(...) は型の範囲だけを見る。メソッド名と引数では絞らない。
    if (text.StartsWith("type(", StringComparison.Ordinal) && text.EndsWith(')')) {
      var typePattern = ReadType(text["type(".Length..^1].Trim());
      _returnType = "*";
      _typeName = typePattern.TypeName;
      _methodName = "*";
      _anyArguments = true;
      _isTypePattern = true;
      _namespaceOnly = typePattern.NamespaceOnly;
      _namespaceRecursive = typePattern.NamespaceRecursive;
      return;
    }

    throw new FormatException("式は method(...) または type(...) です。");
  }

  /// <summary>
  /// 呼び出しのインタフェースメソッドが式に一致するかを判定します。
  /// </summary>
  /// <param name="interfaceMethod">呼び出されたインタフェースメソッド。</param>
  /// <returns>一致するとき true。</returns>
  public bool Matches(MethodInfo interfaceMethod) {
    var declaringType = interfaceMethod.DeclaringType;
    if (declaringType is null) {
      return false;
    }

    if (_isTypePattern) {
      return MatchesType(declaringType);
    }

    // 呼び出し元が見ているインタフェースの完全名と比較する。
    if (declaringType.FullName != _typeName) {
      return false;
    }

    if (!MatchesWildcard(_methodName, interfaceMethod.Name)) {
      return false;
    }

    if (_returnType != "*" && !MatchesReturnType(interfaceMethod.ReturnType, _returnType)) {
      return false;
    }

    // (..) は引数を見ない。() は引数のないメソッドだけを対象にする。
    if (_anyArguments) {
      return true;
    }

    return interfaceMethod.GetParameters().Length == 0;
  }

  /// <summary>
  /// method 式の内側を読みます。
  /// </summary>
  /// <param name="body">method の括弧の内側。</param>
  /// <returns>戻り値の型、型名、メソッド名、引数を問わないか。</returns>
  private (string ReturnType, string TypeName, string MethodName, bool AnyArguments) ReadMethod(string body) {
    // 引数リストは末尾の括弧。型名の中のドットより後ろの括弧を使う。
    var open = body.LastIndexOf('(');
    if (open < 0 || !body.EndsWith(')')) {
      throw new FormatException("method の引数リストがありません。");
    }

    var arguments = body[(open + 1)..^1].Trim();
    var anyArguments = arguments == "..";
    if (!anyArguments && arguments.Length != 0) {
      throw new FormatException("method の引数は () または (..) です。");
    }

    var signature = body[..open].Trim();
    var space = signature.IndexOf(' ');
    if (space <= 0) {
      throw new FormatException("method には戻り値の型と型名.メソッド名が必要です。");
    }

    var returnType = signature[..space].Trim();
    var typeAndMethod = signature[(space + 1)..].Trim();
    // メソッド名は最後のドットの右側。それより左が名前空間付きの型名になる。
    var lastDot = typeAndMethod.LastIndexOf('.');
    if (lastDot <= 0 || lastDot == typeAndMethod.Length - 1) {
      throw new FormatException("method の型名とメソッド名はドットで区切ります。");
    }

    return (returnType, typeAndMethod[..lastDot], typeAndMethod[(lastDot + 1)..], anyArguments);
  }

  /// <summary>
  /// type 式の内側を読みます。
  /// </summary>
  /// <param name="body">type の括弧の内側。</param>
  /// <returns>型名または名前空間と、名前空間の選び方。</returns>
  private (string TypeName, bool NamespaceOnly, bool NamespaceRecursive) ReadType(string body) {
    if (body.Length == 0) {
      throw new FormatException("type の型名が空です。");
    }

    // ..* は末尾が .* でもあるので、配下を含む指定を先に判定する。
    if (body.EndsWith("..*", StringComparison.Ordinal)) {
      var prefix = body[..^3];
      if (prefix.Length == 0) {
        throw new FormatException("type の名前空間が空です。");
      }

      return (prefix, false, true);
    }

    // .* はその名前空間の直下だけ。どちらでもなければ型の完全名。
    if (body.EndsWith(".*", StringComparison.Ordinal)) {
      var ns = body[..^2];
      if (ns.Length == 0 || ns.EndsWith('.')) {
        throw new FormatException("type の名前空間が不正です。");
      }

      return (ns, true, false);
    }

    return (body, false, false);
  }

  /// <summary>
  /// インタフェースの型が type 式に一致するかを判定します。
  /// </summary>
  /// <param name="declaringType">呼び出しのインタフェース型。</param>
  /// <returns>一致するとき true。</returns>
  private bool MatchesType(Type declaringType) {
    var fullName = declaringType.FullName;
    if (fullName is null) {
      return false;
    }

    // 名前空間そのものではなく、その名前空間に属する型の完全名だけを対象にする。
    if (_namespaceRecursive) {
      return fullName.StartsWith(_typeName + ".", StringComparison.Ordinal);
    }

    if (_namespaceOnly) {
      return declaringType.Namespace == _typeName;
    }

    return fullName == _typeName;
  }

  /// <summary>
  /// 戻り値の型名が式と一致するかを判定します。
  /// </summary>
  /// <param name="returnType">インタフェースメソッドの戻り値の型。</param>
  /// <param name="expected">式に書いた型名。</param>
  /// <returns>完全名または型名が一致するとき true。</returns>
  private bool MatchesReturnType(Type returnType, string expected) {
    return returnType.FullName == expected || returnType.Name == expected;
  }

  /// <summary>
  /// * を含むメソッド名が一致するかを判定します。
  /// </summary>
  /// <param name="pattern">式のメソッド名。</param>
  /// <param name="value">インタフェースメソッドの名前。</param>
  /// <returns>一致するとき true。</returns>
  private bool MatchesWildcard(string pattern, string value) {
    // * で区切った断片が、左から順に名前の中へ現れるかを見る。
    var parts = pattern.Split('*');
    var index = 0;
    for (var i = 0; i < parts.Length; i++) {
      var part = parts[i];
      if (part.Length == 0) {
        continue;
      }

      var found = value.IndexOf(part, index, StringComparison.Ordinal);
      if (found < 0) {
        return false;
      }

      // パターンが * で始まらないときは、最初の断片が名前の先頭にある。
      if (i == 0 && found != 0) {
        return false;
      }

      index = found + part.Length;
    }

    // パターンが * で終わらないときは、最後の断片の直後が名前の終わりである。
    if (!pattern.EndsWith('*') && index != value.Length) {
      return false;
    }

    return true;
  }
}
