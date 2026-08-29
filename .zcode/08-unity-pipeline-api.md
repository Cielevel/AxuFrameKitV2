# 08 · Unity Pipeline 本地 API 使用指南（AI Agent 直连 Editor）

> 2026-08-29 实测验证。本工程已安装 `com.unity.pipeline 0.5.0-exp.1`（见 manifest.json），
> Unity 编辑器运行时会启动一个 **仅回环（127.0.0.1）的 HTTP 服务**，任何本机脚本/AI Agent 都可以直接调用它驱动 Editor。
> 已验证：读状态、枚举命令、执行 `console` 命令均成功。**注意：本文件包含的用法可让 agent 实际修改工程，使用时保持谨慎。**

## 0. 已内化的子代理（优先派发，无需手写 curl）

本能力已封装为两个用户级子代理（`~/.zcode/agents/`，会话启动时加载）：

| 子代理 | 职责 | 边界 |
|---|---|---|
| `unity-pipeline` | 驱动运行中 Editor：recompile / run_tests / create_* / build / bake / audit / capture / console | 可执行（有副作用），按最小副作用纪律 |
| `unity-pipeline-observe` | 只读观测：状态 / Console / 测试清单 / 异步进度 | 只读，exec 白名单限只读命令 |

派发规则：Editor 运行中且需要"实际执行/验证"→ `unity-pipeline`；只需看状态/日志 → `unity-pipeline-observe`；Editor 未运行且需无头验证 → `unity-build`；纯日志分析 → `unity-debugger`。

## 1. 服务发现

Editor 启动后（[InitializeOnLoad] 自动启动），描述符写入：

```
Library/Pipeline/.unity-pipeline-port
```

内容（JSON）：`pid`、`port`、`projectPath`、`unityVersion`、`mode`（editor）、`startedAt`、`lastHeartbeat`、`evalToken`。
**端口动态分配**（本次实测 7800），每次读这个文件获取当前端口与 token。

## 2. 认证

所有请求带 `Authorization: Bearer <evalToken>`（token 用 `SecurityTokenManager` 管理，会轮换——以描述符里的最新值为准，服务端校验实时 token）。

## 3. API 端点

| 端点 | 方法 | 说明 |
|---|---|---|
| `/api/status` | GET | 服务状态：`settling`（启动导入/编译窗口）或 `ready` |
| `/api/editor_status` | GET | `compiling` / `domainReloadInProgress` / `playMode` / 项目路径 / 版本 |
| `/api/commands` | GET | **自描述命令清单**（实测 142 个），含名称/描述/参数 |
| `/api/exec` | POST | 执行命令（见下） |
| `/api/progress` | GET | 异步任务进度 |
| `/api/job`、`/api/job/cancel` | GET/POST | 后台任务（detached job）查询与取消 |

## 4. 执行命令（/api/exec）

```bash
TOKEN=$(python -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['evalToken'])")
PORT=$(python -c "import json;print(json.load(open('Library/Pipeline/.unity-pipeline-port'))['port'])")
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"command":"console","parameters":{},"timeout":15000}' \
  "http://127.0.0.1:$PORT/api/exec"
```

请求体：`{"command": "<命令名>", "parameters": {…}, "timeout": 毫秒, "job": false}`。
响应：`{"success": true, "command": …, "result": …, "executionTimeMs": …, "error": …}`。
启动后的"settle 窗口"内主线程命令会被 503 拒绝（可重试）；轮询 `/api/status` 等到 `ready` 再执行。

## 5. 对 Agent 最有价值的命令（142 个中的代表）

- **观测**：`console`（读 Console）、`editor_status`、`capture_game_view`/`capture_scene_view`（截图为 PNG）
- **编译/测试**：测试运行与取消（`cancel_tests` 对应的运行命令见 /api/commands）、`recompile_status` 类轮询
- **资产操作**：`create_script`、`create_asset`、`copy_asset`、`create_folder`、`find_assets`、`import` 系列
- **场景/对象**：`create_scene`、`create_gameobject`、`add_component`、`attach_script`、`create_prefab`(+variant)、`apply_prefab_overrides`
- **动画**：Animator/Timeline 的 layer/state/parameter/track/clip 全套
- **构建/烘焙**：`build`、`add_scene_to_build`、`bake_lighting`、`bake_navmesh`、`bake_occlusion_culling`（均异步+进度）
- **静态分析**：`audit`（Project Auditor 扫描）+ `audit_status`

完整清单每次以 `GET /api/commands` 实时为准。

## 6. 与 Unity CLI 的关系

- 本机已装 **Unity CLI 0.1.0-beta.3**（`C:\Users\cielevel\AppData\Local\Unity\bin\unity.exe`），当前仅含管理命令
  （editors/install/open/projects/auth/doctor…），**尚无** `command`/`eval`/`pipeline` 命令组——博客功能领先分发构建。
- 在 CLI 推出带 `command` 组的新 beta 前，直接按本文用 HTTP API 即可（功能等价，官方 CLI 本就是这层 API 的封装）。
- 两台机器各装 CLI；Pipeline 包随工程 git 同步。

## 7. 注意事项

- `-automated` 警告：交互模式启动 Editor 时服务会警告"模态弹窗可能打断自动化流"。仅提醒；要消除可用
  `-automated` 参数启动 Editor（如 `Unity.exe -projectPath <路径> -automated`）。
- 服务仅监听回环地址，token 门控 eval——不暴露局域网；`Library/` 在 .gitignore 中，token 不会入库。
- Play 模式/`--runtime <player>` 的监听默认关闭（仅 dev 构建手动开启）。
- 包为 experimental（0.5.0-exp.1），API 可能随版本演进。
