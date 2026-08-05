# FFXIV2GO 项目说明

## 项目意图

FFXIV2GO 是一套用于快速配置和卸载 XIVLauncherCN 游戏环境的 **Windows 桌面应用（C# WPF, .NET 10）**，面向网吧、网盘等非持久化环境。核心思路：在一台已配置好的电脑上，把 FFXIV 的游戏配置、XIVLauncherCN 配置、mod、运行依赖全部**持久化备份**到部署根 `[BASE]`（exe 所在任意目录，如 `D:\CloudUDrive\1NBhf6cVm`），之后在任意目标机器上通过**目录联接（junction）**把游戏配置目录指向 `[BASE]`，实现配置随身携带、即插即用。

> 注意：游戏中会频繁对存储介质进行读写操作，若性能不足可能导致游戏频繁卡顿，建议使用移动硬盘或固态颗粒 U盘部署。

## 技术要点

- **框架**：C# WPF，.NET 10；发布为**自包含单文件 exe**（win-x64），U盘/网盘即插即用，不依赖预装运行时
- **权限**：`app.manifest` 声明 `requireAdministrator`，启动即提权
- **深色模式**：WPF-UI（Fluent 风格），支持 跟随系统/浅/深 三档，运行时即时切换
- **多语言**：RESX 资源（中文 zh-CN + 英文 en），默认跟随系统；**首次启动弹窗询问语言并写入 config.ini**
- **托盘**：关闭窗口弹出三选对话框（取消/退出应用/最小化到托盘），托盘图标双击恢复、右键菜单含 显示主窗口/退出

## 部署根目录结构

```text
[BASE]\  部署根（exe 所在任意目录，不要求盘符根目录）
├── FFXIV2GO.exe
├── apps.json                应用下载清单（可编辑：增删应用/改地址/改必选组）
├── config.ini               FFXIV 路径、语言、主题配置
├── inst\                    安装包
│   ├── VC_redist.x64.exe
│   ├── windowsdesktop-runtime-10.0-win-x64.exe   .NET 10（aka.ms 最新直链）
│   └── ACT_Installer.exe     FFCafe ACT 自解压安装包（手动运行解压到 apps\ACT）
├── conf\                    配置文件夹
│   ├── FINAL FANTASY XIV - A Realm Reborn\   游戏配置
│   ├── XIVLauncherCN\       启动器配置
│   └── mods\                Penumbra mod
└── apps\                    软件（按 apps.json 下载）
    ├── XIVLauncherCN\ / XIVLauncherCN-Soil\
    ├── ACT\
    ├── ClashParty\
    ├── Snipaste\
    └── Everything\
```

## 运行顺序

1. **初始化**（一次性，在已配置好的电脑上执行）：备份环境到 `[BASE]`
2. **安装**（目标机器）：挂载配置 + 安装运行库
3. **卸载**：逆转安装，恢复本机原始配置
4. **清理**：删除 `[BASE]` 内 log/old 文件释放空间

---

## UI 结构

- **主面板（仪表盘）**：四个操作卡片（初始化/安装/卸载/清理）+ 环境信息卡（部署根/配置文件/版本）
- **左侧导航栏**：仪表盘/初始化/安装/卸载/清理/**应用**/设置
- **应用管理页**：随时可安装/卸载 `apps.json` 清单中的便携应用（卸载即删除应用文件夹），带进度与状态
- **设置页**：FFXIV 路径（浏览）、语言选择、主题选择、部署根/配置文件展示、保存写入 config.ini
- **向导页**：步骤列表 + 状态图标（○待执行/●执行中/✓完成/✕失败/—跳过）+ 步骤详情 + 进度条 + 开始/取消/返回按钮

## 脚本/代码结构

```
src/FFXIV2GO/
├── App.xaml(.cs)            启动、首次运行语言询问、全局异常处理（含崩溃日志 %TEMP%\ffxiv2go-crash.log）
├── app.manifest             管理员清单
├── MainWindow.xaml(.cs)     主窗口：导航 + 页面切换 + 托盘 + 关闭对话框（延迟弹窗）
├── Resources/
│   ├── Strings.resx         英文
│   └── Strings.zh-CN.resx   中文
├── Services/
│   ├── DeploymentRoot.cs    部署根与 inst/conf/apps 路径
│   ├── AppConfig.cs         config.ini 读写（PATH_FFXIV、Language、Theme）
│   ├── AppManifest.cs       apps.json 模型（AppEntry/AppType/GithubLatestRef）
│   ├── AppManifestService.cs 应用清单加载：exe 旁侧置优先，缺失从内嵌默认生成
│   ├── AppInstallService.cs 应用安装/卸载（解析地址→下载→解压；卸载删应用文件夹）
│   ├── GithubLatestResolver.cs GitHub 最新 Release 资产解析（正则匹配）
│   ├── EnvironmentStatus.cs 初始化检测（conf/inst 有文件）与重置（全部清空）
│   ├── LocalizationService.cs / LocalizedViewModel.cs  多语言
│   ├── ThemeService.cs      主题切换
│   ├── DownloadService.cs   HttpClient 下载 + 进度（VC/Net/启动器/ACT，GitHub 连通检测）
│   ├── ArchiveService.cs    SharpCompress 解压 7z
│   ├── JunctionService.cs   P/Invoke 创建/删除目录联接
│   ├── JsonConfigService.cs Penumbra.json 读写（ModDirectory）
│   ├── RuntimeInstaller.cs  静默安装 VC++/.NET
│   ├── FileSystemService.cs 目录递归复制
│   ├── DesktopHelper.cs     桌面路径（Desktop/OneDrive）
│   ├── ShortcutService.cs   桌面快捷方式创建/删除（WScript.Shell COM）
│   ├── DiskService.cs       log/old 扫描、删除、空间换算
│   └── StepRunner.cs        步骤执行器（顺序执行/失败即停/取消/跳过）
├── ViewModels/              Main/Dashboard/Settings/Init/Setup/Uninstall/Clean
└── Views/                   仪表盘/设置/向导/首次运行/关闭确认 + 转换器
```

## 工作流细节

### 初始化（Init，一次性）
0. **已初始化检测**：若 `conf`/`inst` 下有文件，提示「是否重新初始化」，选是则**清空 conf/inst/apps 全部数据**并按新环境重新初始化
1. 检测 GitHub 连通性（失败则中止）
2. 创建 inst/conf/apps 目录
3. 选择并校验 FFXIV 路径（`game\My Games\FINAL FANTASY XIV - A Realm Reborn` 必须存在）
4. 复制游戏配置 → conf
5. 备份 XIVLauncherCN：`%appdata%\XIVLauncherCN` 存在则复制到 conf；不存在则询问"是否作为新环境"，选否中止
6. 备份 mod：解析 `conf\XIVLauncherCN\pluginConfigs\Penumbra.json` 的 `ModDirectory` → 复制到 conf\mods（无则跳过）
7. 下载 VC++（aka.ms/vc14）与 .NET 10（aka.ms/dotnet/10.0）→ inst
8. **选择要下载的应用**：弹勾选窗口，加载 `apps.json` 清单；校验必选组（如 XIVLauncherCN 至少选一）；取消中止
9. **下载所选应用**：逐个解析地址（固定 URL 或 GitHub 最新 Release 按资产名正则匹配）→ 下载 → 按类型处理（archive 解压 / portable 原样保存 / installer 提示手动安装），带进度与日志；含 `promptExtract` 的应用（如 FFCafe ACT）下载到 `inst` 后弹框提示用户手动运行自解压文件并解压到 `apps\<target>`（应包含 CafeACT.exe）

### 安装（Setup）
0. **未初始化检测**：若未初始化，提示「是否先进行初始化」，选是则跳转初始化页并中止本次
1. 读取 config.ini 的 FFXIV 路径（无则浏览选择并保存）
2. 校验游戏配置目录存在
3. 原游戏配置改名为 `.old`
4. 创建联接 `[游戏配置]` → `conf\FINAL FANTASY XIV - A Realm Reborn`（失败回滚）
5. `%appdata%\XIVLauncherCN` 改名 `.old`
6. 创建联接 → `conf\XIVLauncherCN`
7. 桌面创建 `caches` 目录（Lightless，替代已弃用的 MareSynchronos）
8. 修改 Penumbra.json 的 `ModDirectory` → `conf\mods`
9. 静默安装 `inst\VC_redist.x64.exe` 与 `inst\windowsdesktop-runtime-10.0-win-x64.exe`（`/install /quiet /norestart`，缺包则跳过）
10. **创建桌面快捷方式**：为清单中已安装到 apps 的应用（找到可执行文件，优先非安装器如 CafeACT.exe）建 `.lnk`；再建 FFXIV2GO 与部署根文件夹快捷方式（本地化命名）

### 卸载（Uninstall）
1. 读取 FFXIV 路径（无则浏览）
2. 校验游戏配置联接存在（不存在则中止）
3. 解除游戏配置联接（仅删重解析点）
4. `.old` 恢复为原名
5. 解除 `%appdata%\XIVLauncherCN` 联接
6. `XIVLauncherCN.old` 恢复
7. 删除桌面 `caches`
8. **删除桌面快捷方式**：删除安装阶段创建的应用/FFXIV2GO/部署根文件夹快捷方式（文件夹快捷方式按两种语言名尝试删除）

### 应用管理（Apps，随时可用，不依赖初始化）
- 列出 `apps.json` 清单全部应用与安装状态
- **安装**：解析地址（固定/GitHub 最新版）→ 下载 → 按类型解压/保存，带进度；含 `promptExtract` 的应用（如 FFCafe ACT）弹框提示手动解压
- **卸载**：删除 `apps\<target>` 应用文件夹（全部为 portable），操作前确认
- 刷新按钮重新检测安装状态

### 清理（Clean）
1. 扫描部署根内 `*.log`/`*.old`，显示数量与总大小（无则结束）
2. 确认后逐个删除，实时显示当前文件与进度
3. 汇报释放空间

### 设置（Settings）
- FFXIV 路径：文本 + 浏览
- 语言：跟随系统/中文/英文（即时生效，保存到 config.ini）
- 主题：跟随系统/浅色/深色（即时生效，保存到 config.ini）
- 展示部署根目录与配置文件路径

## 关键实现约定
- 部署根用 `Environment.ProcessPath` 所在目录
- **日志**：`LogService` 写入部署根 `logs\ffxiv2go.log`（UTF-8，线程安全，失败不阻断）；记录启动/配置、步骤执行、下载、目录联接、应用安装卸载、异常；设置页可「打开日志/打开日志文件夹」，未处理异常同时写 `%TEMP%\ffxiv2go-crash.log`
- 耗时操作放后台执行（`Task.Run`），进度/日志经 `IProgress`/绑定推送 UI，避免卡顿
- 步骤失败即停（不再继续后续步骤）；重试通过再次点击"开始"
- 目录联接创建用 P/Invoke（FSCTL_SET_REPARSE_POINT），删除用 `Directory.Delete`（只删重解析点）
- 关闭窗口：不在 `OnClosing` 内同步弹框（窗口处于 closing 状态会抛异常），改用 `Dispatcher.BeginInvoke` 延迟弹出三选对话框；窗口未显示/已隐藏时不弹框直接退出
- 多语言覆盖界面+日志+对话框；文件夹/文件名（如 `FINAL FANTASY XIV - A Realm Reborn`）固定不翻译

## CI 发布
`.github/workflows/release-scripts.yml`：打 `v*` tag 或手动触发 → `dotnet publish`（-c Release -r win-x64 --self-contained true /p:PublishSingleFile=true）→ 打包 `FFXIV2GO.exe` + version.txt → 发布到固定 `latest` Release。
