# 仓库整理与迁移记录

日期：2026-10-03。基于原仓库 `ac574fa`，整理在 `codex/integrate-unity-assets` 分支。

## 原有内容

根目录 `index.html` 是入口，`player.js` 包含场景跳转、行走/视角、对话和喝酒等交互；
4 个 `scene_*.html` 是带嵌入式模型数据的 PlayCanvas 场景，每个约 18–22 MB。
`Model/` 保存酒瓶模型与贴图，`Carsick Cars - Carsick Cars/` 保存音乐，
`talks.txt` 是文字资料，`game-flow.svg` 是流程图。Windows 本地启动脚本原样保留。
原 README 的链接和待办也已保留。

网页各文件之间使用根目录相对路径。为保留当前入口和协作习惯，本次继续在根目录
运行网页，并以 `unity/D22Game` 作为独立子项目。后续要把网页收进 `web/` 时，
应单独修改全部模型/音乐/页面引用与部署入口，不与 Unity 美术发布混在一次改动中。

## 合入的数据

- Unity：Assets、Packages、ProjectSettings 和所有 `.meta`，固定编辑器/URP/MCP 版本。
- Blender：当前 v18 源文件、原灯光审阅图、便携性检查；81 张图片全部打包，无链接库。
- Meshy：27 个生成/重贴图模型版本、9 个建筑 GLB、PBR 贴图、输入图、提示词与任务清单。
- Figma：文件链接、版本化 SVG/PNG/JSON、发布脚本所需的图片及本机 bridge 设置说明。
- 参考资料：资产清单依赖的原照片、设计稿和来源索引。

历史 v9–v17 `.blend` 不复制为当前开发入口。历史 handoff 中保留的旧场景/验证路径
用于说明来源；当前可发布的源文件明确为 `blender/source/D22_Balanced_Lighting_v18.blend`。
Figma 原生文档仍由 Figma 云文件管理，仓库保存交接导出，并非 `.fig` 离线备份。

## 验证结果

- 新目录中 Unity 编译、发布、烘焙成功。
- 全部 1,485 个模型对象与源清单匹配，最大中心误差约 0.000002 米；缺失材质 0。
- 修复 FBX 导入整体旋转 180° 导致的模型/灯光错位；出生点命中地板，高度约 0.028 米。
- 47 个灯光、130 个碰撞组件、4 个协作场景、3 张光照贴图。
- 通过 Unity MCP 选择共享仓库实例，进入/退出 Play Mode，控制台错误数为 0。
- Figma bridge 实时探测成功，响应文件 key 与文档一致。
- 36 个 GLB 的格式、依赖和已知 SHA-256 通过检查。
- Unity Assets 无缺失 `.meta`、无重复 GUID，未将 Library/Temp/个人设置纳入 Git。

截图在 [`previews/`](previews/)，详细场景报告在
[`migration-report.json`](../unity/D22Game/Assets/D22/Validation/migration-report.json)。
以上是编辑器和场景验证，尚未做 Windows/Mac 安装包构建或长时间性能测试。

## Git 与后续协作

已安装本仓库 Git LFS hook，配置 Unity 文本序列化和本机 UnityYAMLMerge。
新源文件/模型/图片/音频/光照数据由 LFS 管理；现有网页二进制资产在这次提交中
转换为 LFS 指针，历史提交保持原样，不重写远端历史。两人克隆/拉取后运行
`git lfs pull`。旧历史中原本的大二进制不会因此自动缩小。

源文件锁定 `.blend`，程序员修改 Gameplay/C#，灯光负责人修改 Lighting。
Environment、Materials 和五个 Prefab 由发布脚本生成；重新发布后需要重烘焙。
当前美术运行基线偏暗，玻璃与反射仍使用 URP 近似；后续视觉调整在 Lighting 中进行。

下一步把网页玩法逐项移到 Unity：场景流程与存档、对话、喝酒效果、音乐/演出事件。
这些逻辑不会因合入网页文件而自动变成 Unity 玩法。建议分别在 Gameplay 场景和
Runtime 脚本中实现，每项用独立分支和可运行的验收步骤交付。
