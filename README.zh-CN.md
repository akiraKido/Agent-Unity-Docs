# Unity Docs Index Generator

[English](README.md) | [日本語](README.ja.md)

一个让AI编程助手在Unity项目中工作时能够参考官方文档的 Claude Code Skill。

灵感来源于 [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## 安装

### 通过 Unity Package Manager（推荐）

1. 打开 **Window > Package Manager**
2. 点击 **「+」按钮 > Add package from git URL...**
3. 输入：`https://github.com/akiraKido/Agent-Unity-Docs.git`

首次启动 Editor 时，会自动提示安装 Claude Code Skill。

### 手动安装

将 `.claude/skills/unity-docs/` 目录复制到你的 Unity 项目中：

```bash
# 从 Unity 项目根目录
mkdir -p .claude/skills
cp -r <此仓库路径>/.claude/skills/unity-docs .claude/skills/
```

运行安装脚本：

```bash
bash .claude/skills/unity-docs/scripts/setup.sh
```

或手动指定版本：

```bash
bash .claude/skills/unity-docs/scripts/setup.sh 6000.0
```

安装脚本将：
1. 从 `ProjectSettings/ProjectVersion.txt` 自动检测 Unity 版本
2. 如果存在 `.unity-docs/`，则迁移到 Skill 目录（无需重新下载）
3. 如果 `CLAUDE.md` 中存在旧版索引，则将其删除
4. 从 Unity CDN 下载文档
5. 在 `.claude/skills/unity-docs/references/index.txt` 生成紧凑索引

安装完成后，当你询问 Unity 相关问题时，Claude Code 会自动触发此 Skill。也可以使用 `/unity-docs` 手动调用。

## 生成的索引格式

```
[Unity Docs Index]|root: .claude/skills/unity-docs/docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...
```

- 使用管道符 `|` 分隔以节省 token
- 按目录分组文件：`dir:{file1,file2}`
- ScriptReference 因体积过大未纳入索引（但可直接查阅）

## 上下文使用量

生成索引的预估大小：

| 项目 | 值 |
|------|-----|
| 文件大小 | 约 100 KB |
| 预估 token 数 | 约 2.5 万 |
| 上下文占用率 | 约 12%（假设 200K 上下文）|

## 文件结构

```
YourUnityProject/
├── .claude/
│   └── skills/
│       └── unity-docs/
│           ├── SKILL.md            # Skill 定义
│           ├── cdn_versions.json   # CDN 配置
│           ├── scripts/
│           │   └── setup.sh        # 下载和索引脚本
│           ├── docs/               # 下载的文档（gitignored）
│           │   ├── Manual/
│           │   └── ScriptReference/
│           └── references/
│               └── index.txt       # 生成的索引（gitignored）
└── .gitignore
```

## 社区 CDN

如果从 Unity 官方 CDN 下载速度较慢，可以使用以下社区 CDN：

```
https://unity-docs.tsuchigoe.com
```

**使用方法：** 编辑 `cdn_versions.json` 添加自定义 CDN URL

**注意：**
- 此 CDN 是非官方的，与 Unity Technologies 无关
- 使用风险自负
- 如果 Unity 要求，可能会停止服务

**联系方式：** 如果您是 Unity 相关人员并对此 CDN 有疑虑，请联系 achts.y@gmail.com

## 许可证

MIT
