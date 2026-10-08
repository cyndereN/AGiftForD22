# A Gift for D22

This repository contains the Unity project at its root. Open this directory in
Unity Hub with Unity **6000.6.4f1**. The current integrated game and environment
art are on `codex/integrate-dev-game-assets`; `dev/game` contains the earlier game
flow and scan scenes without the new 3D record shop and hutong.

If you already cloned the repository, update to the integrated branch before
opening Unity:

```sh
git fetch origin
git switch codex/integrate-dev-game-assets
git pull --ff-only
git lfs pull
git lfs fsck
git branch --show-current
```

The last command must print `codex/integrate-dev-game-assets`. If the branch is
not available locally, run `git switch --track origin/codex/integrate-dev-game-assets`.
Commit or stash your own local edits before switching branches.

## Fresh checkout

```sh
git lfs install
git clone --branch codex/integrate-dev-game-assets git@github.com:cyndereN/AGiftForD22.git
cd AGiftForD22
git lfs pull
git lfs fsck
```

Open the cloned repository root in Unity Hub. Install the Windows Build Support
module to build Windows x64, or Mac Build Support for macOS. The Unity editor
version is recorded in `ProjectSettings/ProjectVersion.txt`.

Use **D22 > Build > Windows x64** or **D22 > Build > macOS**. The output is under
`builds/` in this repository and is ignored by Git. For a Windows build, send
the entire `builds/Windows` output, including the `_Data` directory and DLLs;
the `.exe` alone is not a complete game.

On a fresh clone, an LFS managed FBX or texture must be a real binary file.
If one opens as text beginning with `version https://git-lfs.github.com/spec/v1`,
run `git lfs pull` again before opening Unity. The original scan files and
Gaussian Splatting package are already committed to this branch.

For a failed build, keep the first error in Unity's Console or Editor log. Also
record the current branch, the Unity editor version, and whether `git lfs fsck`
passes; later errors are often consequences of the first one.
