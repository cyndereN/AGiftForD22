# D-22 资产目录

`assets/d22/` 现有 36 个 GLB：27 个 Meshy 生成/重贴图版本和 9 个建筑模块。
它们包含有效 glTF 2.0 模型数据，PBR 贴图可能嵌在 GLB 内，也可能同时保存在独立目录。
原始 GLB 可供重新装配；Unity 的五组 FBX 是场景发布结果。

- `meshy/`：鼓、音箱、吉他、麦克风、吧台、灯具、调音台等初始资产。
- `v9/meshy/`：砖墙、地砖、吧台、货架、杯架、凳子、相框、画廊灯、托盘、杂志、机箱。
- `v10/`、`v11/meshy/`：圆招牌与 Marshall 音箱迭代。
- `v15/meshy/`、`v16/meshy/`：货架/机箱重贴图尝试。v16 两个机箱结果被拒绝，保留为历史，未应用到当前场景。
- `architecture/`：9 个非 Meshy 建筑模块。
- `inputs/`、各版本 `inputs/`：生成模型的输入图。
- `prompts/`、各版本 `prompts/`：原始和重做提示词。
- `generation-manifest.json`、各版本 `manifest.json`：任务 ID、参数、结果与 SHA-256。
- `textures/`：木纹、海报等共享贴图，source JSON 记录来源。

完整机器可读清单在 [`assets/d22/catalog.json`](../assets/d22/catalog.json)。运行
`python3 scripts/assets/catalog.py` 会检查 GLB 头、外部资源和已知校验和并重建清单。
未恢复的任务记录标记为未知，不虚构来源或默认视为已验收资产。

## 源文件到运行时

Meshy GLB / 建筑模块 → Blender v18 装配 → `scripts/unity/export_d22.py`
→ `unity/D22Game/Assets/D22/Art/` → Unity 发布菜单生成材料、Prefab 与 Environment。

原始源资产位于 Unity Assets 外，不会重复导入到游戏。Blender 内的 81 张外部来源
图片已全部打包，且没有链接库；朋友下载当前 `.blend` 后无需访问旧工作区。
输入图与参考照片用于追溯，历史提示词保留原文。历史设计 handoff 内的 v9–v17
场景路径属于归档记录，本仓库以 v18 为当前美术源。

Meshy 的生成成功状态只表示已得到模型。部分清单明确标记 close-up fidelity、
薄片结构和 UV 接缝仍需迭代，不能据此宣称全部资产已达到最终游戏质量。
