# 胡同 v4 · 生活道具与材质精修

基于 v3 的米制居民胡同，沿用弯巷布局、分户边界和视线高度。本版提高近景资产与建筑表面的细节密度，统一为灰砖、旧红漆、褪色绿、灰泥和门口暖灯的日常居住环境。

- Blender 工程：`../../blender/source/D22_HutongOutdoor_v4_LivedIn.blend`，贴图已打包。
- 构建脚本：`../../scripts/blender/build_hutong_outdoor_v4.py`。
- 评审图：`../../blender/verification/hutong-outdoor-v4/`，含巷口日景、住户门口、院落、转角、总览、尺度参照与傍晚。
- Figma：[胡同 v4](https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0?node-id=279-1589)。

## 本版资产

新增四种经过复核的 Meshy PBR 资产：空调外机、电表箱、低木凳、洗衣盆。使用统一缩放，不将整栋房屋交给生成模型。继续使用既有自行车、盆栽、纸箱、暖水壶，合计 8 种 Meshy 源模型、27 个场景实例。

本批共生成并贴图 6 项，实际消耗 180 credits。其中扫帚的穗头生成在杆中部，拖鞋生成了脚趾和脚踝，均未进入最终场景。扫帚改用 Blender 细化的竹柄与扎束纤维。原始生成文件与质检图保留用于追溯，见 `../../assets/hutong/meshy-v4/generation-manifest.json`。

## 建筑、材质与灯光

建筑继续使用 Blender 可编辑几何。砖墙、水泥路面、灰泥接入扫描贴图，按世界米制 UV 铺设，加入不规则墙脚潮色、轻微纵向水痕、地面修补边缘和墙边落叶。木门使用纵向木纹与褪色漆面。白天采用斜向日光与天空补光，傍晚另有门口实用灯状态。

扫描材质来自 Poly Haven 的 CC0 资产：

- [Brick Wall 08](https://polyhaven.com/a/brick_wall_08)
- [Brick Wall 006](https://polyhaven.com/a/brick_wall_006)
- [Concrete Floor 02](https://polyhaven.com/a/concrete_floor_02)
- [White Plaster 02](https://polyhaven.com/a/white_plaster_02)
- [Plaster Grey 04](https://polyhaven.com/a/plaster_grey_04)

各材质的作者、源文件 URL 与校验值保存在 `../../assets/hutong/pbr-v4/*/source.json`。

## 验证边界

主镜头高度 1.65 m，门高 2.1 m；巷宽截面最窄 2.6 m，保留 1.75 m 人体参照。建筑通路的 289 个采样点通过半径 0.30 m 的净空检查。20 张已使用的扫描贴图通过源 MD5 校验。

当前为 Blender 场景美术评审版，尚未导入 Unity。通路检查仅覆盖建筑，不代表物件碰撞、角色移动或交互已通过运行时测试。Livehouse 和唱片店室内工程未修改。
