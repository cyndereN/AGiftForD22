# 本机工具接入

项目文件通过 Git/LFS 协作。控制软件的 MCP 服务、bridge、令牌由每台电脑独立安装。
已经有 MCP 配置时只合并对应服务块，保留其他设置。示例在
[`config/mcp.example.toml`](../config/mcp.example.toml)。

## 固定版本

- Blender `4.5.3 LTS`，MCP 服务 `mcp-for-blender==2.1.3`。
- Unity `6000.6.4f1`，Unity 包与服务 `mcpforunityserver==10.2.0`。
- Figma Console MCP `1.40.6`，使用该发行包生成的 Desktop Bridge。
- Meshy MCP `@meshy-ai/meshy-mcp-server@0.5.2`。

安装 Node.js/npm 和 uv；使用示例的 `uvx`/`npx` 时，会按指定版本下载到本机缓存。
这些服务不属于游戏运行时依赖。直接克隆并运行 Unity 项目无需 Meshy/Figma 账号。

## Blender

运行 `uvx mcp-for-blender==2.1.3 install-addon`，重启 Blender，启用
**Interface: MCP for Blender**。在 3D 视图 `N` 面板检查服务已启动。
打开 `blender/source/D22_Balanced_Lighting_v18.blend`。日常发布也可以直接使用
`scripts/unity/export_d22.sh`，不依赖 MCP。

## Unity

Hub 打开 `unity/D22Game`；Package Manager 会从锁定的 v10.2.0 Git 标签恢复插件。
项目内的 `D22LocalBridge` 会在编辑器启动后连接本机 bridge；需要重连时用
`D22 > Connect Local MCP`。如果同时打开两个 D22 副本，先确认目标实例是共享仓库，
避免把操作发到旧工作区。

## Figma

将自己的 `FIGMA_ACCESS_TOKEN` 提供给 MCP 进程（可在用户级配置的服务 `env` 中设置；
不要写进本仓库）。启动 MCP 后，Figma Desktop 里选择
**Plugins → Development → Import plugin from manifest…**，导入
`~/.figma-console-mcp/plugin/manifest.json`，然后在目标文件里运行插件。
此 manifest 由服务自动生成；不要复制一个缺少 `code.js` / `ui.html` 的孤立 manifest。
文件链接和已有设计导出见 [`design/figma/README.md`](../design/figma/README.md)。

## Meshy

将自己的 `MESHY_API_KEY` 提供给 Meshy MCP 进程。已下载的模型、贴图和任务记录
在 `assets/d22/`，查看和使用它们无需再次生成。新增模型先保存 GLB、贴图、输入图、
提示词和任务 ID，再运行 `python3 scripts/assets/catalog.py` 更新清单。
原始 GLB 可能包含嵌入式 PBR 贴图，不一定有独立 `_textures` 文件夹。

## 配置持久性与检查

Codex 的用户级 `~/.codex/config.toml` 可让后续窗口复用服务配置；桌面软件和对应
bridge 仍需运行。参考 [OpenAI 官方 MCP 文档](https://developers.openai.com/codex/mcp)。
新窗口检查 `codex mcp list` 或客户端 MCP 列表，分别读取 Blender 场景、Unity 实例、
Figma 文件状态和 Meshy 只读账户状态。Windows 的 npm 服务可使用 `cmd /c npx …`；
若桌面客户端找不到程序，请把示例 `command` 改成自己的完整路径。

上游说明：[Blender](https://github.com/ahujasid/mcp-for-blender)、
[Unity](https://github.com/CoplayDev/unity-mcp/tree/v10.2.0)、
[Figma](https://github.com/southleft/figma-console-mcp)、
[Meshy](https://github.com/meshy-dev/meshy-mcp-server)。
