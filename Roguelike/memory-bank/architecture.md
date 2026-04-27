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

## 旅途推进层（2026-04-23，实施计划第8步）
- 目标：把“地图分支”从静态结构升级为可交互流程，形成“选择节点 -> 进入对应场景 -> 完成节点 -> 扣粮并推进”的可验证闭环。
- 关键架构洞察：
  - 采用“进入节点”和“完成节点”双阶段模型，避免将“点击节点”与“推进结算”耦合在同一帧，便于后续接入战斗/事件结算界面。
  - 场景切换由事件驱动（`JourneyNodeEnteredEvent`）而非直接硬编码调用，使流程可替换、可测试、可观测。
  - 断粮阻断抽象为统一消息（`JourneyAdvanceBlockedEvent` + `JourneyAdvanceBlockReason`），UI 与日志不依赖具体业务函数。

- 文件职责（第8步相关）：
  - `Assets/Scripts/Core/GameContext.cs`
    - 旅途推进聚合根：维护当前节点、激活遭遇节点、可选下一节点查询、粮食消耗与阻断校验。
    - 对外暴露 `TryEnterNextJourneyNode`、`TryCompleteActiveJourneyNode`、`GetAvailableNextJourneyNodes` 等接口。
    - 发布第8步推进相关事件，保证调试层与场景路由层解耦。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 定义第8步事件契约：节点进入、节点完成、推进阻断。
    - 定义阻断原因枚举，统一“无粮/路径非法/状态非法”等语义。
  - `Assets/Scripts/Core/JourneyMap.cs`
    - 在原节点列表基础上新增 ID 快速查询能力（`TryGetNode`），支撑当前节点与目标节点合法性判断。
  - `Assets/Scripts/Core/JourneyNodeSceneRouter.cs`
    - 运行时场景路由器：订阅 `JourneyNodeEnteredEvent`，按节点类型映射场景名并尝试切换。
    - 仅负责路由，不负责推进结算，遵循单一职责。
  - `Assets/Scripts/Core/GameContextBootstrap.cs`
    - 启动引导扩展：确保路由器与上下文、调试面板一起自动注入，降低手工挂载负担。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 第8步观测面板：新增推进状态、可选节点、阻断原因显示；字体与换行优化，便于长文本调试。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 第8步验收驱动：提供按钮/热键触发进入节点、完成节点与断粮场景。
    - 通过 `DontDestroyOnLoad` 保持跨场景可操作性，避免切场景后测试入口丢失。

- 生命周期与数据流（第8步）：
  - `GameContext` 初始化并生成地图后，玩家从当前节点读取可选下一节点。
  - 选择下一节点时调用 `TryEnterNextJourneyNode`：
    - 校验地图、路径连通与粮食；失败发布 `JourneyAdvanceBlockedEvent`。
    - 成功记录激活遭遇并发布 `JourneyNodeEnteredEvent`。
  - `JourneyNodeSceneRouter` 接收进入事件后加载对应场景（若已在该场景或未配置则跳过/告警）。
  - 节点内容完成时调用 `TryCompleteActiveJourneyNode`：推进 `JourneyState.NodeIndex/NodesVisited`，扣除粮食，发布 `JourneyNodeCompletedEvent`；若粮食归零再发布阻断事件。

- 边界与后续：
  - 本层不包含危机值递增与灾害强制触发逻辑（实施计划第9步范围）。
  - 当前进入节点后看到蓝色背景属于占位测试场景默认视觉结果，符合第8步“可切场景可推进”的验收目标。

## 第8步验证状态（2026-04-23）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity` + 占位节点场景。
- 验证方法：使用 `GameContextStep8TestDriver` 点击/热键选择分支节点，完成节点结算，并构造断粮场景。
- 验证结果：
  - 点击节点可触发 `JourneyNodeEnteredEvent` 并进入对应场景。
  - 完成节点后可触发 `JourneyNodeCompletedEvent`，`NodeIndex` 推进、`NodesVisited` 增加、粮食按步扣减。
  - 粮食归零后触发 `JourneyAdvanceBlockedEvent(InsufficientFood)`，前进被禁止并显示提示。
  - 验证结论：第8步验收通过。

## 危机与灾害触发层（2026-04-27，实施计划第9步）
- 目标：把“危机值”从只读状态升级为流程驱动量，形成“节点推进累积危机 -> 跨阈值强制触发灾害事件”的可观测闭环。
- 关键架构洞察：
  - 继续沿用事件驱动：危机阈值触发只发布统一消息 `CrisisDisasterTriggeredEvent`，不在 `GameContext` 内直接执行灾害结算，避免与后续事件 UI/结算器耦合。
  - 触发机制采用“基础阈值 + 固定步长”而不是单阈值布尔开关，支持长流程多次触发（如 6、12、18...）并可在 Inspector 调参。
  - 灾害事件选择采用“优先灾害池、缺失时 fallback 普通事件池”的容错策略，保证测试与开发期不会因数据未配齐而中断流程。

- 文件职责（第9步相关）：
  - `Assets/Scripts/Core/GameContext.cs`
    - 新增危机参数：`_crisisGainPerAdvance`、`_disasterTriggerThreshold`、`_disasterTriggerStep`。
    - 在 `TryCompleteActiveJourneyNode` 内接入“每推进一节点增加危机值”。
    - 在 `SetResource(ResourceType.Crisis, ...)` 内统一评估阈值跨越并触发灾害消息。
    - 维护灾害运行时状态：`PendingDisasterEvent`、`PendingDisasterType`、`LastDisasterTriggerMessage`、`NextDisasterTriggerThreshold`。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 新增 `CrisisDisasterTriggeredEvent`，承载危机值、阈值、触发事件、灾害类型与 fallback 标记。
  - `Assets/Scripts/Data/ScriptableObjects/GameDataTypes.cs`
    - 新增 `DisasterEventType` 枚举（`None`、`Plague`、`BanditRaid`、`NaturalDisaster`），统一灾害事件类型语义。
  - `Assets/Scripts/Data/ScriptableObjects/EventConfig.cs`
    - 新增 `IsDisasterEvent` 与 `DisasterType` 字段，使事件资产可被标记为灾害候选。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 新增危机系统摘要区，实时展示危机增长规则、触发阈值、下次触发点与最近一次灾害触发信息。
    - 订阅 `CrisisDisasterTriggeredEvent`，确保灾害触发后 UI 即时刷新。
  - `Assets/Scripts/Core/GameContextStep6TestDriver.cs`
    - 增加第9步验收热键 `V`（将危机值拉到下一个阈值）与 `CrisisDisasterTriggeredEvent` 日志输出。
    - 调整为 `DontDestroyOnLoad` 单例常驻，避免切场景后测试入口丢失。
  - `Assets/Scripts/Core/GameContextStep7TestDriver.cs`
    - 调整为 `DontDestroyOnLoad` 单例常驻，与 Step6 驱动一致，保障 Step8 场景往返时地图调试入口持续可用。
  - `Assets/Data/TestStep4/EventConfig.asset`
    - 测试资产增加灾害标记（`IsDisasterEvent=true`，`DisasterType=Plague`），用于第9步最小可验证样本。

- 生命周期与数据流（第9步）：
  - 完成节点时：`TryCompleteActiveJourneyNode` 扣粮后调用 `AddResource(ResourceType.Crisis, CrisisGainPerAdvance)`。
  - 危机值写入时：`SetResource` 同步更新 `JourneyState.CrisisValue` 并调用 `EvaluateDisasterTrigger`。
  - 阈值跨越时：`GameContext` 选择灾害事件并发布 `CrisisDisasterTriggeredEvent`，同时更新 `PendingDisasterEvent` 与调试消息。
  - 调试观测：`GameContextDebugPanel` 与 Step6 驱动同步接收灾害消息并展示触发结果。

- 边界与后续：
  - 本层只负责“触发与广播”，不包含灾害事件执行器、卡组污染、战斗分流等结算逻辑（实施计划第23步范围）。
  - `PendingDisasterEvent` 当前作为“待处理灾害上下文”，后续可由事件场景控制器消费并在结算后清理。

## 第9步验证状态（2026-04-27）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity` + Step8 占位节点场景。
- 验证方法：
  - 推进节点验证危机值按步增长。
  - 使用 Step6 驱动 `V` 热键将危机值拉高到阈值，观察强制灾害触发日志与调试面板状态。
  - 进入/返回 Step8 节点场景后复测 Step6/Step7 面板与热键可用性。
- 验证结果：
  - 每完成一个节点危机值会增加，满足“每前进一节点增加危机值”。
  - 危机值跨阈值后会发布 `CrisisDisasterTriggeredEvent`，灾害类型正确出现（示例：`Plague`）。
  - Step8 场景往返后 Step6/Step7 调试入口持续可用（常驻对象修正生效）。
  - 验证结论：第9步验收通过。

## 战斗入口层（2026-04-27，实施计划第10步）
- 目标：把“战斗节点”从纯场景路由升级为“节点配置驱动的战斗遭遇入口”，形成“进入 Battle/Boss 节点 -> 准备敌方队列 -> 载入战斗场景 -> 校验队列一致性”的可观测闭环。
- 关键架构洞察：
  - 战斗队列采用“地图生成后预配置 + 进入节点时激活”的双阶段模型，避免在场景切换瞬时随机生成造成不可复现。
  - 遭遇随机性绑定 `map.Seed + nodeId + nodeType`，保证同一 Run 同一节点可重现，便于回放与定位问题。
  - 一致性验证采用“运行时校验器 + 事件日志 + 调试面板”三路观测，降低只靠 UI 观察导致的误判。

- 文件职责（第10步相关）：
  - `Assets/Scripts/Core/GameContext.cs`
    - 新增敌人池装载与注入：`_startingEnemyPool`、`_enemyPool`、`SetEnemyPool`。
    - 新增战斗节点配置仓：`_battleNodeEncounterConfigs`，按地图节点预生成 `BattleEncounterConfig`。
    - 在 `TryEnterNextJourneyNode` 中对 Battle/Boss 节点执行战斗配置存在性校验并激活 `ActiveBattleEncounterConfig`。
    - 发布 `BattleEncounterPreparedEvent`，并在缺配置时发布阻断原因 `MissingBattleEncounterConfig`。
  - `Assets/Scripts/Core/BattleEncounterConfig.cs`
    - 新增战斗入口数据快照：承载节点ID、节点类型、遭遇seed、敌方队列，作为 `GameContext`、调试层、验证器共享契约。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 新增 `BattleEncounterPreparedEvent` 消息体与 `JourneyAdvanceBlockReason.MissingBattleEncounterConfig`。
  - `Assets/Scripts/Core/BattleSceneEntryVerifier.cs`
    - 新增战斗场景入场校验器：监听 `sceneLoaded`，在战斗场景输出“节点配置队列 vs 当前激活队列”的一致性日志。
  - `Assets/Scripts/Core/GameContextBootstrap.cs`
    - 启动引导扩展：自动确保 `BattleSceneEntryVerifier` 随 `GameContext` 注入，避免手工挂载遗漏。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 新增战斗入口摘要区：展示敌人池规模、节点配置队列、激活队列与 `Queue Matches Node Config` 结果。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 接入 `BattleEncounterPreparedEvent` 日志输出，补齐第8步节点交互到第10步战斗入口的桥接观测。
  - `ProjectSettings/EditorBuildSettings.asset`
    - 将 `BattleScene`、`EventScene`、`SupplyScene`、`BossScene` 加入 Build Settings，保证节点路由可真实切场景验证。

- 生命周期与数据流（第10步）：
  - 地图生成阶段：`GameContext.BuildJourneyMap` 后执行 `BuildBattleNodeEncounterConfigs`，为每个 Battle/Boss 节点生成固定敌方队列。
  - 节点进入阶段：`TryEnterNextJourneyNode` 在 Battle/Boss 路径调用 `TryPrepareBattleEncounter`，激活对应配置并发布 `BattleEncounterPreparedEvent`。
  - 场景加载阶段：`JourneyNodeSceneRouter` 执行场景切换；`BattleSceneEntryVerifier` 在场景加载回调中比对节点配置与激活队列并输出一致性结果。
  - 调试观测阶段：`GameContextDebugPanel` 与 Step8 驱动同步刷新，确保场景前后都能看到同一份敌方队列状态。

- 边界与后续：
  - 本层仅实现“战斗入口配置与加载前校验”，不包含第11步回合系统（能量、抽弃牌、行动顺序）和第12步卡牌效果执行。
  - 当前敌方队列为节点入口级别配置，不含战斗中 AI 行动序列与动态增援逻辑（后续步骤扩展）。

## 第10步验证状态（2026-04-27）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity` -> `Assets/Scenes/BattleScene.unity`。
- 验证方法：
  - 在旅途中选择 Battle/Boss 节点，触发 `BattleEncounterPreparedEvent`，确认敌方队列已在进入前生成。
  - 自动切入 `BattleScene` 后观察 `Step10Verifier` 日志中 `nodeConfig` 与 `activeQueue` 对比结果。
  - 对照调试面板 `Battle Entry` 区块，确认 `Queue Matches Node Config` 为 `True`。
- 验证结果：
  - 进入战斗时可稳定输出队列一致性日志，`match=True`。
  - 节点配置队列与激活队列一致，满足“进入战斗时敌人列表与节点配置一致”的验收标准。
  - 验证结论：第10步验收通过。
