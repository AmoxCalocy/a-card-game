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
