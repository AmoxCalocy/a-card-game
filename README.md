# 一人旅途 · Roguelike 卡牌冒险

**一人旅途**是一款 roguelike 牌组构筑 + 轻策略经营游戏。玩家从独行旅人出发，通过探索、卡牌战斗、伙伴招募与城镇建设，逐步成长为帝国的缔造者。

## 核心体验

- **牌组构筑**：旅途与战斗均由卡牌驱动，事件可添加、移除、升级卡牌
- **队伍编制**：招募伙伴组建队伍（最多 4 上阵），伙伴各有专属卡牌与特质
- **资源管理**：粮食/财富/声望三角资源，支撑移动、交易、建设
- **建设系统**：营地→城镇→都城三级建设，解锁新卡池与被动增益
- **风险决策**：危机值累积触发灾害事件，劫匪/瘟疫/天灾影响资源与伙伴状态
- **高重玩性**：分层 DAG 地图、多分支事件、周目解锁与遗产系统

## 技术栈

| 类别 | 选型 |
|------|------|
| 引擎 | Unity 2022.3.62f3 (LTS) + URP 2D |
| 输入 | Input System（键鼠/手柄/触屏） |
| 渲染 | URP 2D Renderer + Cinemachine |
| 资源管理 | Addressables 2.x（异步加载、热更） |
| UI | UI Toolkit + TextMeshPro |
| 动画 | DOTween |
| 数据 | ScriptableObject 数据驱动，JSON 存档 |
| 架构 | 事件总线（GameEventBus）+ 服务定位器（GameServices） |
| CI/CD | GitHub Actions + game-ci/unity-builder |

## 开发进度

实施计划共 **33 步**，分 10 个阶段。当前进度：**15/33（45%）**

### 已完成

| 步骤 | 内容 | 状态 |
|------|------|------|
| 0 | 项目初始化（URP 2D、Input System、Addressables、TMP） | ✅ |
| 1 | Git + Git LFS + 分支策略 | ✅ |
| 2 | CI/CD（GitHub Actions 自动构建 Windows） | ✅ |
| 3 | ScriptableObject 数据模板（6 类） | ✅ |
| 4 | GameContext 全局上下文 + 调试面板 | ✅ |
| 5 | 事件总线 GameEventBus | ✅ |
| 6 | 节点式大地图生成（分层 DAG） | ✅ |
| 7 | 旅途推进 + 场景路由 + 断粮阻断 | ✅ |
| 8 | 危机值与灾害事件触发 | ✅ |
| 9 | 战斗入口（敌方队列配置与校验） | ✅ |
| 10 | 回合制战斗流程（能量/抽牌/弃牌/回洗） | ✅ |
| 11 | 卡牌效果框架（5 类卡牌 + 状态叠层） | ✅ |
| 12 | 敌人 AI 意图系统（攻击/防御/掠夺） | ✅ |
| 13 | 战斗结算（胜利奖励/失败惩罚） | ✅ |
| 14 | 伙伴招募（最多4上阵 + 起始卡牌注入） | ✅ |

### 待完成

| 阶段 | 步骤 | 内容 |
|------|------|------|
| 4 | 15-17 | 伙伴系统（招募/特质/队伍编排） |
| 5 | 18-20 | 资源经济（交易/补给/治疗） |
| 6 | 21-23 | 事件系统（多选项/技能检定/灾害） |
| 7 | 24-26 | UI/UX（HUD、卡牌提示、教程） |
| 8 | 27-28 | 存档与设置 |
| 9 | 29-30 | 平衡与内容（40 卡/10 敌/20 事件/8 伙伴） |
| 10 | 31-33 | 稳定性与打包 |

## 项目结构

```
Assets/
├── Scripts/
│   ├── Core/              # 运行时核心（GameContext、事件总线、战斗、地图、场景路由）
│   └── Data/
│       └── ScriptableObjects/  # 数据定义（CardConfig、EnemyConfig 等）
├── Editor/                # 编辑器工具（ProjectInitializer）
├── Scenes/                # 场景（SampleScene、Battle、Event、Supply、Boss）
├── Data/TestStep4/        # 测试用 ScriptableObject 资产
└── Settings/              # URP 渲染配置
memory-bank/               # 设计文档与开发记录
```

### 架构分层

1. **数据层** — `ScriptableObject` 纯数据（卡牌、敌人、事件、伙伴、遗物、资源表）
2. **运行时核心** — `GameContext` 单例 + `GameEventBus` 事件驱动 + `BattleTurnController` 战斗回合
3. **编辑器** — 项目初始化工具与资产创建菜单

## 构建与运行

```bash
# Headless 构建 (Windows)
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScripts.BuildWindows -buildTarget StandaloneWindows64 -logFile logs/build.log

# 运行 EditMode 测试
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform editmode -logFile logs/tests-edit.log

# 运行 PlayMode 测试
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform playmode -logFile logs/tests-play.log
```

## 贡献指南

- 分支策略：`main`（稳定）/ `dev`（集成）/ `feature/*`（功能开发）
- 提交规范：Conventional Commits（`feat:`/`fix:`/`docs:`/`chore:`）
- 代码风格：C# 10，4 空格缩进，Unity 标准命名（PascalCase 公共，`_camelCase` 私有字段）
- ScriptableObject 命名以 `Config` 结尾，通过 `Assets > Create > OneManJourney/Data/*` 创建
