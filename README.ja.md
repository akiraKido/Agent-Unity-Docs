# Unity Docs Index Generator

[English](README.md) | [中文](README.zh-CN.md)

AIコーディングエージェントがUnityプロジェクトで作業する際に、公式ドキュメントを参照できるようにするClaude Codeスキル。

Inspired by [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## インストール

### Unity Package Manager経由（推奨）

1. **Window > Package Manager** を開く
2. **「+」ボタン > Add package from git URL...** をクリック
3. 入力: `https://github.com/akiraKido/Agent-Unity-Docs.git`

Editor初回起動時に、Claude Codeスキルのインストールが自動で提案されます。

### 手動インストール

`.claude/skills/unity-docs/` ディレクトリをUnityプロジェクトにコピー:

```bash
# Unityプロジェクトのルートから
mkdir -p .claude/skills
cp -r <このリポジトリのパス>/.claude/skills/unity-docs .claude/skills/
```

セットアップを実行:

```bash
bash .claude/skills/unity-docs/scripts/setup.sh
```

バージョンを明示的に指定する場合:

```bash
bash .claude/skills/unity-docs/scripts/setup.sh 6000.0
```

セットアップは以下を行います:
1. `ProjectSettings/ProjectVersion.txt` からUnityバージョンを自動検出
2. 既存の `.unity-docs/` があればスキルディレクトリに移動（再ダウンロード不要）
3. `CLAUDE.md` にレガシーインデックスがあれば削除
4. Unity CDNからドキュメントをダウンロード
5. `.claude/skills/unity-docs/references/index.txt` にコンパクトな索引を生成

セットアップ後、Unity関連の質問をするとClaude Codeが自動でこのスキルをトリガーします。`/unity-docs` で明示的に起動することもできます。

## 生成されるインデックス形式

```
[Unity Docs Index]|root: .claude/skills/unity-docs/docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...
```

- パイプ `|` 区切りでトークン節約
- ディレクトリごとにファイルをグループ化: `dir:{file1,file2}`
- ScriptReferenceはサイズのため索引外（直接参照は可能）

## コンテキスト使用量

生成されるインデックスのサイズ目安:

| 項目 | 値 |
|------|-----|
| ファイルサイズ | 約 100 KB |
| 推定トークン数 | 約 2.5万 |
| コンテキスト占有率 | 約 12% (200Kコンテキスト想定) |

## ファイル構造

```
YourUnityProject/
├── .claude/
│   └── skills/
│       └── unity-docs/
│           ├── SKILL.md            # スキル定義
│           ├── cdn_versions.json   # CDN設定
│           ├── scripts/
│           │   └── setup.sh        # DL＋索引生成スクリプト
│           ├── docs/               # DLされたドキュメント（gitignored）
│           │   ├── Manual/
│           │   └── ScriptReference/
│           └── references/
│               └── index.txt       # 生成された索引（gitignored）
└── .gitignore
```

## Community CDN

Unity公式CDNからのダウンロードが遅い場合、以下のコミュニティCDNを利用できます:

```
https://unity-docs.tsuchigoe.com
```

**使い方:** `cdn_versions.json` を編集してカスタムCDN URLを追加

**注意:**
- このCDNは非公式であり、Unity Technologies とは無関係です
- 利用は自己責任でお願いします
- Unityからの要請があれば停止する可能性があります

**連絡先:** Unity関係者の方で、このCDNに問題がある場合は achts.y@gmail.com までご連絡ください。

## ライセンス

MIT
