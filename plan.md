# FFXIV2GO 重构计划：BAT → C# WPF UI 应用

## 一、项目背景与意图

FFXIV2GO 原为 4 个 BAT 脚本（Init/Setup/Uninstall/Clean）+ GitHub Actions 发布工作流，用于在网吧/网盘等非持久化环境快速配置和卸载 XIVLauncherCN 游戏环境。核心思路：在已配置好的电脑上把游戏配置、XIVLauncherCN 配置、mod、运行依赖持久化备份到部署根 `[BASE]`，目标机器通过目录联接（junction）挂载，实现配置随身携带。

本计划将整个项目重构为**带 UI 的 Windows 桌面应用**，完全替换 BAT 脚本。

## 二、技术决策

| 项 | 决策 |
|---|---|
| 框架 | C# WPF，.NET 10（与项目部署的运行时同源） |
| 发布 | 自包含单文件 exe（win-x64），U盘/网盘即插即用，不依赖预装运行时 |
| UI 形态 | 主面板（四操作卡片）+ 每个操作独立的步骤向导页 |
| BAT | 完全替换移除，不再随包发布 |
| 权限 | app.manifest 声明 requireAdministrator，启动即提权（替代 VBS 提权） |
| 深色模式 | WPF-UI 控件库（Fluent 风格），ApplicationThemeManager 支持 跟随系统/浅/深 三档，运行时切换 |
| 多语言 | RESX 资源文件，初始支持 中文(zh-CN) + 英文(en)，默认跟随系统区域，设置页可切换并持久化 |
| 本地构建 | .NET 10 SDK 10.0.302 已装于 C:\Program Files\dotnet（shell 未加入 PATH，用全路径调用） |

## 三、仓库结构

```
FFXIV2GO/
├── FFXIV2GO.sln
├── plan.md
├── prompt.txt                      项目意图与工作流程说明（更新为 UI 版）
├── src/FFXIV2GO/
│   ├── FFXIV2GO.csproj
│   ├── App.xaml(.cs)               启动、全局异常处理、主题初始化
│   ├── app.manifest                管理员清单
│   ├── MainWindow.xaml(.cs)        主面板：初始化/安装/卸载/清理/设置 五个入口
│   ├── Resources/
│   │   ├── Strings.resx            默认英文文案
│   │   └── Strings.zh-CN.resx      简体中文文案
│   ├── Services/                   与 BAT 逻辑一一对应的核心层
│   │   ├── DeploymentRoot.cs       部署根 = Environment.ProcessPath 所在目录
│   │   ├── AppConfig.cs            config.ini 读写（PATH_FFXIV、语言、主题）
│   │   ├── LocalizationService.cs  多语言：INPC + this["Key"] 索引器，切语言即时刷新
│   │   ├── ThemeService.cs         主题切换：跟随系统/浅/深
│   │   ├── DownloadService.cs      HttpClient + 进度回调（VC/Net/7z/ACT，含 GitHub 连通检测）
│   │   ├── ArchiveService.cs       SharpCompress 解压 7z（替代 tar）
│   │   ├── JunctionService.cs      P/Invoke 创建/删除目录联接（替代 mklink /J 与 rmdir）
│   │   ├── JsonConfigService.cs    Penumbra.json 读写（System.Text.Json）
│   │   ├── RuntimeInstaller.cs     静默安装 VC++/.NET（Process + /quiet /norestart）
│   │   ├── DiskService.cs          文件遍历/大小换算（log/old 清理、释放空间统计）
│   │   └── StepRunner.cs           步骤执行器：异步顺序执行、状态/日志/进度事件推送 UI
│   ├── ViewModels/                 CommunityToolkit.Mvvm
│   └── Views/                      向导页 + 设置页（UserControl）
└── .github/workflows/release-scripts.yml   改为 dotnet publish + 打包发布
```

NuGet 依赖：`CommunityToolkit.Mvvm`、`SharpCompress`、`WPF-UI`

## 四、目录结构（部署根 [BASE]）

```text
[BASE]\  部署根（exe 所在任意目录，不要求盘符根目录，适配网盘如 D:\CloudUDrive\1NBhf6cVm）
├── FFXIV2GO.exe
├── config.ini               FFXIV 路径、语言、主题配置
├── inst\                    安装包
│   ├── VC_redist.x64.exe
│   └── windowsdesktop-runtime-10.0-win-x64.exe   .NET 10（aka.ms 最新直链）
├── conf\                    配置文件夹
│   ├── FINAL FANTASY XIV - A Realm Reborn\   游戏配置
│   ├── XIVLauncherCN\       启动器配置
│   └── mods\                Penumbra mod
└── apps\                    软件
    ├── XIVLauncherCN\
    └── ACT\
```

## 五、四个操作的工作流程

### 1. 初始化 Init（一次性）
GitHub 连通检测 → 创建 inst/conf/apps → 文件夹选择器选 FFXIV 路径并校验 `game\My Games\FINAL FANTASY XIV - A Realm Reborn` → 复制游戏配置→conf → 复制 XIVLauncherCN→conf（无则"作为新环境"分支）→ 解析 Penumbra.json 复制 mods→conf\mods → 下载 VC++/.NET10→inst → 下载+解压 XIVLauncherCN→apps 并删 7z → 下载 ACT→apps\ACT_Installer.exe 并建 apps\ACT →（可选）执行清理。

下载地址：
- VC++：https://aka.ms/vc14/vc_redist.x64.exe
- .NET 10：https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe（版本免维护）
- XIVLauncherCN：github.com/ottercorp/FFXIVQuickLauncher/releases/download/6.4.6-15/XIVLauncherCN-win-Portable.7z
- ACT：https://cafemenu.xivcdn.com/act/update/download?channel=release&variant=sfx&version=latest

### 2. 安装 Setup
读/填 FFXIV 路径（config.ini）→ 校验游戏目录 → 原目录改 `.old` → 联接 conf\FINAL FANTASY XIV - A Realm Reborn → XIVLauncherCN 改 `.old` + 联接 → 建桌面 `caches`（Lightless，替代弃用的 MareSynchronos）→ 改 Penumbra ModDirectory=conf\mods → 静默装 VC++/.NET（可勾选跳过）。

### 3. 卸载 Uninstall
读 FFXIV 路径 → 校验联接存在 → 解除游戏联接 → 恢复 `.old` → 解除启动器联接 → 恢复 `.old` → 删桌面 `caches`。

### 4. 清理 Clean
扫描部署根内 `*.log`/`*.old` → 显示数量+总大小 → 确认 → 逐个删除并汇报释放空间。

### 5. 设置页
FFXIV 路径管理、部署根展示、语言选择（跟随系统/中文/英文）、主题选择（跟随系统/浅/深）、日志查看与导出。

## 六、关键实现要点
- 部署根用 `Environment.ProcessPath`（单文件发布下准确指向 exe 目录）
- 耗时操作用 async/await 后台执行，步骤状态实时刷新，避免 UI 卡死
- 下载失败/网络中断时步骤标红、可重试单步
- 目录联接用 P/Invoke（FSCTL_SET_REPARSE_POINT），删除用 Directory.Delete（只删重解析点不删目标）
- 多语言覆盖界面文案+日志+确认框；文件夹/文件名保持固定不翻译

## 七、CI 工作流改造
release-scripts.yml：checkout → dotnet publish（-c Release -r win-x64 --self-contained true /p:PublishSingleFile=true）→ 生成 version.txt → 打包 exe + version.txt → 发布到 latest Release。

## 八、实施步骤
1. 创建解决方案与 WPF 项目骨架（csproj、app.manifest、App/MainWindow、依赖引入）
2. 核心 Services 层（DeploymentRoot/AppConfig/Localization/Theme/Disk/JsonConfig）
3. 系统操作服务（Junction/Download/Archive/RuntimeInstaller）+ StepRunner
4. UI：主面板 + 设置页 + 主题/多语言资源
5. 四个向导页接入 StepRunner（Init/Setup/Uninstall/Clean）
6. 更新 prompt.txt、README
7. CI 工作流改造 + 本地构建验证（C:\Program Files\dotnet\dotnet.exe build）
8. 补核心逻辑单元测试（AppConfig/DiskService），dotnet test

## 九、验证方式
- 本地：dotnet build 通过；CI：windows-latest 上 publish 打包发布
- 手动验证：Init 备份→Setup 挂载→Uninstall 还原→Clean 清理 全流程
