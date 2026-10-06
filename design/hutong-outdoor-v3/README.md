# 胡同 v3：被生活反复修改的居住街巷

本轮按用户 2026-10-04 的纠正，改为 **Blender 建筑与路面 + Meshy 生活道具**。v1 / v2 的全 Meshy 仿古立面方案停止作为制作依据。年代暂按 **2003–2006**，地点为虚构北京居住街巷；不是对真实 D-22 地址的复刻。

## 本轮实际落地

- 新建独立 `blender/source/D22_HutongOutdoor_v3_Residential.blend`；既有唱片店 v4、livehouse 的源文件与 Unity 场景保持本轮开始时的内容。`preservation-check.json` 记录 SHA-256 对比。
- 米制场景：人形 1.75m，玩家视高 1.65m，门高 2.10m、宽 1.02m；巷道典型横断面 2.60–4.05m。房檐约 2.9m，普通瓦顶仅升高约 0.52m。
- 连贯建筑体量替代薄立面阵列。院墙错进错出，中段向侧面错开；入口不能直视现场门口，经过小空地、再次收窄和直角转弯才揭示。
- 15 个屋顶单元包含老瓦、补瓦、平顶、彩钢棚、带管线后加层；其中 6 个非传统瓦顶，占 40%。
- 连续旧水泥路面、不同年代的修补带、门前小块旧砖、井盖、排水篦和细裂纹。修补块作为表面，不抬高或随机转动地砖。
- 门牌、褪色对联、电表、门铃、塑钢窗、防盗栏、空调外机、排水管和架空线；门边有低凳、桶、鞋、扫帚。三辆自行车使用同一 Meshy 源资产，以真实尺寸、不同停放朝向组合，尚不是三个不同车型。
- 红门位于住户侧边入口，降低饱和度并纳入住户尺度。现有唱片店和 livehouse 只出现外门连接提示，不在本文件重做室内。

## 文件与检查

- 构建：`scripts/blender/build_hutong_outdoor_v3.py`
- 场景：`blender/source/D22_HutongOutdoor_v3_Residential.blend`（贴图已打包）
- 实际渲染：`blender/verification/hutong-outdoor-v3/`
- 尺度与路线：`layout.json`；建筑通路检查：`qa.json`
- Meshy 源文件及哈希：`mesh-provenance.json`
- Figma：<https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0/?node-id=273-1524>

`10 Scale reference 1.75m` 是可开关的人形参照 Collection，默认不参加正常渲染。白天用于检查尺度，傍晚用于检查门灯与天空的层次。

验证范围：脚本沿路线检查半径 0.3m 的玩家与建筑包围盒的关系，并输出人视角渲染。此检查不等同于 Unity 实机移动测试，也不覆盖声音、交互触发或所有道具的碰撞。

## 玩家路线（设计；互动尚未接入）

唱片店外口 → 较宽入口 → 2.65m 住户窄段 → 向右错身 → 门前小空地 → 2.60m 再收窄 → 向东转弯 → 已有 livehouse 外门。可在住户门牌、小桌和收音机声音线索前停留，也可直接继续。门后分别加载既有室内场景。

未来 Unity 接入只新增独立外景，不覆盖现有唱片店、livehouse 或当前 `D22_Hutong`；在路线审阅完成后再实现门口出生点、触发区与声音遮挡。

## 研究依据与设计边界

- 北京旅游网《北京的胡同来历》：胡同作为街巷与两侧四合院的关系。<https://www.visitbeijing.com.cn/article/47QqZYt2dVB>
- 北京市文物局《北京皇城保护规划》：胡同走向、尺度及低层格局的保护。<https://wwj.beijing.gov.cn/bjww/362679/362686/622504/index.html>
- Peter Hessler, *Hutong Karma*, 2006：转角、共享入口、自行车和巷道公共生活的当时观察。<https://www.newyorker.com/magazine/2006/02/13/hutong-karma>

上述来源支持空间类型与生活层；文中的 2.60–4.05m 等数值是本项目的制作标尺，不能当成所有北京胡同的统一历史尺寸。现代街景只能参照空间，不能不加筛选地作为 2003 年物件证据。
