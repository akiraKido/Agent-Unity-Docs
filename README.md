# Unity Docs Index Generator

[日本語](README.ja.md) | [中文](README.zh-CN.md)

A tool that enables AI coding agents (Claude, Cursor, etc.) to reference official Unity documentation while working on Unity projects.

Inspired by [agents-md](https://github.com/vercel/next.js/tree/canary/packages/next-codemod/bin) (vercel/next.js)

## Installation

### Via Unity Package Manager (Git URL)

1. Open **Window > Package Manager**
2. Click **"+" button > Add package from git URL...**
3. Enter: `https://github.com/akiraKido/Agent-Unity-Docs.git`

### Via manifest.json

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.tsuchigoe.agent-unity-docs": "https://github.com/akiraKido/Agent-Unity-Docs.git"
  }
}
```

## Usage

1. Open **Tools > Unity Docs Index Generator** in Unity Editor
2. Confirm the Unity version (auto-detected)
3. Specify output file (default: `CLAUDE.md`)
4. Click **Generate Index**

Documentation will be automatically downloaded from Unity CDN.

※ ScriptReference is excluded due to context size limitations (Manual only)

## Generated Index Format

```
<!-- UNITY-DOCS-INDEX-START -->[Unity Docs Index]|root: ./.unity-docs|version: 6000.0|IMPORTANT: Prefer retrieval-led reasoning...|Manual/GameObjects:{GameObjects.html,class-GameObject.html}|...<!-- UNITY-DOCS-INDEX-END -->
```

- Pipe `|` delimited for token efficiency
- Files grouped by directory: `dir:{file1,file2}`
- Wrapped with markers for idempotent updates

## Context Usage

Estimated size of generated index:

| Item | Value |
|------|-------|
| File size | ~100 KB |
| Estimated tokens | ~25,000 |
| Context usage | ~12% (assuming 200K context) |

※ By excluding ScriptReference (API reference), we achieve a practical size. API documentation is saved at `.unity-docs/ScriptReference/` and can be referenced directly when needed.

## File Structure

After generation:
```
YourUnityProject/
├── CLAUDE.md          # Index for AI agents
├── .unity-docs/       # Downloaded documentation (added to .gitignore)
│   ├── Manual/
│   └── ScriptReference/
└── .gitignore         # .unity-docs auto-added
```

## Community CDN

If downloading from the official Unity CDN is slow, you can use this community CDN:

```
https://unity-docs.tsuchigoe.com
```

**Usage:** Enter the URL above in the CDN URL field

**Note:**
- This CDN is unofficial and unrelated to Unity Technologies
- Use at your own risk
- May be discontinued if requested by Unity

**Contact:** If you are a Unity representative and have concerns about this CDN, please contact achts.y@gmail.com

## License

MIT
