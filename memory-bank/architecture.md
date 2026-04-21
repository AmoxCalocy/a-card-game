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

## 运行时上下文层（2026-04-19，实施计划第5步）
- 新增目录与文件：`Assets/Scripts/Core/`
  - `GameContext.cs`：全局上下文聚合根，管理资源、卡池、事件池、旅途状态；统一暴露资源与进度读写接口，并通过 `Initialized`/`StateChanged` 事件对外广播状态变化。
  - `GameServices.cs`：轻量服务定位器，用于注册与解析运行时单例服务（当前用于 `GameContext` 解析）。
  - `JourneyState.cs`：旅途进度数据模型（章节、节点索引、访问节点数、RunSeed、危机值），并提供默认值工厂。
  - `GameContextBootstrap.cs`：`RuntimeInitializeOnLoadMethod` 启动引导，确保场景加载后存在 `GameContext` 与 `GameContextDebugPanel`。
  - `GameContextDebugPanel.cs`：调试 UI（ScreenSpaceOverlay + TMP），实时展示资源、卡池、事件池和旅途状态，作为第5步验收可视化入口。
- 生命周期与数据流：
  - 应用启动后由 Bootstrap 检测或创建 `GameContextRoot`，`GameContext.Awake()` 中完成单例约束、`DontDestroyOnLoad`、服务注册和初始化。
  - 初始化阶段按顺序执行：加载默认资产（Editor 可自动扫描 `Assets/Data`）→构建资源字典→构建卡池/事件池→创建默认 `JourneyState`。
  - 资源写入规则：除 `ResourceType.Crisis` 外，资源不会被写成负数；`Crisis` 变化会同步到 `JourneyState.CrisisValue`。
- 依赖与边界：
  - 该层消费第4步 ScriptableObject 数据模板，不包含业务事件总线（计划第6步）与战斗/地图流程逻辑。
  - 调试面板仅用于开发期观测，不参与正式 HUD（计划第7步）。

## 第5步验证状态（2026-04-19）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity`。
- 验证方法：进入 Play 后检查运行时生成对象与 TMP 调试文本。
- 验证结果：
  - `GameContextRoot` 成功存在并挂载 `GameContext` + `GameContextDebugPanel`。
  - `GameContextDebugPanel` 的 `ContextText` 内容包含 `GameContext Debug`、`Resources`、卡池/事件池计数与旅途状态字段。
  - 资源显示与 `Assets/Data/TestStep4/ResourceTableConfig.asset` 初始值一致（Food/Wealth/Reputation/MedicalSupplies/Crisis），满足“进场景后可读取并显示初始卡池与资源数值”的第5步验收标准。

## 事件流层（2026-04-19，实施计划第6步）
- 新增文件：
  - `Assets/Scripts/Core/GameEventBus.cs`：轻量泛型事件总线，支持 `Subscribe<T>`、`Publish<T>`、`Unsubscribe<T>`，订阅返回 `IDisposable`，便于生命周期解绑。
  - `Assets/Scripts/Core/GameEventMessages.cs`：统一消息类型定义，当前包含：
    - `GameContextInitializedEvent`
    - `ResourceChangedEvent`
    - `CardDrawnEvent`
    - `NodeSelectedEvent`
- `GameContext` 接入事件发布：
  - 在 `Awake` 创建并注册 `GameEventBus`（通过 `GameServices` 可解析）。
  - 资源变化通过 `SetResource` 发布 `ResourceChangedEvent`（含前值/现值/Delta）。
  - 节点进度变化通过 `SetJourneyProgress`/`AdvanceNode` 发布 `NodeSelectedEvent`。
  - 初始化完成发布 `GameContextInitializedEvent`。
  - 新增 `TryDrawCard(out CardConfig)`，抽卡时发布 `CardDrawnEvent`。
- `GameContextDebugPanel` 接入事件订阅：
  - 通过 `GameServices` 解析事件总线并订阅上述消息，消息到达时刷新面板。
  - 在 `OnDisable`/重绑定时释放订阅，避免悬挂回调和空引用。

## 第6步验证状态（2026-04-19）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity`。
- 验证方法：
  - 通过 `GameContextStep6TestDriver` 触发资源变更、危机值变更、节点推进、抽卡。
  - 观察 `GameContextDebugPanel` 是否实时刷新，以及 Console 事件日志是否输出。
- 验证结果：
  - 抽卡日志输出正常（例如：`Step6TestDriver: drew card New Card (card.id).`）。
  - 资源/节点/卡池变更链路可用，调试面板随事件实时更新。
  - 验证结论：第6步验收通过。

## 世界地图层（2026-04-19，实施计划第7步）
- 新增地图模型与生成器：
  - `Assets/Scripts/Core/JourneyMap.cs`
    - 定义 `JourneyNodeType`（`Battle`/`Event`/`Supply`/`Boss`）。
    - 定义 `JourneyMapGenerationConfig`（层数、每层节点数、战斗/事件/补给权重）。
    - 定义运行时地图结构 `JourneyMap`/`JourneyMapNode`（节点列表、路线数、分支节点数、类型统计）。
  - `Assets/Scripts/Core/JourneyMapGenerator.cs`
    - 生成固定 DAG 拓扑：起点层 -> 中间内容层 -> 首领层。
    - 默认配置下节点总数为 `1 + (LayerCount - 2) * LanesPerLayer + 1`，满足 10+ 节点目标（默认 14）。
    - 起点至少连接 2 个下一层节点，并在中间层引入横向分叉，保证至少 2 条路线。
    - 非首领节点按权重分配为战斗/事件/补给，最后一层固定首领节点。
    - 生成后计算 `RouteCount` 与 `BranchingNodeCount` 供验收使用。
- `GameContext` 接入地图生命周期：
  - 初始化时基于 `JourneyState.RunSeed` 生成地图。
  - 新增 `RegenerateJourneyMap()`/`RegenerateJourneyMap(int seed)`，可在运行时重建地图并更新 `RunSeed`。
  - 新增属性 `JourneyMap` 暴露当前地图快照。
- 事件系统扩展：
  - `Assets/Scripts/Core/GameEventMessages.cs` 新增 `JourneyMapGeneratedEvent`（包含节点数、路线数、分支节点数与类型计数）。
  - `GameContext` 在初始化与地图重建时发布 `JourneyMapGeneratedEvent`。
- 调试可视化：
  - `GameContextDebugPanel` 新增地图摘要区，显示节点总数、路线数、分支节点数、各类型数量。
  - 新增 `Assets/Scripts/Core/GameContextStep7TestDriver.cs`，支持运行时热键触发重建与完整节点日志输出。

## 第7步验证状态（2026-04-19）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity`。
- 验证方法：
  - 运行 `GameContextStep7TestDriver`，通过热键触发地图重生成事件。
  - 观察 Console 中 `JourneyMapGeneratedEvent` 日志，以及调试面板地图摘要。
- 验证结果：
  - 日志示例：`Step7TestDriver Event: MapGenerated seed=1668988292, nodes=14, routes=16, branchingNodes=10, battle/event/supply/boss=7/3/3/1.`
  - 节点数满足 `>=10`，且路线数 `>=2`，存在有效分支节点。
  - 验证结论：第7步验收通过。
