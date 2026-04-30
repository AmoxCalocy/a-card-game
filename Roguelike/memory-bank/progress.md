2026-04-02：完成实施计划第1步（项目基线）
- 使用 Unity 2022.3.62f2c1 在仓库根创建了 URP 2D 项目（含 Assets/Packages/ProjectSettings 等）。
- 通过命令执行初始化脚本，安装并配置了 Addressables、Input System、TextMeshPro、URP、Timeline、Test Framework 等核心包。
- 生成 URP 2D 渲染资源（Assets/Settings/URP-2D-Pipeline.asset、URP-2D-Renderer.asset）并绑定到 Graphics/Quality，创建正交相机和 Global Light 2D 的 SampleScene.unity。
- 创建 Addressables 默认配置（Assets/AddressableAssetsData/*），项目序列化改为 Force Text、版本控制改为 Visible Meta Files。

2026-04-04：完成实施计划第2步（Git + Git LFS + 分支策略）
- 运行 `git lfs install` 并扩展 `.gitattributes` 覆盖主要美术/音视频/3D 资产类型，>50MB 资产默认走 LFS。
- 更新 AGENTS 说明，确认分支模型：`main`（稳定）、`dev`（日常集成）、`feature/*`（需求）；Git LFS 规则已启用。
- 在主分支完成配置提交以验证 LFS/分支设置可用，后续开发将基于 `dev` 分支。

2026-04-04：完成实施计划第3步（CI：GitHub Actions + Unity Builder 打包 Windows）
- 新增工作流 `.github/workflows/ci-build.yml`，使用 `game-ci/unity-builder@v4` 固定 Unity 版本 2022.3.62f2c1，目标 `StandaloneWindows64`，在 `dev/main` 的 pull request 以及手动触发时构建。
- 构建输出目录 `build/StandaloneWindows64`，日志写入 `Logs/ci-build.log`，两者作为 artifact 上传；并缓存 `Library` 缩短重复构建耗时。
- 运行前需在仓库 Secrets 配置 `UNITY_LICENSE`（序列化 license 内容）、`UNITY_EMAIL`、`UNITY_PASSWORD`；缺失时工作流会立即报错提示。工作流已默认使用 Node.js 24（`FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true`）以规避 Node 20 弃用警告；自定义参数仅保留 `-logFile Logs/ci-build.log`，避免与 Unity Builder 默认的 `-batchmode` 重复。
- 为修复“Cannot build untitled scene”，已将 `Assets/Scenes/SampleScene.unity` 加入 Build Settings（`ProjectSettings/EditorBuildSettings.asset`）。

2026-04-08：完成实施计划第4步（ScriptableObject 数据模板）
- 新增数据模板目录 `Assets/Scripts/Data/ScriptableObjects/`，并提交对应 `.meta` 文件，保证 Unity 资源 GUID 稳定可追踪。
- 新增 6 类 ScriptableObject 模板：`CardConfig`、`EnemyConfig`、`EventConfig`、`CompanionConfig`、`RelicConfig`、`ResourceTableConfig`，均带 `CreateAssetMenu`，可直接在 Inspector 创建。
- 新增共享类型文件 `GameDataTypes`，统一定义卡牌类型、稀有度、资源类型、状态类型、事件解法、伙伴定位、遗物触发/修饰器等枚举以及 `ResourceAmount` 结构，减少后续系统间类型分歧。
- `EventConfig` 内加入可序列化 `EventOptionData`，支持多选项事件的基础字段（解法类型、成功率、资源成本/奖励、声望门槛、牺牲卡数量、可招募伙伴引用）。
- 该里程碑仅实现“最小可用模板”，未引入运行时逻辑、编辑器校验器或示例资产；后续由第5步 GameContext 和第6步事件流接入消费。

2026-04-08：第4步验证与工程修正
- 第4步人工验证通过：6类 ScriptableObject 资产可创建、可编辑、可交叉引用，重启 Unity 后序列化数据保持，Console 无序列化报错。
- Unity 项目版本文件 `ProjectSettings/ProjectVersion.txt` 更新为 `2022.3.62f2c1`，与本地 Unity Hub 显示版本一致。
- CI 工作流 `.github/workflows/ci-build.yml` 增强：默认从 `ProjectVersion.txt` 读取 Unity 版本，支持 `workflow_dispatch` 手动覆盖版本；激活策略校验支持 `UNITY_EMAIL` + `UNITY_PASSWORD` 搭配 `UNITY_LICENSE` 或 `UNITY_SERIAL`，并兼容 `UNITY_LICENSING_SERVER`。
- 编辑器初始化脚本 `Assets/Editor/ProjectInitializer.cs` 将已弃用的 `EditorSettings.externalVersionControl` 替换为 `VersionControlSettings.mode`，消除 CS0618 警告。

2026-04-19：完成实施计划第5步（GameContext 全局上下文）并完成验证
- 新增运行时核心脚本：`GameContext`、`GameServices`、`JourneyState`，并通过 `GameContextBootstrap` 在场景加载后自动确保上下文实例存在（`DontDestroyOnLoad`）。
- `GameContext` 已接入第4步数据层：读取 `ResourceTableConfig` 初始化资源字典（含 `Crisis` 同步到 `JourneyState`）、加载起始卡池与事件池，并提供统一读写接口（`GetResource`/`SetResource`/`AddResource`、`SetCardPool`、`SetEventPool`）。
- 新增 `GameContextDebugPanel`（TMP Overlay 调试面板），用于实时显示旅途状态、资源、卡池和事件池，订阅 `Initialized/StateChanged` 事件自动刷新。
- 本次人工验证结果：Play 模式下 `ContextText` 的 TMP `Text` 字段可见 `GameContext Debug`、`Resources` 和卡池/事件池信息，且资源数据可更新；验证通过第5步验收标准“进场景可读取并显示初始卡池与资源数值”。

2026-04-19：完成实施计划第6步代码实现（事件总线与消息类型，待验证）
- 新增事件总线 `GameEventBus`（`Subscribe<T>`/`Publish<T>`/`Unsubscribe<T>`），并通过 `IDisposable` 订阅句柄统一管理解绑。
- 新增消息类型 `GameContextInitializedEvent`、`ResourceChangedEvent`、`CardDrawnEvent`、`NodeSelectedEvent`，统一约定“初始化/资源变更/抽卡/节点选择”事件载荷。
- `GameContext` 完成事件发布接入：初始化完成、资源变化、节点推进、抽卡动作均会发布对应消息。
- `GameContextDebugPanel` 完成事件订阅接入：订阅上述消息后刷新 UI，并在禁用/重绑定时释放订阅，降低空引用风险。
- 验证状态：等待测试执行第6步验收（资源变更触发 UI 实时刷新，且无空引用）。

2026-04-19：第6步验证通过（事件流）
- 由测试执行验证通过：事件链路可触发并驱动 UI 刷新，抽卡日志正常（示例：`Step6TestDriver: drew card New Card (card.id).`）。
- 验收结论：实施计划第6步完成；按要求暂不开始第7步。

2026-04-19：完成实施计划第7步代码实现（节点式大地图，待验证）
- 新增地图建模与生成逻辑：`JourneyMap`、`JourneyMapNode`、`JourneyMapGenerationConfig`、`JourneyMapGenerator`。
- 地图拓扑采用分层 DAG：起点 -> 多层内容节点（战斗/事件/补给）-> 首领节点；默认配置下生成 14 个节点（满足 10+）。
- 生成逻辑保证起点至少 2 条可选分支，并统计 `RouteCount` 与 `BranchingNodeCount` 供验收。
- `GameContext` 已接入地图初始化与重建接口（`RegenerateJourneyMap`），并发布 `JourneyMapGeneratedEvent`。
- `GameContextDebugPanel` 已展示地图摘要（节点数、路线数、分支节点数、类型计数）；新增 `GameContextStep7TestDriver` 便于手工验证。
- 验证状态：等待测试执行第7步验收；在通过前不进入第8步。

2026-04-19：第7步验证通过（节点式大地图）
- 测试结果通过，日志示例：`Step7TestDriver Event: MapGenerated seed=1668988292, nodes=14, routes=16, branchingNodes=10, battle/event/supply/boss=7/3/3/1.`
- 验收结论：节点数、路线数、分支数与节点类型分布均符合第7步目标。
- 按要求：在你明确指令前，不开始第8步实现。

2026-04-23：完成实施计划第8步代码实现（旅途推进 + 场景路由 + 断粮阻断）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增旅途推进双阶段流程：`TryEnterNextJourneyNode`（进入节点）与 `TryCompleteActiveJourneyNode`（完成节点并结算），并加入粮食消耗（默认每步 1）与断粮拦截。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增第8步事件与阻断原因枚举：`JourneyNodeEnteredEvent`、`JourneyNodeCompletedEvent`、`JourneyAdvanceBlockedEvent`、`JourneyAdvanceBlockReason`。
- 在 `Assets/Scripts/Core/JourneyMap.cs` 增加节点 ID 索引（`TryGetNode`）用于路径合法性校验和节点查询。
- 新增 `Assets/Scripts/Core/JourneyNodeSceneRouter.cs`，监听 `JourneyNodeEnteredEvent` 并按节点类型场景名尝试加载对应场景（未进 Build Settings 时给出 warning，不中断流程）。
- 更新 `Assets/Scripts/Core/GameContextBootstrap.cs`，自动确保 `JourneyNodeSceneRouter` 存在；更新 `Assets/Scripts/Core/GameContextDebugPanel.cs` 展示第8步信息（可选下一节点、激活遭遇、阻断提示等）。
- 新增 `Assets/Scripts/Core/GameContextStep8TestDriver.cs` 作为第8步手工验收驱动（GUI 点击与热键），支持验证“点击节点进入场景、完成后推进并扣粮、断粮禁止前进”。
- 体验修正：`GameContextStep8TestDriver` 改为 `DontDestroyOnLoad` 常驻，避免切场景后按钮消失；调试面板字体与换行参数调整为更易读。

2026-04-23：第8步验证通过（由测试执行）
- 验证通过：点击节点可进入对应场景；完成节点后推进到目标节点并扣除粮食；粮食为 0 时禁止继续前进并提示阻断原因。
- 现状说明：进入节点后看到蓝色背景属于占位测试场景默认相机背景色，符合当前阶段预期；正式地图/HUD 回场流程不在第8步范围内。
- 按约束执行：第9步（危机值与灾害事件挂钩）尚未开始。

2026-04-27：完成实施计划第9步代码实现（危机值联动灾害事件）
- 在 `Assets/Scripts/Core/GameContext.cs` 接入危机系统参数与状态：每次完成节点后按 `CrisisGainPerAdvance` 自动增长危机值；新增灾害触发阈值与步长（`DisasterTriggerThreshold` / `DisasterTriggerStep`）以及下一次触发阈值追踪。
- 在 `Assets/Scripts/Core/GameContext.cs` 增加阈值检测与强制触发链路：`SetResource(ResourceType.Crisis, ...)` 时评估是否跨阈值；跨阈值后强制选择灾害事件并发布 `CrisisDisasterTriggeredEvent`。
- 在 `Assets/Scripts/Data/ScriptableObjects/GameDataTypes.cs` 新增 `DisasterEventType` 枚举（`Plague` / `BanditRaid` / `NaturalDisaster`）；在 `Assets/Scripts/Data/ScriptableObjects/EventConfig.cs` 增加 `IsDisasterEvent` 与 `DisasterType`，支持策划标记灾害事件。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 `CrisisDisasterTriggeredEvent`（危机值、触发阈值、事件引用、灾害类型、是否 fallback），供调试层与后续事件执行层消费。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 新增危机系统可视化区（每步危机增长、阈值、下次触发阈值、待处理灾害、最近触发信息）。
- 在 `Assets/Scripts/Core/GameContextStep6TestDriver.cs` 增加第9步手工验收入口：`V` 键将危机值快速设置到下一触发阈值，并输出 `CrisisDisasterTriggeredEvent` 日志。
- 更新 `Assets/Data/TestStep4/EventConfig.asset`：将测试事件标记为灾害事件（`Plague`），确保第9步验收时可直接观察“灾害类型正确出现”。

2026-04-27：第9步验证通过（由测试执行）
- 验证通过：手动拉高危机值可触发强制灾害事件，且事件类型正确输出（满足实施计划第9步验收标准）。
- 兼容性修正：为避免进入 Step8 节点场景后 Step6/Step7 测试入口丢失，已将 `GameContextStep6TestDriver` 与 `GameContextStep7TestDriver` 调整为 `DontDestroyOnLoad` 单例常驻；返回后按钮文字与热键恢复正常。
- 约束执行：在你确认第9步验证通过前未开始第10步；当前已完成文档同步，后续可按指令再进入第10步。

2026-04-27：完成实施计划第10步代码实现（战斗入口）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增敌人池接入（`_startingEnemyPool` / `SetEnemyPool`）与战斗节点配置生成器：基于地图 seed + nodeId 生成可复现的 `BattleEncounterConfig`，覆盖 Battle/Boss 节点。
- 在 `Assets/Scripts/Core/GameContext.cs` 的 `TryEnterNextJourneyNode` 接入战斗入口校验：进入 Battle/Boss 节点前必须存在对应战斗配置；成功时激活 `ActiveBattleEncounterConfig`，失败时发布阻断原因 `MissingBattleEncounterConfig`。
- 新增 `Assets/Scripts/Core/BattleEncounterConfig.cs`，统一承载“节点ID、节点类型、遭遇seed、敌方队列”运行时快照，避免 UI/验证器直接依赖 `GameContext` 内部字典结构。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 `BattleEncounterPreparedEvent`，用于广播“战斗节点已准备好敌方队列”；并扩展 `JourneyAdvanceBlockReason`。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 新增战斗入口观测区：展示敌人池规模、节点配置队列、当前激活队列及一致性结果（`Queue Matches Node Config`）。
- 新增 `Assets/Scripts/Core/BattleSceneEntryVerifier.cs` 并在 `Assets/Scripts/Core/GameContextBootstrap.cs` 自动注入：战斗场景加载后输出“nodeConfig vs activeQueue”一致性日志，作为第10步验收主日志。
- 更新 `Assets/Scripts/Core/GameContextStep8TestDriver.cs`：订阅并输出 `BattleEncounterPreparedEvent`，便于在旅途面板直接确认节点进入时的敌方队列。
- 更新 `ProjectSettings/EditorBuildSettings.asset`：补齐 `BattleScene` / `EventScene` / `SupplyScene` / `BossScene` 到 Build Settings，确保第8/10步场景切换链路完整。

2026-04-27：第10步验证通过（由测试执行）
- 验证通过：从战斗节点进入 `BattleScene` 时，敌方队列与节点配置一致（`Step10Verifier` 日志 `match=True`），满足“进入战斗时敌人列表与节点配置一致”的实施计划第10步验收标准。
- 验证过程配套日志：`Step8TestDriver Event: BattleEncounterPrepared ... queue=[...]` 与 `Step10Verifier: Battle entry loaded ... nodeConfig=[...], activeQueue=[...], match=True`。
- 约束执行：在你确认第10步验证通过前未开始第11步；当前仅完成第10步与文档同步。

2026-04-30：完成实施计划第11步（回合流程）并验证通过（由测试执行）
- 新增 `BattleTurnController`，实现战斗回合主循环：玩家回合（能量重置/抽牌）-> 出牌 -> 弃牌 -> 敌方行动阶段 -> 下一回合抽牌补满。
- 回合内统一使用共享能量池，出牌按 `CardConfig.EnergyCost` 扣能量，并按 `ExhaustOnPlay` 进入 `ExhaustPile` 或 `DiscardPile`。
- 抽牌逻辑加入弃牌堆回洗（reshuffle）并带边界保护，避免卡堆越界与空堆异常。
- 新增并接入回合事件：`BattleFlowInitializedEvent`、`BattleTurnStartedEvent`、`BattleCardPlayedEvent`、`BattleHandDiscardedEvent`、`BattleEnemyTurnResolvedEvent`、`BattleCardsDrawnEvent`、`BattleFlowEndedEvent`。
- `GameContextDebugPanel` 与 `GameContextStep8TestDriver` 已接入第11步事件与状态展示，支持热键 `P`（打第一张可打牌）和 `E`（结束回合）做手工回归。
- 验证结论：已满足第11步验收标准（每回合能量重置、抽弃牌计数正确、无卡堆越界）；按约束未开始第12步。
