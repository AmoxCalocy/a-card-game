## 基础架构现状（2026-04-04）
- 引擎版本：Unity 2022.3.62f2c1（LTS），暂代计划中的 Unity 6 LTS；待后续确认是否升级。
- 项目结构：`Assets/`、`Packages/`、`ProjectSettings/`、`UserSettings/` 已生成；使用 URP 2D 渲染管线。
- 渲染配置：
  - `Assets/Settings/URP-2D-Renderer.asset`：默认 2D Renderer。
  - `Assets/Settings/URP-2D-Pipeline.asset`：绑定默认渲染管线，已在 Graphics/Quality 应用。
  - `Assets/UniversalRenderPipelineGlobalSettings.asset`：URP 全局设置。
- 场景：
  - `Assets/Scenes/SampleScene.unity`：正交相机 + Global Light 2D，作为基线示例场景，已加入 Build Settings 供 CI 构建。
- 包与系统：
  - Input System 已添加，计划后续在 Project Settings 中确认默认使用新输入系统。
  - Addressables 已初始化：`Assets/AddressableAssetsData/`（默认分组、构建脚本、模板）。
  - TextMeshPro、Timeline、Test Framework 已安装（packages-lock 记录）。
- 编辑器与版本控制：
  - 序列化模式：Force Text；版本控制：Visible Meta Files（配合未来 Git/LFS）。
  - 编辑器版本来源：`ProjectSettings/ProjectVersion.txt`，当前记录 `2022.3.62f2c1`（`m_EditorVersionWithRevision: 2022.3.62f2c1 (92e6e6be66dc)`），Unity Hub 以该文件显示项目版本。
  - 版本控制：Git + Git LFS 已启用；分支模型 `main`（稳定）、`dev`（日常集成）、`feature/*`（需求开发）。`.gitattributes` 跟踪主要美术/音视频/3D 资产类型，>50MB 资产默认走 LFS。
- CI/CD：
  - GitHub Actions（`.github/workflows/ci-build.yml`）：使用 game-ci/unity-builder v4，在 `dev`/`main` 的 pull request 及手动触发时构建 `StandaloneWindows64`；默认从 `ProjectVersion.txt` 解析 Unity 版本，也支持 `workflow_dispatch` 输入 `unity_version` 手动覆盖；缓存键包含 `ProjectVersion.txt` 与 `packages-lock.json`。
  - 产物输出到 `build/<targetPlatform>` 并上传 artifact（`roguelike-win-<targetPlatform>`）；日志写入 `Logs/ci-build.log` 并归档上传；默认 opt-in Node.js 24（`FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true`）。
  - 激活策略：优先支持 `UNITY_LICENSING_SERVER`；否则要求 `UNITY_EMAIL` + `UNITY_PASSWORD`，并搭配 `UNITY_LICENSE` 或 `UNITY_SERIAL`。
- 已添加的编辑器脚本：
  - `Assets/Editor/ProjectInitializer.cs`：一次性初始化工具（设置输入系统、创建 URP 2D 资源与示例场景、生成 Addressables 设置、强制文本序列化等）。版本控制模式设置已切换到 `VersionControlSettings.mode`（替代废弃 API `EditorSettings.externalVersionControl`）。后续如升级 Unity 或重建项目，可再次运行 `Tools > OneManJourney > Run Project Init`。

## 数据模板层（2026-04-08，实施计划第4步）
- 新增目录：`Assets/Scripts/Data/ScriptableObjects/`
  - 定位：纯数据定义层（ScriptableObject + 可序列化类型），不承载运行时流程与业务状态。
  - 依赖关系：仅依赖 `UnityEngine` 与基础 C# 集合；后续由 GameContext/事件系统读取。
- 新增文件职责：
  - `Assets/Scripts/Data/ScriptableObjects/GameDataTypes.cs`：统一枚举与资源数值结构（`ResourceAmount`），作为所有 Config 的共享类型中心，避免跨系统重复定义同名枚举。
  - `Assets/Scripts/Data/ScriptableObjects/CardConfig.cs`：卡牌模板（类型、费用、基础数值、状态附加、是否消耗），用于构筑卡池与战斗出牌效果映射。
  - `Assets/Scripts/Data/ScriptableObjects/EnemyConfig.cs`：敌人模板（基础生命/攻防、主意图、击败奖励），用于战斗节点生成敌方单位和掉落结算。
  - `Assets/Scripts/Data/ScriptableObjects/EventConfig.cs`：事件模板主体（事件文案 + 选项列表）；内含 `EventOptionData` 表示多分支解法（战斗/检定/支付/牺牲）及其资源与招募结果。
  - `Assets/Scripts/Data/ScriptableObjects/CompanionConfig.cs`：伙伴模板（定位、生命、忠诚、检定加值、特质 ID、初始专属卡引用），用于招募与队伍编制。
  - `Assets/Scripts/Data/ScriptableObjects/RelicConfig.cs`：遗物模板（触发时机、修饰器类型、强度、资源/状态作用域、是否一次性），用于全局被动与战斗触发效果。
  - `Assets/Scripts/Data/ScriptableObjects/ResourceTableConfig.cs`：资源表模板（开局资源、危机初值、章节掉落衰减），作为全局经济与进程参数入口。
- 资源创建入口：
  - 以上 6 类模板均通过 `CreateAssetMenu` 暴露在 `OneManJourney/Data/*` 菜单下，可直接由策划在 Inspector 创建资产并序列化入库。
- 当前边界（重要）：
  - 本层不包含校验器、运行时执行器、示例资产与 UI 绑定逻辑；后续里程碑按“数据定义层 → 读取层（GameContext）→ 事件流/UI”逐步接入。

## 第4步验证状态（2026-04-08）
- 验证环境：Unity 2022.3.62f2c1。
- 验证结果：6 类 ScriptableObject 模板均可在 `OneManJourney/Data/*` 菜单创建；字段可编辑并持久化；`CompanionConfig.StarterCards` 与 `EventOptionData.RecruitedCompanion` 交叉引用正常；重启后无序列化报错。
