# 网页 → Unity 迁移

## 数据与实现边界

原仓库中的 `serve.bat` 只是调用 PowerShell 静态网页服务器。真实原型是 `index.html`、`player.js`、4 个 SuperSplat HTML 及模型/音乐。原文件完整保留在 `legacy/web/`，其内部相对路径保持一致。

Unity 使用原生 C# 主流程，不启动浏览器、不嵌入网页。主线按原型顺序进入唱片店，再接胡同、原始 Live House 扫描、Blender 重建 Livehouse 与演出空间。原型缺少胡同之后的关卡连接；Unity 添加了 E 交互出口与主菜单空间漫游，作为可继续开发的连接基线。

已迁移的数据：中英开场、六片海报、唱片店介绍、店主对话、酒的叙事、三项喝酒选择、酒瓶模型、原音乐、4 个扫描。原网页的三项酒后效果只有 `alert` 文字；Unity 将其实现为实际音频滤波/失真/混响与 URP 调色。镜头呼吸默认关闭，可在暂停菜单关闭“减少镜头晃动”后体验。喝酒冷却为 3 秒，第 5 口触发休息/返回菜单。

尚未在旧原型实现的吉他/鼓/贝斯技能不当作已完成内容。扫描本身没有碰撞几何，继续使用有边界的自由相机；不能把它视作可编辑 Meshy/Blender 模型。Blender Livehouse 单独保留真实模型、碰撞和烘焙光照。

## Gaussian 资产可复现流程

1. 原始网页 → `python scripts/scans/extract.py`，恢复 SOG，不改变任何高斯点，SHA-256 和数量写入 `assets/scans/manifest.json`。
2. `npm ci --prefix scripts/scans` 安装锁定的 `@playcanvas/splat-transform 3.9.0`。
3. `node scripts/scans/convert.cjs` → 本机忽略目录 `work/scans/*.ply`。
4. Unity **D22 → Game → Import Scans From Converted PLY** → `Assets/D22/Scans/` 原生资源。

普通开发者只需要 `git lfs pull`，不需要重新转换。4 个源扫描合计 5,888,173 个点，保持原始数量和 DC 色彩。高斯数据以独立 `.bytes` 用 LFS 存储；Unity `.asset` 描述和 `.meta` 保持文本。

嵌入插件 `org.nesnausk.gaussian-splatting` 为上游 **v1.1.1 / 9310dce438da726244ace17eaf6f768826435fa4**，MIT 许可，源码随仓库保存。插件未修改。官方支持 Metal、D3D12/Vulkan；本项目固定 Mac Metal 与 Windows D3D12，URP RenderGraph、MSAA 关闭。参见 [插件文档](https://github.com/aras-p/UnityGaussianSplatting/blob/v1.1.1/docs/render-pipeline-integration.md)。

扫描坐标从网页的 Z 轴旋转 180°，再作 Z 镜像到 Unity；对应世界点 `(x,y,z) → (-x,-y,-z)`。原型出生相机位于零点，朝网页 +Z，即 Unity -Z。重建场景使用自己的米制坐标，场景切换不复用扫描出生坐标。

## UI 与协作

当前 UI 使用分辨率独立的 Unity IMGUI 原型实现，统一虚拟画布 1440×900；字体为 Noto Sans SC，许可随字体提交。对话内容在 `Assets/D22/Story/story.json`，可脱离脚本修改。`scripts/scans/extract-story.cjs` 仅供从原型重新提取；不要用它覆盖后续已编辑的 Unity 剧情。

菜单与扫描场景已生成并提交。生成工具发现已有扫描场景会保留它，避免覆盖同伴调整过的出口和相机；主菜单资源引用由生成工具刷新。美术发布只更新 Blender Environment，不会重置游戏入口或替换玩法场景。

## 验证

本机为 macOS，Windows 的 GPU 渲染与输入需要朋友在 Windows 机器最终验收。具体自动和手动验证结果在 `docs/validation/`；不要把跨平台配置检查等同于已经在 Windows 实机测试。
