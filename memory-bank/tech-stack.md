# 最简但健壮的 Unity 技术栈（Roguelike 卡牌）

## 引擎与渲染
- **Unity 6 LTS（6000.x）+ URP 2D Renderer**：LTS 稳定、原生支持 2D 光照/后处理，满足卡面特效与轻量地图表现。
- **Input System**：事件驱动输入，易做手柄/触屏/键鼠共存与重绑定。
- **Cinemachine**：无代码相机过渡，适合事件演出与战斗镜头。

## 游戏框架（单机优先）
- **数据驱动**：`ScriptableObject` 定义卡牌/事件/伙伴，配合 JSON 存档（加密可选）。
- **状态管理**：轻量 MVC（或 UniRx/事件总线）驱动 UI 与逻辑解耦；避免过早引入 ECS。
- **动画/过渡**：DOTween（免费）处理卡牌出牌、抽牌、UI 缩放与插值。

## 资源与内容分发
- **Addressables 2.x**：异步加载、分组热更；移动端用 Addressables for Android 以 Play Asset Delivery 降包体。
- **美术管线**：PSD 导入（Sprite Editor）+ 统一 2048 图集；音频用 .ogg 压缩并走 Addressables。

## UI
- **UI Toolkit**：菜单、卡库、设定等静态/列表界面；样式用 USS，复用模板。
- **TextMeshPro**：统一字体渲染与富文本；数值/关键词高亮。

## 存档与配置
- **本地存档**：`Application.persistentDataPath` 下二进制/JSON；关键字段做校验哈希。
- **云存档（可选）**：Unity Cloud Save 或自建 S3/OSS 搭配简单 token 验证。

## 网络（可选扩展）
- **Netcode for GameObjects (NGO)**：未来若加合作或异步竞速，可用 NGO + Unity Relay；保持客户端权威以简化作弊防护。

## 构建与 CI/CD
- **版本控制**：Git + Git LFS（大图/音频）；分支策略 main/dev/feature。
- **自动化构建**：GitHub Actions + game-ci/unity-builder，输出 Windows/Linux/Mac；Addressables Build 脚本化。
- **质量工具**：NUnit + PlayMode 测试；Profiler/Memory Profiler；Code Coverage（编辑器包）。

## 监控与分析（可选）
- **Unity Analytics**：事件埋点留存/漏斗。
- **Crash 报告**：Backtrace 或 Unity Cloud Diagnostics。

## 推荐最小依赖列表
1) 核心：Unity 6 LTS、URP、Input System、TextMeshPro、Cinemachine  
2) 资源：Addressables（含 Android 插件）  
3) 工具：DOTween、NUnit、Profiler/Memory Profiler  
4) 可选：Netcode for GameObjects + Relay、Unity Cloud Save、Analytics
