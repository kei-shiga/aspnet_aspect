# MySQL 操作説明（AspectSample）

## 起動

リポジトリ直下で次を実行する。

```powershell
docker compose up -d
```

初回起動時、`docker/mysql/init` 内の SQL が自動実行され、テーブルと初期データが作成される。

## 接続情報

| 項目 | 値 |
|------|-----|
| ホスト | 127.0.0.1 |
| ポート | 3306 |
| データベース | aspect_sample |
| ユーザー | aspect |
| パスワード | aspect |
| root パスワード | root |

AspectSample の接続文字列は `appsettings.json` の `ConnectionStrings:AspectSample` を参照する。文字化けを防ぐため `CharSet=utf8mb4` を含める。

口座名称が文字化けしている場合は、初期化 SQL 実行時の文字コード設定が反映されていない可能性がある。`docker compose down -v` のあと `docker compose up -d` で DB を作り直す。

## 作成されるテーブル

- `account` … 口座（初期: 口座A / 口座B、残高 1000.00）
- `audit_log` … 監査ログ

## 初期化のやり直し

データボリュームごと削除してから再起動する。

```powershell
docker compose down -v
docker compose up -d
```

## 停止

```powershell
docker compose down
```

## サンプル画面での確認

1. AspectSample を起動する。
2. ブラウザで `/Account` を開く。
3. 「正常振替」「口座更新後に失敗」「監査確定後に外側失敗」「初期化」を順に試す。
4. アプリログにメソッドの開始・終了が出力されることを確認する。
