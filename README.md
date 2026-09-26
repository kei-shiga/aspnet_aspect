# AspnetAspect

ASP.NET Core 上で **Spring 風の AOP（Attribute + インターセプタ連鎖）** を実現するライブラリと、その動作確認用サンプル Web アプリです。  
`DispatchProxy` でインタフェース実装を包み、DI から解決した `IAspect` がメソッド呼び出しの前後に介入します。

## 要件

### 目的

- ASP.NET 上で Spring の Aspect に近い **アスペクト指向** を実現する。
- **Proxy パターン + Attribute** で、横断的関心事（トランザクション、メソッドログなど）を本処理から分離する。
- サンプルとして **Transactional（TransactionScope）** と **メソッド開始・終了ログ** を提供し、ブラウザから動作を確認できるようにする。

### 技術前提

| 項目 | 要件 |
|------|------|
| 言語 | C#（.NET 10 ターゲット） |
| AOP 実装 | `System.Reflection.DispatchProxy`（追加 AOP パッケージは使わない） |
| プロキシ対象 | **インタフェース経由の呼び出しのみ**（実装型を直接注入して呼んではプロキシを通らない） |
| ライブラリ本体 | `AspnetAspect/AspnetAspect` — **DB に非依存** |
| サンプル | `AspnetAspect/AspectSample` — MVC + MySQL |
| データアクセス | **ADO.NET**（`DbConnection` / `DbCommand` 等）。ドライバは **MySqlConnector** |
| トランザクション | **`System.Transactions.TransactionScope` のみ**。独自 TransactionManager 等は作らない |
| 説明・コメント | **日本語**（README、操作説明、XML ドキュメント） |

### ライブラリ機能要件

- `AspectAttribute` 基底、`IAspect`、`AspectContext`、`AspectNext` を提供する。
- `AspectDispatchProxy` がインタフェース呼び出しを横取りし、有効なアスペクトを **連鎖実行**する。
- 戻り値は **`void` / 同期 / `Task` / `Task<T>`** に対応する。
- DI 拡張として、プロキシごとに次の 3 種を提供する（`ServiceLifetime` を引数で渡す API は **設けない**）。
  - `AddScopedAspectProxy<TInterface, TImplementation>()`
  - `AddSingletonAspectProxy<TInterface, TImplementation>()`
  - `AddTransientAspectProxy<TInterface, TImplementation>()`
- 各拡張は **実装型** と **インタフェース用プロキシ** を同じ寿命で登録する。Service / Logic / Repository のいずれにも使える。
- 有効にするアスペクトは **DI に登録された `IAspect` 実装**のうち、呼び出し対象の **Attribute 型が一致するものだけ**（グローバルな `AddAspect` で全メソッドに付与する方式は **採用しない**）。

### アスペクト動作要件

**いつ実行するか**

- メソッドに Attribute があればそれを優先。なければ **クラス** の Attribute を参照。
- どちらにも無い Attribute 型に対応するアスペクトは **実行しない**。

**複数アスペクト**

- `IAspect.Order` の **昇順**（値が小さいほど **外側**：先に開始し、後に終了）。
- サンプルでは **ログがトランザクションより外側**（`MethodLoggingAspect.Order` が `TransactionAspect.Order` より小さい）。

**Transactional**

- `Required` → `TransactionScopeOption.Required`（外側スコープに参加）。
- `RequiresNew` → `TransactionScopeOption.RequiresNew`（外側と独立した環境トランザクション）。
- `using` + 正常時 `Complete()`、例外時は `Complete()` せずロールバックし **例外を再スロー**。
- Repository は接続文字列から `MySqlConnection` を開き、`TransactionScope` のアンビエントトランザクションに **自動参加**させる。

### AspectSample アーキテクチャ要件

**レイヤード構成**

- Controller → Service → Logic → Repository。

**呼び出しの許可 / 禁止**

| 許可 | 禁止 |
|------|------|
| Controller → Service | Service → Service |
| Service → Logic | Logic → Service |
| Logic → Repository | Repository → 上位レイヤ |
| Logic → Logic | |

- 依存はすべて **コンストラクタインジェクション**。
- 振替の入口は **`IAccountService` のみ**。`AccountService` が `IAccountLogic`（口座）と `IAuditLogic`（監査）を呼び分ける。
- アスペクト対象は Service に限らない。**Logic / Repository** も、インタフェース経由で呼ばれるメソッド（またはクラス）に Attribute を付ければ対象にできる。

**サンプルアスペクト**

- `LogAttribute` + `MethodLoggingAspect`（`ILogger` で開始・終了、例外時も終了ログのあと再スロー）。
- `TransactionalAttribute`（既定 `Required`）+ `TransactionAspect`（`TransactionPropagation`: `Required` / `RequiresNew`）。
- 例: `AccountService` に `[Log]` `[Transactional]`、`AuditLogic.RecordAsync` に `[Transactional(Propagation = RequiresNew)]`。

**画面・操作（`/Account`）**

- 口座残高・監査ログの表示。
- 正常振替。
- **口座更新後に意図的失敗** → 同一外側トランザクション内で口座更新・監査が **ともにロールバック**。
- **監査確定後に外側失敗** → 監査は RequiresNew で **残り**、口座更新は外側ロールバックで **戻る**。
- 残高 1000 / 1000・監査ログ空への **初期化**。
- ナビから Account へ遷移できること。

**インフラ**

- リポジトリ直下 `docker-compose.yml`（MySQL 8.4、`aspect_sample` DB、初期 SQL は `docker/mysql/init`）。
- 接続文字列キー `ConnectionStrings:AspectSample`（文字化け防止のため **`CharSet=utf8mb4`** を含める）。
- 操作手順は [docs/mysql.md](docs/mysql.md) に記載する。

### 動作確認要件

ブラウザで `/Account` を開き、次を確認する。

1. 正常振替  
2. 同一トランザクション内でのロールバック  
3. `RequiresNew` により監査だけ残るケース  
4. 初期化  
5. アプリログに **メソッド開始・終了** が出力されること  

### 実装スコープ外・判断ルール

- 計画（設計書）に書いていない設計判断は、**実装前に確認**する。
- ライブラリに MySQL や EF Core などの永続化依存を **入れない**。

## リポジトリ構成

```
aspnet_aspect/
├── AspnetAspect/
│   ├── AspnetAspect.slnx          … ソリューション
│   ├── AspnetAspect/              … AOP ライブラリ（net10.0）
│   └── AspectSample/              … MVC サンプル（net10.0）
├── docker-compose.yml             … MySQL 8.4（サンプル用）
├── docker/mysql/init/             … DB 初期化 SQL
├── docs/mysql.md                  … MySQL の起動・接続の詳細
└── .editorconfig                  … インデント・C# 整形ルール
```

## ソリューション概要

| プロジェクト | 役割 |
|-------------|------|
| **AspnetAspect** | Attribute 定義、`IAspect`、プロキシ、DI 拡張 |
| **AspectSample** | 口座振替デモ。ログ／トランザクションアスペクトと MySQL ADO.NET |

### サンプルのレイヤー

```
Controller (AccountController)
    → Service (IAccountService)     … [Log] [Transactional] クラス属性
        → Logic (IAccountLogic, IAuditLogic)
            → Repository (IAccountRepository, IAuditRepository)
```

- **Service → Service の呼び出しは行わない**（Logic 経由）。
- **Logic → Logic** はサンプルで使用（振替 + 監査記録）。
- Controller / Service / Logic / Repository はいずれも **インタフェース経由で DI 登録**し、プロキシ対象にできます。

## ライブラリ（AspnetAspect）の要点

### 概念

| 要素 | 説明 |
|------|------|
| `AspectAttribute` | アスペクト適用対象を示す Attribute の基底 |
| `IAspect` | `AttributeType`・`Order`・`InvokeAsync` を持つ介入処理 |
| `AspectContext` | 1 回の呼び出しについて、メソッド・引数・Attribute などを渡す |
| `AspectDispatchProxy` | インタフェース呼び出しを横取りし、一致するアスペクトを順に実行 |
| `Add*AspectProxy` | 実装型と **プロキシ付きインタフェース** を同じ寿命で登録 |

### アスペクトの適用ルール

- DI に登録された `IAspect` のうち、**メソッドまたはクラスに付いた Attribute 型が一致するものだけ**が実行されます。
- 複数ある場合は `IAspect.Order` の昇順（小さいほど外側）で連鎖します。
- 戻り値 `Task` / `Task<T>` / 同期戻り値 / `void` をプロキシ側で扱い分けます。

### DI 登録例

```csharp
builder.Services.AddScoped<MyAspect>();
builder.Services.AddScoped<IAspect>(sp => sp.GetRequiredService<MyAspect>());

builder.Services.AddScopedAspectProxy<IMyService, MyService>();
// AddSingletonAspectProxy / AddTransientAspectProxy も同様
```

## サンプルアスペクト（AspectSample）

| 名前 | Attribute | 概要 |
|------|-----------|------|
| **MethodLoggingAspect** | `LogAttribute` | メソッド開始・終了（例外時も）をログ出力 |
| **TransactionAspect** | `TransactionalAttribute` | `TransactionScope` でトランザクション境界（`Required` / `RequiresNew`） |

`AccountService` に `[Log]` と `[Transactional]` を付与。  
`AuditLogic.RecordAsync` には `[Transactional(Propagation = RequiresNew)]` により、監査を外側トランザクションから切り離すデモを行います。

### 振替デモ（`/Account`）

| 操作 | 意図 |
|------|------|
| 正常振替 | 口座 A → B へ残高移動 + 監査ログ |
| 口座更新後に失敗 | Logic 内で例外 → **口座更新がロールバック**されることを確認 |
| 監査確定後に外側失敗 | Service 内で例外 → **外側トランザクション全体のロールバック**を確認 |
| 初期化 | 口座残高・監査ログを初期状態に戻す |

## 動かし方

### 前提

- .NET 10 SDK
- Docker（MySQL 用）

### 1. MySQL

リポジトリ直下で:

```powershell
docker compose up -d
```

接続情報・トラブルシュートは [docs/mysql.md](docs/mysql.md) を参照。

### 2. Web アプリ

```powershell
cd AspnetAspect/AspectSample
dotnet run
```

ブラウザで `/Account` を開き、振替とログ出力を確認します。

接続文字列は `AspectSample/appsettings.json` の `ConnectionStrings:AspectSample`（`CharSet=utf8mb4` 付き）。

## コーディング規約（抜粋）

`.editorconfig` に従います。

- インデント: 半角スペース 2
- class / メソッド / `if`・`try` など: **`{` は前行**（namespace など一部型宣言は例外）
- XML ドキュメント: **`/// <see ... />` と `/// <inheritdoc />` は使わない**。メソッドには `<param>` / `<returns>` を記載
- 1 ファイル 1 クラス（Models など）

## 関連ドキュメント

- [docs/mysql.md](docs/mysql.md) — MySQL コンテナ、初期化、文字コード

## ライセンス

（未設定。必要に応じて追記してください。）
