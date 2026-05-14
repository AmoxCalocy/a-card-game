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

## 回合流程层（2026-04-30，实施计划第11步）
- 目标：将战斗入口扩展为可运行的回合循环，形成“玩家回合 -> 出牌 -> 弃牌 -> 敌方阶段 -> 下一回合抽牌”的闭环，并保证能量与牌堆计数可观测、可验证。
- 关键架构洞察：
  - 回合状态集中在单点控制器（`BattleTurnController`），避免 UI、输入驱动、上下文层各自维护一份回合状态导致分叉。
  - 抽牌系统采用“抽牌堆空时回洗弃牌堆”的策略，并且每次抽牌前做边界判定，保证不发生下标越界。
  - 回合推进通过事件总线发布，不把验证逻辑硬编码在控制器里，便于后续替换 UI、接入自动化测试与录像回放。
- 文件职责（第11步相关）：
  - `Assets/Scripts/Core/BattleTurnController.cs`
    - 第11步核心控制器，负责战斗回合生命周期、能量重置、抽牌/弃牌/消耗与回洗、回合阶段切换。
    - 在进入 `Battle/Boss` 节点时基于 `GameContext.ActiveBattleEncounterConfig` 初始化战斗流；在节点完成或离开战斗节点时结束战斗流。
    - 对外暴露只读运行时状态（phase/turn/energy/draw/hand/discard/exhaust）供调试面板和测试驱动读取。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 扩展第11步事件契约与 `BattleTurnPhase` 枚举，作为回合层与 UI/测试层之间的稳定接口。
  - `Assets/Scripts/Core/GameContextBootstrap.cs`
    - 启动引导扩展，自动确保 `BattleTurnController` 注入，避免场景切换后因漏挂组件导致回合系统失活。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 第11步可视化观察层，展示回合阶段、能量、牌堆计数与手牌摘要，用于快速人工验收与问题定位。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 第11步手工回归入口，新增 `P`/`E` 热键与回合事件日志，支持“打牌 -> 结束回合 -> 下一回合”连续验证。
- 生命周期与数据流（第11步）：
  - 进入战斗节点后：`GameContext` 准备遭遇配置 -> `BattleTurnController` 初始化牌堆并发布 `BattleFlowInitializedEvent`。
  - 开始玩家回合：控制器重置能量并抽牌，发布 `BattleCardsDrawnEvent` 与 `BattleTurnStartedEvent`。
  - 出牌与结束回合：出牌发布 `BattleCardPlayedEvent`；结束回合后依次发布 `BattleHandDiscardedEvent`、`BattleEnemyTurnResolvedEvent`，随后开启下一玩家回合。
  - 结束战斗流：发布 `BattleFlowEndedEvent` 并清空运行时状态。
- 边界与后续：
  - 本层仅负责回合流程骨架与牌堆/能量流转，不负责具体卡牌效果执行（第12步范围）。
  - 当前敌方阶段为“流程占位 + 事件发布”，敌方意图与具体行动规则在后续步骤实现。

## 卡牌效果框架层（2026-05-11，实施计划第12步）
- 目标：将第11步“可出牌但仅消耗资源”的回合骨架扩展为“按牌类型执行可观测效果”的战斗能力，形成“出牌 -> 执行效果 -> 状态变化 -> 事件广播”的闭环，并支持状态叠层验证。
- 关键架构洞察：
  - 将“单位状态”从控制器临时变量抽离到独立模型 `BattleCombatantState`，把生命/护甲/状态叠层逻辑集中，避免后续敌方 AI、遗物、DOT 结算重复实现。
  - 出牌事件继续沿用事件总线，但载荷升级为“意图 + 结果”双信息结构（卡牌原始参数 + 实际效果数值），使 UI、日志、自动化验证不需窥探控制器内部状态。
  - 牌类型映射先采用稳定规则（Attack/Defense/Strategy/Logistics/Tactic），保持可迭代的最小实现：先保证语义可验，再逐步扩展复杂特效脚本化。

- 文件职责（第12步相关）：
  - `Assets/Scripts/Core/BattleCombatantState.cs`
    - 运行时单位状态对象（玩家/敌人共用）：管理 `CurrentHealth`、`Armor`、状态栈字典与 `ApplyDamage`/`AddArmor`/`Heal`/`AddStatus`。
    - 提供 `GetStatusSummary()` 作为调试输出的统一来源，减少面板/日志重复拼接状态字符串。
  - `Assets/Scripts/Core/BattleTurnController.cs`
    - 新增 `_playerState`、`_enemyStates` 与初始化逻辑，在进入战斗节点后构建战斗快照。
    - 在 `TryPlayCard` 中调用 `ExecuteCardEffect` 执行 5 类卡效：
      - `Attack`：对首个存活敌人造成伤害并附加状态。
      - `Defense`：为玩家增加护甲并附加状态。
      - `Strategy`：额外抽牌，并可附加玩家状态。
      - `Logistics`：为玩家治疗并附加状态。
      - `Tactic`：对首个目标造成伤害，并对所有存活敌人叠加状态。
    - 持有 `LastCardEffectSummary`，作为调试面板“最近一次效果”文本源。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 扩展 `BattleCardPlayedEvent`：新增 `CardType`、`CardBaseValue`、`StatusEffect`、`RequestedStatusStacks` 与实际结果字段（伤害/护甲/治疗/抽牌/状态叠层/摘要）。
    - 作用：让外部观测层可直接判定“描述是否生效”，无需访问控制器私有字段。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 新增战斗效果观测区：玩家 HP/护甲/状态、敌方逐单位状态、最后一次卡效摘要。
    - 作用：第12步手工验收主界面，便于快速核对“数值变化 + 状态叠层”。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 扩展出牌日志，输出卡效结果明细（dmg/armor/heal/draw/status + summary）。
    - 作用：与面板互补，提供可复制的 Console 验收证据。
  - `Assets/Data/TestStep4/CardConfig.asset`
    - Step12 攻击示例牌（破甲斩）：用于验证伤害 + 流血叠层。
  - `Assets/Data/TestStep4/CardConfig_Defense.asset`
    - Step12 防御示例牌（坚守阵线）：用于验证护甲 + 士气叠层。
  - `Assets/Data/TestStep4/CardConfig_Strategy.asset`
    - Step12 策略示例牌（侦察预案）：用于验证额外抽牌。
  - `Assets/Data/TestStep4/CardConfig_Logistics.asset`
    - Step12 后勤示例牌（战地医护）：用于验证治疗 + 士气叠层。
  - `Assets/Data/TestStep4/CardConfig_Tactic.asset`
    - Step12 战术示例牌（包围网）：用于验证单体伤害 + 群体围捕叠层。

- 生命周期与数据流（第12步）：
  - 进入 Battle/Boss 节点后，`BattleTurnController` 初始化玩家与敌方状态。
  - 玩家出牌时：
    - 先完成能量消耗与手牌迁移（弃牌/消耗堆）。
    - 再执行 `ExecuteCardEffect` 写入单位状态（HP/Armor/Status）与抽牌变化。
    - 最后发布扩展后的 `BattleCardPlayedEvent`，调试面板与测试驱动即时刷新。
  - 回合结束与下回合开始保持第11步流程不变（弃牌 -> 敌方阶段占位 -> 回合开始抽牌）。

- 边界与后续：
  - 本层仅实现“卡牌类型 -> 基础效果映射”，未引入卡牌脚本化 DSL、触发连锁、遗物联动与持续伤害结算。
  - 第13步敌人 AI 意图系统尚未开始；当前敌方仍为回合占位与状态承载。

## 第12步验证状态（2026-05-11）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity` + `BattleScene.unity`。
- 验证方法：通过 `GameContextStep8TestDriver` 进入战斗节点，循环出牌并观察 `GameContextDebugPanel` 与 Console 事件日志。
- 验证结果：
  - 5 张示例牌均按描述生效（攻击、防御、策略抽牌、后勤治疗、战术群体状态）。
  - 状态可叠层（示例：`Bleed` 可连续增长）。
  - `BattleCardPlayedEvent` 明细与面板状态一致，可用于后续自动化断言。
- 验证结论：第12步验收通过；按约束未开始第13步。

## 敌人意图与执行层（2026-05-13，实施计划第13步）
- 目标：把敌方阶段从“占位流程”升级为“可预览意图 + 可执行行动”的闭环，形成“回合开始发布下回合意图 -> 玩家决策 -> 敌方按意图执行 -> 输出执行结果摘要”的可观测链路。
- 关键架构洞察：
  - 引入“计划值（planned）”与“实际生效值（effective）”双层语义：
    - planned：`BattleEnemyIntentUpdatedEvent` 中的 `IntentType + IntentValue`，用于 UI 预览。
    - effective：`BattleEnemyTurnResolvedEvent` 中的伤害/护甲/掠夺结果，受护甲吸收、资源上限、单位存活等运行时条件影响。
  - 验收与调试必须按同一 `turn` 对齐比较“类型一致性”和“结果合理性（effective <= planned）”，而不是要求数值逐项恒等。
  - 保持 `BattleTurnController` 单点控制：意图生成与执行均在同一控制器，避免 UI 层和控制层维护两套敌方行为状态导致漂移。

- 文件职责（第13步相关）：
  - `Assets/Scripts/Core/BattleTurnController.cs`
    - 新增意图计划缓存：`_enemyIntents`，并在 `BeginPlayerTurn` 中调用 `RebuildEnemyIntentPlan()`。
    - 新增敌方执行链路：`ResolveEnemyTurn()`，按意图执行 `Attack/Defend/Plunder`。
    - 新增资源掠夺策略：`TryPlunderResources()`（优先 `Wealth`，不足补扣 `Food`）。
    - 新增观测字段：`EnemyIntents`、`LastEnemyTurnSummary`。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 新增 `BattleEnemyIntentView`（敌方意图快照结构）。
    - 新增 `BattleEnemyIntentUpdatedEvent`（按回合广播意图列表）。
    - 扩展 `BattleEnemyTurnResolvedEvent`，承载敌方回合实际执行聚合结果与摘要。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 新增调试展示：`Next Enemy Intents`、`Last Enemy Turn`。
    - 订阅 `BattleEnemyIntentUpdatedEvent`，确保回合意图刷新时面板即时同步。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 新增第13步日志观测入口：输出意图刷新事件和敌方回合执行结果，供人工验收与回归记录。

- 生命周期与数据流（第13步）：
  - 玩家回合开始：`BattleTurnController.BeginPlayerTurn()` 先生成下一次敌方行动计划并发布 `BattleEnemyIntentUpdatedEvent`，再执行玩家抽牌/能量重置。
  - 玩家结束回合：`TryEndPlayerTurn()` 将阶段切换到 `EnemyTurn`，调用 `ResolveEnemyTurn()` 执行已发布的意图计划。
  - 敌方执行后：发布扩展后的 `BattleEnemyTurnResolvedEvent`（dmg/armor/plunder + summary），随后开启下一玩家回合并刷新新一轮意图。

- 边界与后续：
  - 本层仅实现基础三意图（攻击/防御/掠夺）及可读提示，不包含复杂技能脚本、多段连携、遗物联动和敌方战术树。
  - 第14步战斗结算（胜负奖励/惩罚、伙伴受伤）仍未开始，保持阶段边界清晰。

## 第13步验证状态（2026-05-13）
- 验证环境：Unity 2022.3.62f2c1，`Assets/Scenes/SampleScene.unity` + `BattleScene.unity`。
- 验证方法：通过 `GameContextStep8TestDriver` 持续结束玩家回合，观察 `EnemyIntentUpdated` 与 `EnemyTurnResolved` 的同回合对齐关系，以及调试面板实时刷新。
- 验证结果：
  - 回合编号持续增长且意图事件持续刷新。
  - `EnemyTurnResolved` 与对应回合意图在“行动类型”上保持一致。
  - 观察到 planned 与 effective 数值可不同（如伤害被护甲吸收、掠夺受资源余量限制），符合第13步语义定义。
  - 验证结论：第13步验收通过。

## 战斗结算层（2026-05-14，实施计划第14步）
- 目标：将战斗从"无限循环"升级为"可结束流程"，形成"检测胜负 -> 执行结算 -> 发布事件 -> 结束战斗"的闭环。
- 关键架构洞察：
  - 结算在 `EndBattleFlow` 清空状态前发布 `BattleSettledEvent`，确保调试面板/测试驱动在状态仍存在时即可观测结算结果。
  - 胜利检测点有两处：打出卡牌后（可能一击杀死最后敌人）和结束回合后（敌方阶段结束重新检测）。立即检测避免玩家需要手动"结束空回合"。
  - 失败检测仅在敌方回合结算后，因为敌方只能在其回合内造成伤害。
  - 失败惩罚的卡牌丢弃目标为 `GameContext.CardPool`（持久卡池），而非临时战斗牌堆（`_drawPile/_hand` 等会在 `EndBattleFlow` 中被清空），确保损失具有持久性。
- 文件职责（第14步相关）：
  - `Assets/Scripts/Core/BattleTurnController.cs`
    - 第14步结算聚合点：`CheckBattleOutcome()` 统一检测胜负，`ResolveVictory()` 汇总敌人物品掉落并发放，`ResolveDefeat()` 扣除资源与卡牌。
    - 新增可配置失败惩罚参数：财富损失比例、粮食损失比例、弃牌数量。
    - 保持 `EndBattleFlow` 为单一清场出口，结算在清空前完成资源写入与事件发布。
  - `Assets/Scripts/Core/GameEventMessages.cs`
    - 新增 `BattleSettledEvent`：承载胜负结果、奖励列表、损失列表、弃牌计数、伙伴受伤占位、结算摘要，作为战斗结束的统一可观测契约。
  - `Assets/Scripts/Core/GameContext.cs`
    - 新增 `TryRemoveRandomCard()`：从卡池随机移除一张卡牌，供失败弃牌惩罚调用，与 `TryDrawCard`（从头部移除）互补。
  - `Assets/Scripts/Core/GameContextDebugPanel.cs`
    - 新增结算展示区（Last Settlement）：显示结局（VICTORY/DEFEAT）、摘要、奖励明细、损失明细、弃牌数。通过 `_lastBattleSettledEvent` 缓存实现战斗结束后持续可见。
  - `Assets/Scripts/Core/GameContextStep8TestDriver.cs`
    - 新增 `BattleSettledEvent` 订阅与 `FormatResourceAmounts` 格式化辅助，输出结算详情日志供人工回归。
  - `Assets/Data/TestStep4/EnemyConfig.asset`
    - 更新测试敌人战利品（+10 Food、+5 Wealth），确保第14步奖励路径有可观测数据。
- 生命周期与数据流（第14步）：
  - 胜利流程：玩家出牌 `TryPlayCard` → `ExecuteCardEffect` 击杀最后敌人 → `CheckBattleOutcome()` → `AllEnemiesDefeated()` 返回 true → `ResolveVictory()` 汇总 `EnemyConfig.DefeatRewards` → `AddResource` 发放 → `Publish(BattleSettledEvent)` → `EndBattleFlow("Victory")`。
  - 失败流程：玩家结束回合 `TryEndPlayerTurn` → `ResolveEnemyTurn` 敌方攻击致死 → `CheckBattleOutcome()` → `IsPlayerDefeated()` 返回 true → `ResolveDefeat()` 扣除财富/食物百分比 + 随机弃牌 → `Publish(BattleSettledEvent)` → `EndBattleFlow("Defeat")`。
  - 外部节点完成（JourneyNodeCompletedEvent）仍调用 `EndBattleFlow("Journey node completed.")` 作为第三出口，不触发结算。
- 边界与后续：
  - 本层仅实现胜负二元结算与资源/卡牌奖惩，`CompanionInjured` 硬编码为 `false` 作为伙伴系统的占位钩子。
  - 第15步伙伴招募/编制系统将消费该钩子，补充伙伴受伤/离队逻辑。
  - 当前未实现"部分奖励"（战败时给予已击杀敌人的战利品），设计上保持二元清晰。
