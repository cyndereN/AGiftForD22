# 唱片店 v3：胡同里，听完这面再走

美术方向：2000 年代北京胡同独立唱片店。窄门面、旧灰泥、氧化红墙裙、旧木家具、纸张、CD 与黑胶、暖色试听台灯；乐队照片与小刊物带出自由、文艺和摇滚的生活气息。

## 场景与交付

- 房间 4.30 × 8.20 × 2.85 米。入口为新到唱片台，左侧翻碟与 CD 墙，右侧为唱片机试听桌椅，后方为柜台和通向胡同的门。
- 105 个静态 Meshy 模型实例，24 个源 GLB。另有一个新生成的 Meshy 剧情酒瓶，实际高度 29 厘米。
- 可见模型全部来自 Meshy。Blender 只执行导入、摆位、尺寸适配、材质连接、既有网格的 UV 调整、灯光和相机设置。Unity 添加不可见碰撞、行走与剧情锚点。
- 乐队图像使用已有 Meshy 图像生成任务 `01a0d7e5-8314-7177-936c-757f8c1de777` 的素材，贴在既有 Meshy 相框网格上。
- 不符合类型的地板重试模型与海报模型已标记为拒绝使用；保留任务记录，没有混入最终场景。

Blender：`blender/source/D22_RecordShop_v3_MeshyOnly.blend`。

Unity：`unity/D22Game/Assets/D22/Scenes/D22_RecordShopV3.unity`；剧情入口 `D22_RecordShop.unity` 已同步，旧版保留为 `D22_RecordShop_PreV3.unity`。

[Figma 美术与布局](https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0/?node-id=255-1322) · [Figma Unity 实机预览](https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0/?node-id=255-1392)

渲染：`blender/verification/recordshop-v3/` 下的 `entry.png`、`listening.png`、`cash.png` 是 Blender Cycles 渲染；`unity.png` 是 Unity URP 实时相机截图。两端共用布局，实时灯光经过单独校准，实时阴影与离线渲染仍有差别。

## 来源与验证

`layout.json` 保存源文件及 SHA-256、105 个实例的实际包围盒、相机和访问路线。生成任务记录位于 `assets/recordshop/v3/`，复用资产的原始记录位于 `assets/recordshop/v2/` 与 `assets/d22/`。

`unity-qa.json` 核对模型数量、材质/贴图、Meshy 导出路径、源包围盒与实际 Unity 坐标、缺失脚本，以及半径 21 厘米胶囊的 246 个路线采样点。`playmode-qa.json` 记录实际剧情场景内 CharacterController 沿路线移动的结果。

`pickup-qa.json` 已验证 Meshy 酒瓶底部与柜台 0.94 米台面重合、模型高 0.29 米，并在 Play Mode 通过 `TryInteract` 成功拿取。`unity-pickup.png` 保存柜台与酒瓶实机近景。行走和拿取检查通过调用实际运行组件执行；没有覆盖完整键鼠剧情流程或发布版构建。

## 重建

从仓库根目录执行：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python scripts/blender/build_recordshop_v3_meshy_only.py
/Applications/Blender.app/Contents/MacOS/Blender --background blender/source/D22_RecordShop_v3_MeshyOnly.blend --python scripts/unity/export_d22.py -- --destination Assets/D22/Art/RecordShopV3 --prefix RS3 --manifest recordshop-v3-export.json --scene D22_RecordShop_v3_MeshyOnly
cp design/recordshop-v3/layout.json unity/D22Game/Assets/D22/Art/RecordShopV3/Data/layout.json
/Applications/Blender.app/Contents/MacOS/Blender --background --python scripts/blender/prepare_recordshop_meshy_bottle.py
/Applications/Blender.app/Contents/MacOS/Blender --background blender/source/D22_RecordShop_MeshyBottle.blend --python scripts/unity/export_d22.py -- --destination Assets/D22/Art/RecordShopV3/Wine --prefix RS3_Wine --manifest wine-export.json --scene D22_RecordShop_MeshyBottle
```

在 Unity 执行 `D22 > Publish > Record Shop V3 Meshy Only`，然后执行 `Validate Record Shop V3` 和 `Capture Record Shop V3`。编辑器默认从主菜单进入 Play；主菜单剧情继续加载已更新的 `D22_RecordShop`。Figma 使用 Bridge 写入实际渲染和 `layout.json` 的平面布局。
