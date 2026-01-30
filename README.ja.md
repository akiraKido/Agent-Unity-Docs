# Unity Docs Index Generator

[English](README.md) | [中文](README.zh-CN.md)

AIコーディングエージェント（Claude、Cursor等）がUnityプロジェクトで作業する際に、公式ドキュメントを参照できるようにするためのツール。

Inspired by [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## インストール

### Unity Package Manager経由 (Git URL)

1. **Window > Package Manager** を開く
2. **「+」ボタン > Add package from git URL...** をクリック
3. 入力: `https://github.com/akiraKido/Agent-Unity-Docs.git`

### manifest.json経由

`Packages/manifest.json` に以下を追加:

```json
{
  "dependencies": {
    "com.tsuchigoe.agent-unity-docs": "https://github.com/akiraKido/Agent-Unity-Docs.git"
  }
}
```

## 使い方

1. Unity Editor で **Tools > Unity Docs Index Generator** を開く
2. Unityバージョンを確認（自動検出）
3. 出力ファイルを指定（デフォルト: `CLAUDE.md`）
4. **Generate Index** をクリック

ドキュメントはUnity CDNから自動的にダウンロードされます。

※ ScriptReferenceはコンテキストサイズの関係で除外されます（Manualのみ）

## 生成されるインデックス形式

```
<!-- UNITY-DOCS-INDEX-START -->[Unity Docs Index]|root: ./.unity-docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...<!-- UNITY-DOCS-INDEX-END -->
```

- パイプ `|` 区切りでトークン節約
- ディレクトリごとにファイルをグループ化: `dir:{file1,file2}`
- マーカーで囲んで更新可能（idempotent）

## コンテキスト使用量

生成されるインデックスのサイズ目安:

| 項目 | 値 |
|------|-----|
| ファイルサイズ | 約 100 KB |
| 推定トークン数 | 約 2.5万 |
| コンテキスト占有率 | 約 12% (200Kコンテキスト想定) |

※ ScriptReference（APIリファレンス）を除外することで、実用的なサイズを実現しています。APIドキュメントは `.unity-docs/ScriptReference/` に保存されており、必要に応じて直接参照できます。

## ファイル構造

生成後:
```
YourUnityProject/
├── CLAUDE.md          # AIエージェント用インデックス
├── .unity-docs/       # ダウンロードされたドキュメント（.gitignoreに追加）
│   ├── Manual/
│   └── ScriptReference/
└── .gitignore         # .unity-docs が自動追加
```

## Community CDN

Unity公式CDNからのダウンロードが遅い場合、以下のコミュニティCDNを利用できます:

```
https://unity-docs.tsuchigoe.com
```

**使い方:** CDN URL フィールドに上記URLを入力

**注意:**
- このCDNは非公式であり、Unity Technologies とは無関係です
- 利用は自己責任でお願いします
- Unityからの要請があれば停止する可能性があります

**連絡先:** Unity関係者の方で、このCDNに問題がある場合は achts.y@gmail.com までご連絡ください。

## ライセンス

MIT
