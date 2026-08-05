# 项目约定（Agent 必读）

## Git 提交规范

本仓库由单人维护，直接提交到 `main`，不使用功能分支 / PR 流程。

### Commit Message

采用 Conventional Commits，类型用英文，正文用中文。

```
<type>: <中文标题>

- 要点 1
- 要点 2
```

类型：
- `feat:` 新功能
- `fix:` 修复
- `docs:` 文档
- `refactor:` 重构
- `chore:` 构建、工具链、配置
- `test:` 测试
- `ci:` CI 工作流

### 提交粒度

一个逻辑变更一次提交。多个独立改动不要混在同一个提交里；涉及大特性时按功能拆分。

### 提交前检查

- 运行 `dotnet build FFXIV2GO.slnx`
- 运行 `dotnet test`
- 检查 `git status` / `git diff`，只暂存预期的文件，不提交构建产物（bin/obj/publish）、日志、config.ini 等。

## 开发命令

- 构建：`dotnet build FFXIV2GO.slnx`
- 测试：`dotnet test`
- 发布单文件：`dotnet publish src/FFXIV2GO/FFXIV2GO.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true`

## 发布

- 打 `vMAJOR.MINOR.PATCH` tag 触发 CI 自动构建并发布到 GitHub Release（latest）。
