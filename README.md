# FFXIV2GO

> 注意：游戏中会频繁对存储介质进行读写操作，若性能不足可能导致游戏频繁卡顿，如使用外置存储设备部署本项目，**强烈建议使用移动硬盘或使用固态颗粒的U盘部署此工具！**

FFXIV2GO 是一款用于快速配置和卸载 XIVLauncherCN 游戏环境的 **Windows 桌面应用**（C# WPF，.NET 10），用于在网吧等特定环境持久化保存游戏数据，避免每次重复配置。

## 特性

- **便携即用**：自包含单文件 exe，U盘/网盘即插即用，无需预装 .NET 运行时
- **部署位置灵活**：exe 可放在任意目录（如 `D:\CloudUDrive\1NBhf6cVm`），不要求盘符根目录
- **可配置应用下载**：`apps.json` 清单驱动（XIVLauncherCN/Soil、ACT、Clash Party、Snipaste、Everything），支持固定地址与 GitHub 最新版自动解析，直接编辑 exe 旁的 `apps.json` 即可增删应用
- **应用管理页**：随时安装/卸载便携应用（卸载即删除应用文件夹），不限于初始化阶段；应用运行中卸载会先提示关闭
- **全局浮动按钮**：右下角一键安装/卸载（安装成功后自动启动勾选的应用），完成弹 toast 通知
- **状态指示**：仪表盘与向导页以图标展示初始化/安装/卸载状态（绿对号/红错号/灰点点）
- **检查更新**：关于页比对 GitHub 最新 Release 并一键跳转下载
- **深色/浅色主题**：跟随系统或手动切换，运行时即时生效
- **中英双语**：首次启动询问语言，随时可在设置中切换
- **系统托盘**：关闭窗口可选最小化到托盘（可设置不再询问），托盘常驻
- **完整日志**：分级日志（Debug/Info/Warn/Error）写入部署根 `logs\ffxiv2go.log`，设置页可直接打开
- **一键完成**：环境初始化备份、目标机安装挂载、卸载还原、空间清理

## 使用说明

### 准备工作

> 请在已经配置好客户端和插件的计算机上运行**初始化**。**此操作只需要执行一次！** 初始化时请保持稳定的网络连接，必要时使用魔法上网。

### 文件结构

```text
X:\  部署根目录（exe 所在任意文件夹）
├── FFXIV2GO.exe
├── apps.json              应用下载清单（可编辑）
├── config.ini             FFXIV 路径、语言、主题、日志级别、关闭行为、自动启动应用配置
├── logs\                  运行日志
├── inst\                  环境安装包
├── conf\                  配置文件夹
│   ├── FINAL FANTASY XIV - A Realm Reborn\   游戏配置
│   ├── XIVLauncherCN\                        启动器配置
│   └── mods\                                 Penumbra mod
└── apps\                  软件（按 apps.json 下载，均可随时卸载）
```

### 运行顺序

1. **初始化**（一次性）：备份游戏配置、XIVLauncherCN、mod；勾选并下载应用；下载运行库到部署根。重复执行会提示重新初始化（清空全部数据作为新环境）。
2. **安装**：在目标机器上把部署根的配置挂载到游戏与启动器目录（目录联接），安装运行库，并在**桌面创建快捷方式**（已装应用、FFXIV2GO、部署根文件夹）。未初始化时会提示先执行初始化。也可直接点击右下角浮动按钮一键安装/卸载。
3. **卸载**：解除本机目录联接、恢复原始配置，并删除桌面快捷方式
4. **清理**：删除部署根内 log/old 文件，释放空间

### 应用管理

左侧导航进入「应用」页：列出 `apps.json` 中全部应用与安装状态，可随时**安装**（下载+解压，带进度）或**卸载**（删除应用文件夹，先检测是否运行中）。用于初始化后增补/清理软件。

### 查看日志

 `logs\ffxiv2go.log` 记录启动、配置、步骤执行、下载、目录联接、应用安装/卸载与异常，日志级别可在设置中调整（Debug/Info/Warn/Error）。设置页可**打开日志**或**打开日志文件夹**；未处理异常同时写入 `%TEMP%\ffxiv2go-crash.log`。

### 设置

设置页可配置：FFXIV 路径（浏览/打开文件夹）、语言、主题、日志级别、关闭窗口行为（是否询问 + 退出或最小化到托盘）、**自动启动应用**（勾选后安装完成自动启动）；并可直接打开部署根目录、配置文件与日志。

## 系统要求

- Windows 10 / 11
- 运行需要管理员权限（启动时自动提权）
- 部署建议使用移动硬盘或固态颗粒 U盘

## 开发构建

- 需要 .NET 10 SDK
- 本地构建：`dotnet build FFXIV2GO.slnx`
- 单元测试：`dotnet test`
- 发布单文件：`dotnet publish src/FFXIV2GO/FFXIV2GO.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true`
- 打 `v*` tag 自动触发 CI 打包并发布到 GitHub Release（最新版本 Release 即 GitHub "Latest"，固定地址 `https://github.com/Lograthmic/FFXIV2GO/releases/latest/download/FFXIV2GO.zip` 始终下载最新版）
- 详细需求说明见 [prompt.md](prompt.md)

## License

MIT
