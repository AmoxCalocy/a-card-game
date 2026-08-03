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

2026-05-11：完成实施计划第12步（牌类型与效果框架）并验证通过（由测试执行）
- 新增 `Assets/Scripts/Core/BattleCombatantState.cs`，建立战斗运行时单位状态模型：生命、护甲、状态叠层、受击/治疗/护甲增减接口，供卡效执行统一消费。
- 扩展 `Assets/Scripts/Core/BattleTurnController.cs`：
  - 新增玩家与敌方运行时状态（`_playerState`、`_enemyStates`），战斗开始时基于遭遇配置构建单位快照。
  - 在出牌链路接入 `ExecuteCardEffect`，按 `CardType` 执行攻击/防御/策略/后勤/战术五类效果，并支持状态叠加（如 `Bleed`、`Morale`、`Encircled`）。
  - 新增效果摘要 `LastCardEffectSummary`，用于调试面板与日志快速比对“描述 -> 实际生效”。
- 扩展 `Assets/Scripts/Core/GameEventMessages.cs` 的 `BattleCardPlayedEvent` 载荷，补充卡牌类型、基础值、状态请求与实际效果结果（伤害/护甲/治疗/抽牌/状态叠层/摘要）。
- 更新 `Assets/Scripts/Core/GameContextDebugPanel.cs`，新增战斗观测字段：玩家 HP/护甲/状态、敌方逐单位 HP/护甲/状态、最近一次卡效摘要。
- 更新 `Assets/Scripts/Core/GameContextStep8TestDriver.cs` 的出牌日志，输出第12步效果结果明细，便于手工验收。
- 更新/新增 5 张示例牌资产：
  - `Assets/Data/TestStep4/CardConfig.asset`（Attack：破甲斩）
  - `Assets/Data/TestStep4/CardConfig_Defense.asset`（Defense：坚守阵线）
  - `Assets/Data/TestStep4/CardConfig_Strategy.asset`（Strategy：侦察预案）
  - `Assets/Data/TestStep4/CardConfig_Logistics.asset`（Logistics：战地医护）
  - `Assets/Data/TestStep4/CardConfig_Tactic.asset`（Tactic：包围网）
- 验证结论：第12步验收通过（5张示例牌按描述生效，且状态可叠层）；按约束未开始第13步。

2026-05-13：完成实施计划第13步（敌人 AI 意图）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/BattleTurnController.cs` 引入敌方意图计划层：每个玩家回合开始时为每个敌人生成下一回合意图快照（`Attack`/`Defend`/`Plunder`），并通过事件总线发布。
- 将敌方阶段从“占位事件”升级为“按意图执行”：
  - `Attack`：对玩家状态执行伤害结算（受护甲吸收影响）。
  - `Defend`：为对应敌方单位增加护甲。
  - `Plunder`：优先掠夺 `Wealth`，不足时继续掠夺 `Food`，并记录掠夺总量。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增第13步事件契约：
  - `BattleEnemyIntentView`：单个敌人的意图快照（类型、计划值、摘要、是否已死亡）。
  - `BattleEnemyIntentUpdatedEvent`：回合维度的意图刷新事件。
  - 扩展 `BattleEnemyTurnResolvedEvent`：新增 `TotalDamageToPlayer` / `TotalArmorGained` / `TotalResourcesPlundered` / `Summary`，用于对照意图与执行结果。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 增加战斗观测字段：`Next Enemy Intents` 与 `Last Enemy Turn`，并订阅 `BattleEnemyIntentUpdatedEvent` 以确保意图变化即时可见。
- 在 `Assets/Scripts/Core/GameContextStep8TestDriver.cs` 增加第13步日志输出：
  - `Step13TestDriver Event: EnemyIntentUpdated ...`
  - `Step13TestDriver Event: EnemyTurnResolved ... result(dmg/armor/plunder)=...`
- 验收说明（关键口径）：
  - `EnemyIntentUpdated` 的 `intents` 表示“计划值（planned）”；
  - `EnemyTurnResolved` 的 `result` 表示“实际生效值（effective）”；
  - 二者按同一 `turn` 对齐，允许出现 `effective <= planned`（如护甲吸收、资源不足、目标已死亡）。
- 验证结论：第13步验收通过；在你后续明确指令前不开始第14步。

2026-05-14：完成实施计划第14步（战斗结算）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 `BattleSettledEvent`（IsVictory、Rewards、ResourcesLost、CardsDiscardedCount、CompanionInjured、SettlementSummary），作为结算结果的唯一事件契约。
- 在 `Assets/Scripts/Core/GameContext.cs` 新增 `TryRemoveRandomCard(out CardConfig)`，从卡池中随机移除一张牌，供失败弃牌惩罚使用。
- 在 `Assets/Scripts/Core/BattleTurnController.cs` 实现结算核心：
  - 新增检查器参数：`_defeatWealthLossPercent`（30%）、`_defeatFoodLossPercent`（20%）、`_defeatCardsLostCount`（1）。
  - 新增 `CheckBattleOutcome()`：打出卡牌后及敌方回合结束后调用，检测全部敌人阵亡（胜利）或玩家阵亡（失败）。
  - 新增 `ResolveVictory()`：遍历全部敌人状态，汇总 `EnemyConfig.DefeatRewards`，通过 `GameContext.AddResource` 发放，发布 `BattleSettledEvent`，调用 `EndBattleFlow("Victory")`。
  - 新增 `ResolveDefeat()`：按百分比扣除财富/粮食，随机移除卡牌，发布 `BattleSettledEvent`，调用 `EndBattleFlow("Defeat")`。
  - 在 `TryPlayCard` 中接入胜利检测（打出卡牌后立即检测，击败最后一个敌人即刻胜利）。
  - 在 `TryEndPlayerTurn` 中接入失败检测（敌方回合结算后检测，仅在未结束时开始下一回合）。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 新增结算观测区（Last Settlement），战斗结束后持续可见；包含结局、摘要、奖励、损失、弃牌数。
- 在 `Assets/Scripts/Core/GameContextStep8TestDriver.cs` 新增 `BattleSettledEvent` 订阅，输出结算日志及 `FormatResourceAmounts` 工具方法。
- 更新 `Assets/Data/TestStep4/EnemyConfig.asset`：测试敌人战利品设为 +10 Food、+5 Wealth（原值为 0，导致奖励不可见）。
- 关键设计决策：
  - 结算在 `EndBattleFlow` 清空状态前发布事件，确保订阅方可访问战斗数据。
  - 战利品包含已击败和刚击败的全部敌人（`_enemyStates` 不清理已击败单位）。
  - 失败为二元结果，不给部分奖励；弃牌从持久化 `GameContext.CardPool` 移除（非临时战斗牌堆）。
  - `CompanionInjured` 硬编码为 `false`，作为第15-17步伙伴系统的占位钩子。
- 验证结论：第14步验收通过（胜利后资源增加、失败后资源/卡牌减少均符合规则）；按约束未开始第15步。

2026-07-30：完成实施计划第15步（伙伴招募）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 `CompanionRecruitedEvent`（Companion、ActiveCompanionCount、ReserveCompanionCount、StarterCardsAdded、AddedToActive、Summary），作为伙伴招募的统一事件契约。
- 在 `Assets/Scripts/Core/GameContext.cs` 新增伙伴管理系统：
  - 新增 `_activeCompanions`（最多 4 上阵）与 `_companionReserve`（后备）列表，以及 `MaxActiveCompanions` 常量。
  - 新增 `TryRecruitCompanion(CompanionConfig, out string)`：校验空值/重复招募，若激活未满则加入激活队伍否则进入后备；自动将伙伴的 `StarterCards` 加入持久卡池；发布 `CompanionRecruitedEvent`。
  - 新增 `ResetCompanionState()` 在初始化时清空伙伴列表。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 新增伙伴观测区（`AppendCompanionSummary`）：显示激活队伍（角色/HP/忠诚/卡牌数）和后备列表；订阅 `CompanionRecruitedEvent` 实时刷新；面板高度从 760 增加到 880。
- 新增 `Assets/Scripts/Core/GameContextStep15TestDriver.cs`：第15步手工验收驱动，`G` 键逐个招募、`H` 键一键全部招募；Editor 下自动从 `Assets/Data` 扫描 `CompanionConfig` 资产填充测试列表；失败时输出 `Debug.LogError` 确保 Console 可见。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 新增 `GameContextStep15TestDriver` 自动注入，避免手动挂载。
- 更新/新增 6 个测试伙伴资产（老兵/斥候/护卫/医者/使节/游侠），覆盖全部 5 种 CompanionRole，用于验证激活上限与后备机制。
- 关键设计决策：
  - 激活队伍上限 4 人，超出的伙伴进入后备（Reserve），后备暂不参与战斗但已招募且卡牌已入池。
  - 重复招募同一伙伴被拒绝（基于 `CompanionConfig` 引用判重），确保不会重复添加卡牌。
  - 伙伴起始卡牌直接加入 `GameContext.CardPool`（持久卡池），不在战斗牌堆中，确保招募效果跨战斗持续。
  - `CompanionInjured` 在第14步已预留钩子，第16步伙伴忠诚与受伤系统将消费此钩子。
- 验证结论：第15步验收通过（招募后伙伴进入激活/后备正确，卡池自动增加起始卡牌，重复招募被拒绝并显示错误日志）；按约束未开始第16步。

2026-07-31：完成实施计划第16步（伙伴特质与忠诚度）并验证通过（由测试执行）
- 新增 `Assets/Scripts/Core/CompanionState.cs`：运行时伙伴状态模型，封装 `CompanionConfig` 并管理 `CurrentLoyalty`（0-100）、`CurrentHealth`、`IsInjured`；提供四级忠诚度标签（Loyal ≥60 / Uneasy 30-59 / Discontent 1-29 / Rebellious 0）、离队风险计算（0%/15%/40%/100%）、忠诚度修正技能检定值（+2/0/-2/-5）。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 扩展事件体系：
  - `CompanionRecruitedEvent` 载荷从 `CompanionConfig` 升级为 `CompanionState`。
  - 新增 `CompanionLoyaltyChangedEvent`（前后忠诚度、delta、原因）。
  - 新增 `CompanionDepartureWarningEvent`（离队风险、警告消息）。
  - 新增 `CompanionDepartedEvent`（是否曾为激活队员、离队原因）。
  - 新增 `CompanionSkillCheckEvent`（d20、技能值、DC、使用的特质、成功/失败）。
- 重构 `Assets/Scripts/Core/GameContext.cs` 伙伴系统：
  - `_activeCompanions` 与 `_companionReserve` 从 `List<CompanionConfig>` 改为 `List<CompanionState>`，公开属性同步更新。
  - 新增 `TryFindCompanion(CompanionConfig, out CompanionState)`：按配置引用查找运行时状态。
  - 新增 `ModifyCompanionLoyalty(state, delta, reason)`：修改忠诚度后自动检测归零离队或发布警告。
  - 新增 `TryCompanionSkillCheck(state, difficulty, out result)`：d20 + 技能值 vs DC 检定。
  - 新增 `CheckCompanionDeparture(state, out message)`：按离队风险随机判定。
  - 新增 `TryRemoveCompanion(state, reason)`：从队伍移除并发布离队事件。
- 升级 `Assets/Scripts/Core/GameContextDebugPanel.cs`：伙伴区域改为显示运行时状态（HP、忠诚度标签、离队风险、特质列表、技能加值）；新增 4 个事件订阅（LoyaltyChanged/DepartureWarning/Departed/SkillCheck）。
- 升级 `Assets/Scripts/Core/GameContextStep15TestDriver.cs` 为 Step15+16 联合驱动：
  - 新增 Step16 热键：`[+]/[-]`（小键盘）增减选中伙伴忠诚度 10 点、`K` 技能检定、`L` 离队判定、`1/2` 切换选中伙伴。
  - GUI 面板增大（260→400），显示每个伙伴的忠诚度/标签/风险和选中伙伴详细属性。
  - 新增 4 个事件日志输出。
- 关键设计决策：
  - 忠诚度归零立即自动离队（`ShouldAutoDepart`），不需要额外手动操作。
  - 离队风险判定采用随机 roll vs `DepartureRisk` 概率，仅在主动调用 `CheckCompanionDeparture` 时生效。
  - 技能检定时随机选择一个伙伴特质 ID 作为 flavor 输出，后续可扩展为特质间联动规则。
  - 离队后伙伴起始卡牌不从卡池移除，保持简化设计。
- 验证结论：第16步验收通过（忠诚度归零自动离队、警告/危险区间概率离队、技能检定正确输出结果）；按约束未开始第17步。

2026-07-31：完成实施计划第17步（队伍编排 UI）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增队伍编排 API：
  - `SwapActiveCompanions(indexA, indexB)`：交换激活队伍中两个伙伴的位置，发布 `CompanionSquadReorderedEvent`。
  - `MoveCompanionToActive(companion, targetIndex)`：将后备伙伴移到激活队伍指定位置，发布 `CompanionMovedToActiveEvent`。
  - `MoveCompanionToReserve(companion)`：将激活伙伴移到后备，发布 `CompanionMovedToReserveEvent`。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 3 个编排事件：`CompanionSquadReorderedEvent`、`CompanionMovedToActiveEvent`、`CompanionMovedToReserveEvent`。
- 在 `Assets/Scripts/Core/BattleTurnController.cs` 接入伙伴编队：
  - 新增 `_companionFormation` 列表与 `CompanionFormation` 公开属性。
  - 战斗开始时从 `GameContext.ActiveCompanions` 读取当前顺序作为战斗站位快照。
  - 战斗结束时清空编队。
  - 站位标签：`[0] Vanguard`、`[1] Left`、`[2] Right`、`[3] Slot3`。
- 在 `Assets/Scripts/Core/GameContextDebugPanel.cs` 战斗区域新增 `Companions (Formation)` 区块，显示站位标签、伙伴名、角色和忠诚度。
- 升级 `Assets/Scripts/Core/GameContextStep15TestDriver.cs` 为 Step15+16+17 联合驱动：
  - GUI 移至屏幕居中，避免与 Step8 驱动和 Debug 面板重叠。
  - 新增 Step17 热键：`[`/`]` 交换位置、`R` 移到后备、`A` 移到激活。
  - 激活队伍显示站位标签（Vanguard/Left/Right/Slot）。
- 关键设计决策：
  - 编队在进入战斗时拍快照，战斗中不动态更新，保证战斗流程确定性。
  - 战斗外可自由编排，下次进入战斗时生效。
  - 站位标签为位置语义标识，后续可影响技能目标选择和受击概率。
- 验证结论：第17步验收通过（交换位置后进入战斗编队正确更新，激活/后备互移正常）；按约束未开始下一步。

2026-07-31：完成实施计划第18步（资源系统验证）并验证通过（由测试执行）
- 资源核心系统在 Step 5 已实现（`GetResource`/`SetResource`/`AddResource`、`ResourceChangedEvent`、非 Crisis 负数截断），第18步仅补充验收驱动。
- 新增 `Assets/Scripts/Core/GameContextStep18TestDriver.cs`：
  - F1-F8 热键增减 Food/Wealth/Reputation/MedicalSupplies（每次 ±10）。
  - F9 负数截断测试：设 Food=5 后 -15，验证结果为 0（PASS/FAIL 日志）。
  - F10 Crisis 负数测试：设 Crisis=-10，验证负数不被截断（PASS/FAIL 日志）。
  - 订阅 `ResourceChangedEvent` 并输出 Console 日志和面板最近变更列表。
  - GUI 面板位于右下角，避免遮挡调试面板。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step18 驱动自动注入。
- 验证结论：第18步验收通过（资源增减正确、非 Crisis 负数截断生效、Crisis 允许负值、事件日志完整）；按约束未开始下一步。

2026-07-31：完成实施计划第19步（交易系统）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增交易 API：
  - `GetTradePrice(ResourceType type, bool isBuying)`：基于声望计算价格。买入价 = 基准价 × (1 - 声望×0.5%，上限折扣 50%)；卖出价固定为基准价×0.5，不受声望影响。
  - `TryBuyResource(type, amount, out message)`：消耗 Wealth 购买资源，余额不足返回失败。
  - `TrySellResource(type, amount, out message)`：出售资源换取 Wealth，库存不足返回失败。
  - 基准价：Food(2)、MedicalSupplies(3)、BuildingMaterials(4)、Intel(5)、DraftOrder(5)。
  - 不可交易 Wealth 和 Crisis。
- 新增 `Assets/Scripts/Core/GameContextStep19TestDriver.cs`：
  - F1/F2 买/卖 Food，F3/F4 买/卖 MedicalSupplies，F5 输出所有资源当前价格到 Console。
  - GUI 面板位于右侧中部，显示 Wealth、Reputation、各资源买卖价和最近操作结果。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step19 驱动自动注入。
- 在 `SampleScene` 中创建 Step18/Step19 驱动 GameObject，便于 Inspector 调参。
- 关键设计决策：卖出价固定，仅买入享受声望折扣——避免高声望反而低卖出的不合理情况。
- 验证结论：第19步验收通过（高声望后买入价格显著下降、卖出价保持固定、买卖资源正确增减）；按约束未开始下一步。

2026-07-31：完成实施计划第20步（补给与治疗）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增治疗/受伤 API：
  - `TryHealCompanion(companion, out message)`：消耗 1 MedicalSupplies 治愈伙伴受伤，恢复满血。
  - `TryInjureCompanion(companion)`：使伙伴受伤（HP 减半），供测试验证治疗链路。
- 新增 `Assets/Scripts/Core/GameContextStep20TestDriver.cs`：
  - F1/F2 在 Supply 节点用 Wealth 买 Food（1/5 个），复用 Step19 交易 API。
  - F3 治愈选中伙伴（消耗 1 Medical），F4 使选中伙伴受伤（测试用）。
  - F5/F6 切换选中伙伴。
  - 补给操作仅在当前激活节点为 Supply 时生效，非 Supply 节点提示"Not at a Supply node"。
  - GUI 面板位于右侧上部（Step8 下方），显示节点状态、资源、伙伴 HP/受伤状态。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step20 驱动，`SampleScene` 中创建对应 GameObject。
- 关键设计决策：补给操作绑定 Supply 节点类型校验，确保"在补给节点可买粮治疗"的语义正确。
- 验证结论：第20步验收通过（Supply 节点可买粮治疗、非 Supply 节点拒绝、MedicalSupplies 不足时治愈失败、治愈后受伤状态清除 HP 恢复）；按约束未开始下一步。

2026-08-03：完成实施计划第21步（事件系统）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增 `TryResolveEvent(EventConfig, optionIndex, out summary)`：
  - 支持四种解法类型：`Combat`（触发战斗）、`SkillCheck`（按 SuccessChance 随机判定）、`PayResource`（扣除 cost 直接成功）、`SacrificeCard`（随机移除卡牌）。
  - 统一处理 costs 扣除、rewards 发放、Reputation 门槛校验、Companion 招募。
  - 发布 `EventResolvedEvent`（event/option/type/success/summary），Console 同步日志。
- 在 `Assets/Scripts/Core/GameEventMessages.cs` 新增 `EventResolvedEvent` 事件体。
- 新增 `Assets/Scripts/Core/GameContextStep21TestDriver.cs`：
  - F1-F3 选择事件选项，F4 切换事件，F5 Console 输出事件详情。
  - Editor 下自动从 `Assets/Data` 扫描 EventConfig 资产。
  - GUI 面板位于右侧中部，显示事件描述和各选项的 cost/reward/type/chance。
  - 订阅 `EventResolvedEvent` 输出结算日志。
- 更新 `Assets/Data/TestStep4/EventConfig.asset` 测试事件：配置三种解法（PayResource: 3Food→5Wealth、SkillCheck: 60%→3Medical、SacrificeCard: 1Card→10Reputation）。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step21 驱动，`SampleScene` 中创建对应 GameObject。
- 验证结论：第21步验收通过（三种解法均能落地到资源/卡牌变化，事件日志完整）；按约束未开始下一步。

2026-08-03：完成实施计划第22步（技能检定系统）并验证通过（由测试执行）
- 在 `Assets/Scripts/Data/ScriptableObjects/EventConfig.cs` 的 `EventOptionData` 新增 `FailurePenalties` 字段，支持检定失败时扣除资源。
- 升级 `Assets/Scripts/Core/GameContext.cs` 的 `TryResolveEvent` SkillCheck 分支：
  - 从简单概率改为 d20 + 伙伴技能值 + 声望修正 判定。
  - 自动选择激活队伍中技能值最高的伙伴（`GetBestCompanionForCheck`）。
  - DC = max(6, 12 - 声望×3%)，高声望降低难度。
  - 成功发放 Rewards，失败扣除 FailurePenalties。
  - 无伙伴时纯 d20 vs DC。
- 新增 `Assets/Scripts/Core/GameContextStep22TestDriver.cs`：
  - F1 对最佳伙伴执行技能检定，F2 显示最佳伙伴和当前 DC。
  - GUI 右侧底部，显示声望、DC、最佳伙伴信息。
- 更新测试事件（Scout Ahead）：失败时 -2 Food。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step22 驱动，`SampleScene` 中创建对应 GameObject。
- 关键设计决策：声望降低 DC 而非提高，确保"高声望=高成功率"的直觉正确。
- 验证结论：第22步验收通过（高声望 DC 降低、伙伴技能值参与检定、失败扣资源正确）；按约束未开始下一步。

2026-08-03：完成实施计划第23步（灾害事件卡组污染）并验证通过（由测试执行）
- 在 `Assets/Scripts/Core/GameContext.cs` 新增 `_disasterCardTemplates` 字段（灾害触发时注入卡池的负面牌模板）。
- 升级 `TriggerDisasterEvent`：灾害触发后自动遍历模板列表，将 Curse/Disease 牌加入 `_cardPool`，消息中标注"Added N disaster card(s)"。
- Editor 下自动从 `Assets/Data` 扫描文件名含 "Curse" 或 "Disease" 的 `CardConfig` 资产填充模板列表。
- 创建测试负面牌资产：
  - `Assets/Data/TestStep4/CardConfig_Curse.asset`（Plague Curse，攻击牌，BaseValue=-3）
  - `Assets/Data/TestStep4/CardConfig_Disease.asset`（Fatigue，后勤牌，BaseValue=-5）
- 新增 `Assets/Scripts/Core/GameContextStep23TestDriver.cs`：
  - F1 显示灾害信息（Crisis/阈值/Next/Pending），F2 拉高 Crisis 触发灾害。
  - 订阅 `CrisisDisasterTriggeredEvent` 输出 Console 日志。
  - GUI 右下角，显示 Crisis、阈值、卡池计数、Pending 灾害。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 Step23 驱动，`SampleScene` 创建 GameObject。
- 验证结论：第23步验收通过（灾害触发后卡池自动增加 Curse/Disease 牌、事件类型和 fallback 标记正确）；按约束未开始下一步。

2026-08-03：完成实施计划第24步（主 HUD）并验证通过（由测试执行）
- 新增 `Assets/Scripts/Core/GameContextHUD.cs`：屏幕顶部横条 HUD，Canvas Overlay。
  - 常驻显示 Food/Wealth/Reputation/Medical/Crisis 资源值。
  - 旅途中显示当前节点 ID 和类型。
  - 战斗中显示 Turn/Energy/Draw/Hand/Discard 实时计数。
  - 订阅 ResourceChanged、TurnStarted、CardPlayed、CardsDrawn、HandDiscarded、FlowEnded、NodeEntered 七种事件，每次操作即时刷新。
- 创建 HUD Prefab `Assets/Prefabs/GameContextHUD.prefab`：半透明黑底白字横条，16px 字体，1920x1080 自适应缩放。
- Debug 面板下移 32px（HUD 高度），避免遮挡。
- 在 `Assets/Scripts/Core/GameContextBootstrap.cs` 注册 HUD 自动注入。
- 验证结论：第24步验收通过（资源变化实时刷新、战斗中出牌/抽牌/弃牌计数同步、切场景数值一致）；按约束未开始下一步。
