---
name: unity-docs
description: |
  Unity Documentation offline reference skill.
  Downloads and indexes Unity Manual & ScriptReference locally for Claude Code.
  Triggers:
  - Unity API, component, or Editor feature questions
  - Unity ScriptReference or Manual page lookups
  - "How do I do X in Unity?" in a Unity project
  - When working in a Unity project and needing documentation
user-invocable: true
allowed-tools: Bash(*), Read, Glob, Grep, Write
---

# Unity Docs Skill

## 初期化チェック

トリガーされたら、まずこの SKILL.md と同じディレクトリにある `docs/Manual/` が存在するか確認する。

### 未初期化の場合

セットアップスクリプトを実行する：

```bash
bash .claude/skills/unity-docs/scripts/setup.sh
```

バージョンを明示的に指定する場合：

```bash
bash .claude/skills/unity-docs/scripts/setup.sh 6000.0
```

セットアップは以下を行う：
1. `ProjectSettings/ProjectVersion.txt` からUnityバージョンを自動検出（引数なしの場合）
2. プロジェクトルートに `.unity-docs/` が存在すればスキルディレクトリに移動（レガシーマイグレーション）
3. なければ Unity公式CDNからドキュメントZIPをダウンロード
4. `docs/` に展開
5. `references/index.txt` に索引を生成

### 初期化済みの場合

`references/index.txt` を読み込んで索引を取得する。

## 索引の使い方

索引ファイルはパイプ区切りのコンパクトフォーマット：

```
[Unity Docs Index]|root: .claude/skills/unity-docs/docs|version: X.Y|Manual:{file1.html,file2.html}|Manual/sub:{file3.html}|...
```

- 各エントリはディレクトリごとにHTMLファイルをグループ化
- ファイル名から内容を推測して関連ページを特定
- 特定したHTMLファイルを `Read` ツールで読み込む

## ドキュメント参照

Manual ページの読み込み：
- パス: `.claude/skills/unity-docs/docs/Manual/<PageName>.html`

ScriptReference（API docs）の読み込み：
- パス: `.claude/skills/unity-docs/docs/ScriptReference/<ClassName>.html`
- サイズが大きいため索引には含まれない。ファイル名で直接 `Glob` 検索する

## 注意事項

- pre-training の知識よりドキュメントの内容を優先すること（retrieval-led reasoning）
- HTMLファイルはそのまま読める（タグを含むがテキスト内容は十分取得可能）
