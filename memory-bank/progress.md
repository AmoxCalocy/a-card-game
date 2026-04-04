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
- 新增工作流 `.github/workflows/ci-build.yml`，使用 `game-ci/unity-builder@v4` 固定 Unity 版本 2022.3.62f2，目标 `StandaloneWindows64`，在 `dev/main` 的 pull request 以及手动触发时构建。
- 构建输出目录 `build/roguelike-win`，日志写入 `Logs/ci-build.log`，两者作为 artifact 上传；并缓存 `Library` 缩短重复构建耗时。
- 运行前需在仓库 Secrets 配置 `UNITY_LICENSE`（序列化 license 内容）；缺失时工作流会立即报错提示。工作流已默认使用 Node.js 24（`FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true`）以规避 Node 20 弃用警告；`providerStrategy` 使用 `unity-licensing-file` 自动读取该 secret。
