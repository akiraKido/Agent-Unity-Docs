# Unity Docs Index Generator

[日本語](README.ja.md) | [中文](README.zh-CN.md)

A Claude Code skill that enables AI coding agents to reference official Unity documentation while working on Unity projects.

Inspired by [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## Installation

### Via Unity Package Manager (Recommended)

1. Open **Window > Package Manager**
2. Click **"+" button > Add package from git URL...**
3. Enter: `https://github.com/akiraKido/Agent-Unity-Docs.git`

On first Editor startup, you will be prompted to install the Claude Code skill automatically.

### Manual Installation

Copy the `.claude/skills/unity-docs/` directory into your Unity project:

```bash
# From your Unity project root
mkdir -p .claude/skills
cp -r <path-to-this-repo>/.claude/skills/unity-docs .claude/skills/
```

Then run the setup:

```bash
bash .claude/skills/unity-docs/scripts/setup.sh
```

Or specify a version manually:

```bash
bash .claude/skills/unity-docs/scripts/setup.sh 6000.0
```

The setup will:
1. Auto-detect the Unity version from `ProjectSettings/ProjectVersion.txt`
2. Migrate existing `.unity-docs/` if present (no re-download)
3. Remove legacy index from `CLAUDE.md` if present
4. Download documentation from Unity CDN
5. Generate a compact index at `.claude/skills/unity-docs/references/index.txt`

After setup, Claude Code will automatically trigger this skill when you ask Unity-related questions. You can also invoke it with `/unity-docs`.

## Generated Index Format

```
[Unity Docs Index]|root: .claude/skills/unity-docs/docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...
```

- Pipe `|` delimited for token efficiency
- Files grouped by directory: `dir:{file1,file2}`
- ScriptReference excluded from index due to size (but available for direct lookup)

## Context Usage

Estimated size of generated index:

| Item | Value |
|------|-------|
| File size | ~100 KB |
| Estimated tokens | ~25,000 |
| Context usage | ~12% (assuming 200K context) |

## File Structure

```
YourUnityProject/
├── .claude/
│   └── skills/
│       └── unity-docs/
│           ├── SKILL.md            # Skill definition
│           ├── cdn_versions.json   # CDN configuration
│           ├── scripts/
│           │   └── setup.sh        # Download & index script
│           ├── docs/               # Downloaded docs (gitignored)
│           │   ├── Manual/
│           │   └── ScriptReference/
│           └── references/
│               └── index.txt       # Generated index (gitignored)
└── .gitignore
```

## Community CDN

If downloading from the official Unity CDN is slow, you can use this community CDN:

```
https://unity-docs.tsuchigoe.com
```

**Usage:** Edit `cdn_versions.json` to add the custom CDN URL

**Note:**
- This CDN is unofficial and unrelated to Unity Technologies
- Use at your own risk
- May be discontinued if requested by Unity

**Contact:** If you are a Unity representative and have concerns about this CDN, please contact achts.y@gmail.com

## License

MIT
