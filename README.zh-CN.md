# Unity Docs Index Generator

[English](README.md) | [日本語](README.ja.md)

一个让AI编程助手（Claude、Cursor等）在Unity项目中工作时能够参考官方文档的工具。

灵感来源于 [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## 安装

### 通过 Unity Package Manager (Git URL)

1. 打开 **Window > Package Manager**
2. 点击 **「+」按钮 > Add package from git URL...**
3. 输入：`https://github.com/akiraKido/Agent-Unity-Docs.git`

### 通过 manifest.json

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.tsuchigoe.agent-unity-docs": "https://github.com/akiraKido/Agent-Unity-Docs.git"
  }
}
```

## 使用方法

1. 在 Unity Editor 中打开 **Tools > Unity Docs Index Generator**
2. 确认 Unity 版本（自动检测）
3. 指定输出文件（默认：`CLAUDE.md`）
4. 点击 **Generate Index**

文档将从 Unity CDN 自动下载。

※ 由于上下文大小限制，ScriptReference 被排除（仅包含 Manual）

## 生成的索引格式

```
<!-- UNITY-DOCS-INDEX-START -->[Unity Docs Index]|root: ./.unity-docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...<!-- UNITY-DOCS-INDEX-END -->
```

- 使用管道符 `|` 分隔以节省 token
- 按目录分组文件：`dir:{file1,file2}`
- 使用标记包裹以支持幂等更新

## 上下文使用量

生成索引的预估大小：

| 项目 | 值 |
|------|-----|
| 文件大小 | 约 100 KB |
| 预估 token 数 | 约 2.5 万 |
| 上下文占用率 | 约 12%（假设 200K 上下文）|

※ 通过排除 ScriptReference（API 参考），实现了实用的大小。API 文档保存在 `.unity-docs/ScriptReference/`，需要时可以直接参考。

## 文件结构

生成后：
```
YourUnityProject/
├── CLAUDE.md          # AI 助手用索引
├── .unity-docs/       # 下载的文档（已添加到 .gitignore）
│   ├── Manual/
│   └── ScriptReference/
└── .gitignore         # 自动添加 .unity-docs
```

## 社区 CDN

如果从 Unity 官方 CDN 下载速度较慢，可以使用以下社区 CDN：

```
https://unity-docs.tsuchigoe.com
```

**使用方法：** 在 CDN URL 字段中输入上述 URL

**注意：**
- 此 CDN 是非官方的，与 Unity Technologies 无关
- 使用风险自负
- 如果 Unity 要求，可能会停止服务

**联系方式：** 如果您是 Unity 相关人员并对此 CDN 有疑虑，请联系 achts.y@gmail.com

## 许可证

MIT
