# D22 双人开发工作流

## 固定的项目基线

- Unity Hub 打开仓库内 `unity/D22Game`，不是仓库根目录。
- 两人统一 Unity `6000.6.4f1`、URP `17.6.0`；Unity Package Manager 的 manifest 和 packages-lock 一并提交。
- MCP for Unity 的 Unity 包和 Python 服务均锁定 `10.2.0`。本机通过用户级配置自动启动服务，Unity 项目里的 `D22LocalBridge` 只启动本地编辑器 bridge。
- 当前美术源文件：`blender/source/D22_Balanced_Lighting_v18.blend`。`.blend` 保存在 Unity Assets 之外。
- 当前导出：`unity/D22Game/Assets/D22/Art/`。FBX、纹理、材料描述都有稳定路径，重复导出不应删除对应 `.meta`。

## 所有权和场景拆分

游戏入口是 `D22_Menu`，菜单 `D22 > Game > Open Main Menu` 打开后进入 Play Mode。四个扫描场景各自独立；`D22GameFlow` 保存跨场景的本次会话状态，`D22GameUI` 展示剧情和交互。

美术场景菜单 `D22 > Open Collaborative Workspace` 一起打开以下四个 Blender 协作场景：

1. `D22_Bootstrap`：加载入口，只负责按需加载后续场景。
2. `D22_Environment`：建筑、舞台道具、吧台与调音台、画廊装饰和灯具五组 Prefab。由美术发布脚本生成。
3. `D22_Lighting`：Unity 灯光、反射探针、光照探针、色调和烘焙设置。由指定的灯光负责人编辑。
4. `D22_Gameplay`：摄像机、有碰撞的行走控制器与后续可独立编写的交互挂点。朋友可独立维护；美术重新发布不会覆盖此场景。

每次改动前在聊天中确认谁负责哪个场景/Prefab。不要两个人同时修改同一个 `.unity` 或同一个大 Prefab；拆场景降低冲突概率，不能替代沟通。

## 日常协作

1. `git pull --ff-only`，然后 `git lfs pull`；确认没有正在进行的合并。
2. 从稳定分支开一个短期任务分支。例：`feature/interaction-door`、`art/stage-update`。
3. 只改分配给自己的场景、脚本和资源。在 Unity 内移动/重命名 Assets，保留 `.meta` GUID。
4. `.blend`、FBX、PSD 是二进制资源。远端支持 Git LFS locks 时，编辑前锁定：`git lfs lock blender/source/D22_Balanced_Lighting_v18.blend`。初次提交前先确保文件已被 Git/LFS 跟踪。
5. 保存；美术变更在协作工作区运行 `D22 > Validate Published Scene`，玩法变更从主菜单进 Play Mode 检查，再提交一项完整的小改动。模型和 `.meta`、材质和贴图、场景与引用需要一起提交。
6. 通过 Pull Request 互相看改动与一张运行截图；通过后合并，释放二进制锁。

不要提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.env` 或个人 MCP 凭证。已提供 `.gitignore`、LFS 属性、Unity 文本序列化和 SmartMerge 设置。光照贴图及其 `.meta` 应一起提交，否则朋友机器上只有模型而没有烘焙照明。

共享仓库已保留网页原型，并将 Unity、Blender、Meshy 和 Figma 资料放在各自目录。历史 v9–v17 Blender 场景不作为当前 Unity 源文件；需要考古时从原始工作区恢复。

首次入库集合是 `.gitignore`、`.gitattributes`、`unity/D22Game`、`scripts/unity`、`blender/source`、`assets/d22`、`design` 和本说明。朋友在 Windows 用 `scripts/unity/setup_collaboration.ps1`，你在 Mac 用 `scripts/unity/setup_collaboration.sh` 配置本机 LFS/SmartMerge；然后用 Hub 打开 `unity/D22Game`。无需为正常运行安装 Python。

## Blender → Unity 发布

Blender 负责几何、UV、贴图源和艺术布局；Unity 负责运行时材质、碰撞、灯光烘焙、相机与玩法。

1. 保存并锁定本次 `.blend` 源文件。
2. Mac 在仓库根目录运行以下命令：

```sh
scripts/unity/export_d22.sh
```

Windows PowerShell 用 `$env:BLENDER_BIN="C:\Program Files\Blender Foundation\Blender 4.5\blender.exe"` 指定 Blender，然后运行 `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/unity/export_d22.ps1`。

3. 等 Unity 导入结束。选择 `D22 > Publish > Update Art From Blender`。这个命令更新生成的模型、材质、Prefab 与 Environment；保留已有的 Lighting 和 Gameplay。首次发布才会生成灯光场景。如果确实需要从 Blender 重置灯位，先提交当前灯光，再选择 `D22 > Publish > Reset Lighting From Blender`。如果只改代码或交互，不要运行发布命令。
4. 使用 `D22 > Bake Lighting` 生成本次场景的光照数据，然后验证 Play Mode。
5. 一并提交本次源文件、导出的 Art、对应 Materials/Prefabs、两个生成场景、烘焙资产和验证结果。

稳定 GUID 来自保留 `.meta`，而不是重新删掉整个 Art 目录。五个生成的 Prefab 和 Environment 会在重新发布时重建，不要在其中保存手工 override；可把手工交互挂点与脚本放到 Gameplay 中的独立对象。源控制的布局回 Blender 修改。生成 FBX 不设为 LFS lockable，避免朋友克隆后只读文件阻止导出；用 `.blend` 源文件锁协调同一次美术发布。

## 材质与灯光的转换边界

已转换基础色、基础色贴图、切透、法线、金属度和光滑度打包纹理，以及灯泡发光材质。Blender 的 Roughness 转换为 Unity 的 `1 - Roughness`；金属度放 R，光滑度放 A。木地板的对象坐标投影转为显式 UV。

Cycles 的程序化微表面凹凸不会通过 FBX 原样带入，导出报告逐材质记录这一点。玻璃先使用透明 URP Lit 近似。URP 与 Cycles 灯光单位/响应不同，保留灯位、颜色与分组后，在 Unity 中重新校准；不要把 Blender 的瓦数直接当作 Unity 强度。

面积灯用于烘焙，舞台重点聚光灯保留 Mixed；静态环境使用 lightmap，动态角色通过光照探针采样。几何或灯位改动后重烘焙。只有生成检查通过且画面通过人工检查才算迁移完成。

## 本机控制与朋友接入

本机 Unity MCP 已加入 `~/.codex/config.toml`；打开 D22Game 后 bridge 自动启动。新 Codex 窗口可用 Unity MCP；编辑器菜单 `D22 > Connect Local MCP` 可手动重连。

朋友执行 `uv tool install --python 3.11 mcpforunityserver==10.2.0`，在其自己的 MCP 客户端配置 `mcp-for-unity --transport stdio`。不要复制你的全局 config，因为它含其他服务的凭证。每人的服务只连自己本机 Unity；Git 共享项目数据，不共享正在运行的 MCP 会话。

Play Mode：WASD 行走，按住鼠标右键转头，Shift 快走，Space 跳跃。这是 Blender 空间的行走控制；扫描空间沿用自由相机。游戏入口还支持 E 交互、1 喝酒、Esc 暂停。

## 参考依据

- Unity 3D 模型格式：https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/models/creating-dccassets/3d-formats
- Unity 多场景工作流：https://docs.unity.com/en-us/engine/6000.5/manual/working-with-scenes/multi-scene-editing
- MCP for Unity：https://github.com/CoplayDev/unity-mcp/tree/v10.2.0
