# A Gift for D22

一款收集声音的增量休闲步行模拟。玩家走在已经消失的地方里，靠听到的声音把场景一点点还原回来。

An incremental casual walking simulator about collecting sound. You walk through places that are already gone, and the sounds you gather bring the rooms back.

## 这是什么

D22 是北京五道口一间 2012 年歇业的小酒吧，也是当年地下乐队过夜的地方。这支游戏不把那条街做成博物馆，而是让你走进三间被声音记住的房间：唱片店、胡同、Livehouse。

每一间都先是一处残缺的现场。高斯扫描留着地点消失前的样子，重建后的模型是它可以再被走过的样子。文案把你领到门边、柜台和天际线；你收集到的声音决定这间房如何响、下一步往哪走。

### 怎么玩

用 WASD 走动，鼠标看周围。声音是路标，也是收集品。

1. **唱片店。** 在格子和柜台之间走动，听这间店为什么还在。带走那杯酒，再走出门。
2. **胡同。** 门上的条子要求先把声音收齐：草丛里的蛐蛐、巷子里的磨刀吆喝、头顶飞过的鸽哨。收齐之后，门才开。
3. **Livehouse。** 走进 D22 的最后一夜。吉他、贝斯和鼓把舞台重新立起来。
4. **演出。** 名字还在人口里传来传去。终章把这些声音放在一起。

跳关只是调试用的捷径。正常游玩时，下一间房要等这一间的声音到齐。

## What it is

D22 was a small bar in Beijing's Wudaokou. It closed in 2012, and it was where the underground bands stayed until morning. This game does not turn that street into a museum. It lets you walk three rooms that the sound still remembers: a record shop, a hutong, and a livehouse.

Each place begins incomplete. A Gaussian scan keeps the site as it was when it disappeared. The rebuilt models are the same site once it can be walked again. The writing leads you to a door, a counter, a strip of sky. The sounds you collect decide how the room plays, and where you are allowed to go next.

### How it plays

Walk with WASD. Look with the mouse. Sound is both the guide and the thing you are collecting.

1. **Record shop.** Walk the bins and the counter. Listen for why the shop is still here. Take the drink, then leave through the door.
2. **Hutong.** The note on the door asks you to gather the lane first: a cricket in the grass, the sharpener's cry, a pigeon whistle overhead. The door opens when those three are together.
3. **Livehouse.** Step into D22's last night. Guitar, bass, and drums stand the stage back up.
4. **Performance.** The name is still being passed from mouth to mouth. The last scene plays the sounds as one.

Skipping a chapter is a debug shortcut. In a normal playthrough the next room waits until this room's sounds are in.

## 打开工程

仓库根目录就是 Unity 工程。用 Unity **6000.6.4f1** 打开。版本写在 `ProjectSettings/ProjectVersion.txt`。唯一保留的分支是 `main`。

```sh
git lfs install
git clone git@github.com:cyndereN/AGiftForD22.git
cd AGiftForD22
git lfs pull
git lfs fsck
```

已经克隆过的话：

```sh
git fetch origin
git switch main
git pull --ff-only
git lfs pull
```

如果模型或贴图打开后是以 `version https://git-lfs.github.com/spec/v1` 开头的文本，先再跑一次 `git lfs pull`，然后再开 Unity。

菜单 **D22 > Build > Windows x64** 或 **D22 > Build > macOS**。成品在 `builds/`，不进 Git。Windows 构建要连同 `builds/Windows` 里的 `_Data` 和 DLL 一起交出去，单个 exe 不能运行。

构建失败时保留 Console 里的第一条报错，并记下 Unity 版本和 `git lfs fsck` 是否通过。后面的报错常常只是第一条的后果。

从 Blender 更新 Livehouse 的说明在 `scripts/unity/README.md`。当前导出源是：

- Livehouse：`blender/source/D22_Balanced_Lighting_v18.blend`
- 唱片店：`blender/source/D22_RecordShop_v4_MeshyOnly.blend`
- 胡同：`blender/source/D22_HutongOutdoor_v4_LivedIn.blend`

Unity 里用 **D22 > Publish > Record Shop V4 Meshy Only** 和 **D22 > Publish > Update Art From Blender** 把导出写回正式场景。

## Open the project

The repository root is the Unity project. Open it in Unity **6000.6.4f1**. The version is recorded in `ProjectSettings/ProjectVersion.txt`. `main` is the only branch.

```sh
git lfs install
git clone git@github.com:cyndereN/AGiftForD22.git
cd AGiftForD22
git lfs pull
git lfs fsck
```

If you already have a clone:

```sh
git fetch origin
git switch main
git pull --ff-only
git lfs pull
```

An LFS model or texture must be a real binary. If it opens as text beginning with `version https://git-lfs.github.com/spec/v1`, run `git lfs pull` again before opening Unity.

Use **D22 > Build > Windows x64** or **D22 > Build > macOS**. Output goes to `builds/` and is ignored by Git. A Windows build needs the whole `builds/Windows` folder, including the `_Data` directory and the DLLs. The `.exe` alone is not a game.

When a build fails, keep the first Console error, the Unity version, and whether `git lfs fsck` passed. Later errors are usually fallout from the first one.

Livehouse export notes are in `scripts/unity/README.md`. The Blender files that still match the game are:

- Livehouse: `blender/source/D22_Balanced_Lighting_v18.blend`
- Record shop: `blender/source/D22_RecordShop_v4_MeshyOnly.blend`
- Hutong: `blender/source/D22_HutongOutdoor_v4_LivedIn.blend`

In Unity, **D22 > Publish > Record Shop V4 Meshy Only** and **D22 > Publish > Update Art From Blender** write those exports back into the scenes the game loads.
