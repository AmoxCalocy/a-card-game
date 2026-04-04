# Repository Guidelines

## Project Structure & Module Organization
- Current files: `game-design-document.md` (design scope) and `tech-stack.md` (Unity/URP plan). Keep design notes at root until a `docs/` folder is added.
- When the Unity project lands, expect `Assets/`, `Packages/`, and `ProjectSettings/`; place gameplay scripts in `Assets/Scripts/`, data configs in `Assets/Data/ScriptableObjects/`, tests in `Assets/Tests/{EditMode,PlayMode}/`, and Addressables content in `Assets/Addressables/`.
- Git LFS is enabled (.gitattributes covers major art/audio/video/3D asset types; >50MB assets should route to LFS). Keep meta files committed alongside assets.

## Build, Test, and Development Commands
- Open with Unity 6 LTS (URP 2D Renderer). Use asset serialization `Force Text` and version control `Visible Meta Files`.
- Headless build example (adjust method name/target as needed):
```powershell
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScripts.BuildWindows -buildTarget StandaloneWindows64 -logFile logs/build.log
```
- Tests (split by platform for clearer logs):
```powershell
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform editmode -logFile logs/tests-edit.log
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform playmode -logFile logs/tests-play.log
```
- Run `addressables build` (via build script) before producing deliverables.

## Coding Style & Naming Conventions
- C# 10, 4-space indent, UTF-8, trailing newline. Favor composition over inheritance in MonoBehaviours.
- Naming: classes/methods PascalCase; private fields `_camelCase`; constants PascalCase; ScriptableObjects end with `Config`; editor-only scripts sit under `Assets/Editor/`.
- Use `[SerializeField] private` for inspector wiring; prefer `nameof` for UnityEvents and DOTween for small motion cues noted in `tech-stack.md`.

## Testing Guidelines
- EditMode tests for pure logic; PlayMode for integration. Store under `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`.
- File naming: `FeatureNameTests.cs`; test names `Method_Scenario_Expected`. Aim for 70%+ coverage on combat/math utilities; document flakiness with issue links.

## Commit & Pull Request Guidelines
- Branching: `main` = release-ready; `dev` = default integration; feature branches use `feature/<short-task>`, open PRs into `dev`, then promote `dev` → `main` when stable.
- Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:`). Subjects ≤72 chars, body explains behavior impact and test scope.
- PR checklist: summary + why, linked issue/design section, build/test command output, screenshots or short clips for UI/gameplay, explicit note when scenes or Addressables change.

## Security & Configuration
- Do not commit keys or service tokens; keep them in Unity Cloud or local user secrets ignored by VCS.
- Prefer Addressables over `Resources/` for remote-friendly assets; size-heavy binaries should go to Git LFS once enabled.

重要提示：

写任何代码前必须完整阅读 memory-bank/@architecture.md（包含完整数据库结构）

写任何代码前必须完整阅读 memory-bank/@game-design-document.md

每完成一个重大功能或里程碑后，必须更新 memory-bank/@architecture.md
