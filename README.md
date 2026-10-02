# A Gift for D22

D-22 是一个围绕现场音乐空间的交互体验项目。这个仓库同时保存网页原型、Unity 桌面端项目、Blender 场景源文件、Meshy 模型和 Figma 设计交接资料。Windows/Mac 桌面端以 Unity 6 + URP 为当前开发基线。

## 现在可以运行什么

- **网页原型**：仓库根目录的 `index.html`，包含胡同、Live House、演出和唱片店场景。Windows 运行 `serve.bat`；Mac 在仓库根目录运行 `python3 -m http.server 8091 --bind 127.0.0.1`，访问 `http://127.0.0.1:8091/`。
- **Unity 项目**：用 Unity Hub 打开 [`unity/D22Game`](unity/D22Game)，版本固定为 `6000.6.4f1`，URP 固定为 `17.6.0`。入口场景由四个协作场景组成：Bootstrap、Environment、Lighting、Gameplay。
- **Blender 源文件**：[`blender/source/D22_Balanced_Lighting_v18.blend`](blender/source/D22_Balanced_Lighting_v18.blend)。这是当前布局和灯光审阅的权威源文件。

## 仓库结构

```text
assets/d22/                 Meshy GLB、PBR 贴图、输入图、提示词和生成记录
blender/source/             当前 Blender 源文件
blender/verification/       Blender 场景审阅记录
design/                     Figma 导出、平面图和设计 handoff
references/                 设计稿、资产生成依赖的照片及出处
docs/                       协作、迁移和资产说明
scripts/unity/              Blender → Unity 发布与本机协作脚本
unity/D22Game/              Unity 6 + URP 项目
index.html, scene_*.html    原有网页原型
Model/, Carsick Cars - ...  原有网页模型和音乐
```

资产目录说明见 [`docs/ASSET_CATALOG.md`](docs/ASSET_CATALOG.md)，Figma 连接说明见 [`design/figma/README.md`](design/figma/README.md)。

## 两人协作规则

1. 安装 Git LFS，克隆后先运行 `git lfs pull`，再运行 `python scripts/unity/setup_collaboration.py`。它只修改本仓库的 LFS 和 Unity SmartMerge 配置，不复制个人凭证。
2. 两人使用同一个 Unity 版本和同一个 URP 包版本。每项工作从分支开始，例如 `art/stage-update` 或 `feature/interaction-door`。
3. Blender 负责结构、布局、UV 和源材质；Unity 负责运行时材质、碰撞、灯光烘焙、相机和玩法。不要直接改 Unity 里由 Blender 发布生成的模型布局。
4. `D22 > Publish > Update Art From Blender` 只更新 Environment 和生成资源，会保留已有的 Lighting 与 Gameplay。第一次发布或确实要覆盖灯位时才使用 `D22 > Publish > Reset Lighting From Blender`。
5. 光照或几何改动后运行 `D22 > Bake Lighting` 和 `D22 > Validate Published Scene`。源文件、导出资源、场景、`.meta`、光照数据和验证结果一起提交。
6. 大型二进制文件由 Git LFS 管理；编辑前锁定 `.blend` 或 PSD 源文件。生成的 FBX 保持可写，随源文件一起发布。不要提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.env` 或 MCP/Figma 会话凭证。

本机连接步骤和固定版本见 [`docs/MCP_SETUP.md`](docs/MCP_SETUP.md)。

完整流程见 [`docs/UNITY_WORKFLOW.md`](docs/UNITY_WORKFLOW.md)。

## Blender → Unity 发布

在仓库根目录执行：

```sh
BLENDER_BIN=/Applications/Blender.app/Contents/MacOS/Blender \
  scripts/unity/export_d22.sh
```

导出结果写入 `unity/D22Game/Assets/D22/Art/`，随后打开 Unity 项目执行发布菜单。`Data/d22-export.json` 保存了源对象、材质、灯光和坐标转换信息。当前迁移基线包含 1,485 个模型渲染器、62 个材质和 47 个有效灯光；验证报告位于 `unity/D22Game/Assets/D22/Validation/migration-report.json`。

## Figma、Blender 和 Meshy 的边界

Figma bridge、Blender MCP、Meshy MCP 和 Unity MCP 都是每个开发者本机运行的工具。仓库只保存可审阅的设计导出、Blender 源文件、Meshy 生成资产和任务记录，不保存 MCP 配置、访问令牌或桌面桥接会话。这样朋友克隆后能复现文件和导出流程，同时各自使用自己的服务凭证。

Meshy 资产已经在 `assets/d22/meshy/` 及其版本目录中，包含 GLB、PBR 贴图、输入图和提示词；Unity 使用的 FBX/纹理是由当前 Blender 场景发布出的运行时副本。

## 当前迁移状态

已在共享仓库路径完成 Unity 编译、场景发布和灯光烘焙：1,485 个源对象全部匹配，
缺失材质为 0，47 盏灯，3 张光照贴图。入口与舞台截图见
[`docs/previews/`](docs/previews/)。

原有 4 个 PlayCanvas 网页场景和 `player.js` 保留原路径；其中剧情、对话、喝酒和音乐
逻辑尚未改写为 Unity C#。当前 Unity 是 Livehouse 空间与行走控制基线，URP 的玻璃、
反射和明暗响应仍与 Cycles 不同。仓库审阅和后续模块拆分见
[`docs/REPOSITORY_REVIEW.md`](docs/REPOSITORY_REVIEW.md)。

## 网页原型待办（保留原 README）

原参考：<https://zhuanlan.zhihu.com/p/182995276>

- [ ] BGM
- [ ] 越喝酒越晕眩，喝多重开
- [ ] 音效：喝啤酒、鸽哨、蛐蛐、磨剪子磨刀
- [x] 背景随着音乐产生效果
- [ ] 对话，以及对话对摄像机的影响
- [x] outro
