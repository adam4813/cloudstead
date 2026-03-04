# Copilot Custom Instructions — Unity Game Project

> Place at `.github/copilot-instructions.md` in your Unity project root.
> Customize the Project Context section per project. Architecture rules apply universally.

## Project Context

Unity 6+ (URP) game project. C# 9.0 / .NET Standard 2.1.

**[GAME-SPECIFIC]** Brief game description:
<!-- e.g., "First-person datacenter management sim — build rooms, place equipment, respond to emergencies." -->

## Architecture

### Managers
- All managers extend `Singleton<T> : MonoBehaviour` with an explicit `Initialize()` method.
- A `GameBootstrapper` with `[DefaultExecutionOrder(-100)]` calls `Initialize()` on every manager in deterministic order. Do NOT rely on Awake/Start ordering.
- State mutations go through manager instance methods (never static setters or direct field writes from outside). This enables future networking without rewrite.

### Events
- Static `EventBus` class: `Subscribe<T>(Action<T>)` / `Publish<T>(T)`.
- Events are plain C# structs in `GameEvents.cs`. No UnityEvents for cross-system communication.

### Data
- **ScriptableObjects** for all data definitions. Odin Inspector attributes for editor UX (`[ShowInInspector]`, `[Button]`, `[FoldoutGroup]`, `[Required]`, `[PreviewField]`).
- Runtime MonoBehaviours reference their SO definition and hold mutable state.

### Interfaces
Only where polymorphism is needed:
- `IInteractable` — player interaction targets
- `IBuildable` — grid-placeable items
- `ISaveable` — components contributing save data (`SaveState()` / `RestoreState()`)

### Input
- New Input System only. Actions in `.inputactions` file, organized by map (Player, Building, UI).
- **Never hand-edit the auto-generated `.cs` wrapper** — Unity regenerates it from `.inputactions`.

### Multiplayer Readiness (No Networking Code)
- `uint ownerId` on entities (defaults to 0).
- Player identity threaded through interaction methods.
- Deterministic init via GameBootstrapper.

## Code Style

- `[SerializeField]` for private Inspector fields — never public fields.
- One MonoBehaviour per file, filename matches classname.
- Domain folders: `Core/`, `Player/`, `Grid/`, `Building/`, `[Domain]/`, `UI/`, `SaveLoad/`, `Utilities/`, `Editor/`.
- Assembly definitions: `Game.asmdef` (main, refs: InputSystem, TMP, OdinInspector), `Game.Editor.asmdef` (editor-only, refs: Game).
- No DI containers, no ECS, no abstract base classes unless 3+ concrete implementations exist.

## Conventions

- `Assets/Plugins/` is third-party — never modify.
- Serialized Unity assets are YAML (`ForceText`). Don't manually edit `.asset`/`.unity`/`.prefab`/`.mat` unless you understand Unity YAML.
- `Assets/Data/` holds ScriptableObject instances. `Assets/Prefabs/` holds prefabs. `Tools/` (project root) holds generator scripts.
- Save/load: JSON file persistence via `ISaveable` → `SaveManager`.
- Git: stage specific files (`git add <file>`), never `git add .`. Conventional commits. Include `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` trailer.
- Every Unity asset or script staged for a commit **must include its `.meta` file**. Stage them together: `git add Assets/Path/File.cs Assets/Path/File.cs.meta`. Omitting `.meta` files breaks GUIDs and causes missing-reference errors for other contributors. This applies to `.cs`, `.png`, `.wav`, `.prefab`, `.asset`, `.unity`, `.anim`, `.controller`, `.inputactions`, and any other file Unity tracks in the Project window.
- **Sub-agents must NOT run git write operations** (`git add`, `git commit`, `git push`, `git reset`, `git checkout`, `git stash`, etc.). Only the main/orchestrating agent performs staging and commits. Sub-agents may read git state (`git status`, `git diff`, `git log`, `git show`).
