# A Gift for D22

一个围绕 D-22、唱片店、胡同与现场音乐的桌面交互体验。**主项目是 Unity，支持 Windows 和 macOS。** 根目录的 `OpenUnity.bat` / `OpenUnity.command` 只是打开编辑器的快捷入口，不是游戏本身。

## 第一次打开

两人都安装 **Unity Hub + Unity 6000.6.4f1**、Git 和 Git LFS。朋友在 Windows 安装 Windows Build Support；Mac 开发者安装 macOS Build Support。不要各自升级 Unity 或包版本。

```sh
git clone git@github.com:cyndereN/AGiftForD22.git
cd AGiftForD22
git lfs install --local
git lfs pull
```

首次整合尚在 `codex/integrate-unity-assets` 分支。该分支推送后、合入 main 之前，朋友需要先运行：

```sh
git fetch origin
git switch --track origin/codex/integrate-unity-assets
git lfs pull
```

- **macOS**：运行 `bash scripts/unity/setup_collaboration.sh`，然后双击 `OpenUnity.command`。
- **Windows PowerShell**：运行 `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/unity/setup_collaboration.ps1`，然后双击 `OpenUnity.bat`。无需 Python、Node、Blender 或 MCP 就能运行已有 Unity 项目。
- 也可以在 Unity Hub 的 **Add project from disk** 中选择 **`unity/D22Game`**，不是仓库根目录。
- 自定义编辑器安装位置时设置 `UNITY_EDITOR` 为 Unity 可执行文件的完整路径，或直接从 Hub 打开。

Unity 导入结束后，执行 **D22 → Game → Open Main Menu**，点击 Play。从主菜单开始故事，或选择空间漫游。WASD 移动、按住右键转向、E 交互、1 喝酒、Esc 打开菜单。默认开启减少镜头晃动，可在暂停菜单调整。扫描空间使用与网页一致的自由相机，Blender 重建空间使用有碰撞的行走控制。

## 游戏与资源在哪里

```text
unity/D22Game/                 主游戏工程：Assets + Packages + ProjectSettings
  Assets/D22/Scenes/           主菜单、4 个扫描空间、Blender 分层场景
  Assets/D22/Scripts/          C# 游戏流程、交互、UI、导入和构建工具
  Assets/D22/Story/            中英剧情数据
  Assets/D22/Scans/            Unity 原生 Gaussian Splatting 数据
  Assets/D22/Art/              Blender 发布模型、PBR、酒瓶
  Assets/D22/Audio/            原型音乐资源
blender/source/               可编辑 .blend 源文件（LFS）
assets/d22/                   Meshy 原始 GLB、贴图、生成输入与记录
assets/scans/                 从网页恢复的原始 SOG 扫描及哈希
design/                      Figma 导出、布局、交接资料
references/                   设计文档、参考照片
scripts/                      跨平台设置与资源转换工具
docs/                         协作、资产、连接及验证说明
legacy/web/                   保留完整相对路径的网页原型归档
OpenUnity.bat / .command       Windows / macOS 编辑器快捷入口
```

Unity 的 **Assets** 是游戏使用的资源；根目录 **assets/** 与 **blender/** 是创作源文件与生成记录。源资产也要提交，但不塞进 Unity 的 Assets，以免 Unity 依赖本机 Blender 导入 `.blend`、重复导入历史模型或把参考资料打进游戏。

## 这次改动怎么 push

当前本机分支为 `codex/integrate-unity-assets`。在仓库目录执行：

```sh
git status
git push -u origin codex/integrate-unity-assets
```

Git LFS 的 pre-push hook 会上传本次提交引用的大资源，再上传 Git 提交；**不需要手动把资源另传一次**。首次上传资源较多。推送成功后在 GitHub 创建该分支到 `main` 的 Pull Request，两人确认后合并。没有执行 push 时，朋友还看不到你本机的迁移成果。

日常流程：先保存 Unity 场景，`git status` 检查文件，从最新 main 创建自己的分支，例如 `feature/recordshop-dialogue`。完成后 `git add <相关文件或目录>`、`git commit -m "..."`、`git push -u origin <你的分支>`，通过 PR 合并。不要两个人同时修改同一个场景文件；Environment、Lighting、Gameplay 已拆开。

提交 `.meta`、`Packages/manifest.json`、`Packages/packages-lock.json` 和 `ProjectSettings`。不要提交 `Library`、`Temp`、`Logs`、`UserSettings`、构建产物或个人服务密钥。详细操作见 [双人协作](docs/UNITY_WORKFLOW.md)。

## 构建与创作

- 独立游戏：Unity 菜单 **D22 → Build → macOS / Windows x64**；结果写入仓库的 `builds/`，不进 Git。玩家不需要 Unity、Blender 或 MCP。
- Gaussian 渲染固定使用 **Mac Metal / Windows DX12**。Windows 显卡/驱动须支持 DX12；不支持 DX11。Unity 高斯插件已随工程固定版本提交，朋友不用重复安装。
- Blender → Unity：Mac 运行 `scripts/unity/export_d22.sh`；Windows 设置 `BLENDER_BIN` 后运行 `scripts/unity/export_d22.ps1`。然后在 Unity 执行 **D22 → Publish → Update Art From Blender**。更新几何后重新烘焙灯光。
- Figma bridge / Blender MCP / Meshy MCP / Unity MCP 是开发工具，每台电脑单独连接，不是游戏运行依赖。见 [连接说明](docs/MCP_SETUP.md)。Figma 文件引用与可审阅导出在 [design/figma](design/figma/README.md)。
- Meshy 原模型确实已保存：36 个 GLB 中包含 27 个 Meshy 模型版本和 9 个结构模块，含输入、贴图与来源记录。见 [资产说明](docs/ASSET_CATALOG.md)。

迁移范围、验证结果和剩余差异见 [游戏迁移说明](docs/GAME_MIGRATION.md)。旧网页保留在 `legacy/web`，可运行其中的 `serve.bat`，或在该目录运行 `python3 -m http.server 8091 --bind 127.0.0.1`；它不再是主入口。
