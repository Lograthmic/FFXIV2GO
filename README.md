# FFXIV 环境配置工具

这是一个用于快速配置和卸载 XIVLauncherCN 游戏环境的批处理脚本工具集，用于在网吧等特定环境持久化保存游戏数据，避免每次重复配置。

## 使用说明

### 准备工作：

1. 将两个脚本放在预配置文件的同一盘符下
2. 确保 pre 文件夹包含所有必要的文件

## 文件结构
```text
X:\  U盘根目录
├── FFXIV_Setup.bat  安装脚本
├── FFXIV_Uninstall.bat  卸载甲苯
└── pre\
    ├── FINAL FANTASY XIV - A Realm Reborn\  游戏配置目录
    ├── XIVLauncherCN\  XIVLauncherCN配置目录
    ├── VC_redist.x64.exe  VC依赖包
    └── windowsdesktop-runtime-8.0.21-win-x64.exe  dotNet依赖包
```

### 运行顺序

1. 先运行 FFXIV_Setup.bat 进行配置
2. 需要卸载时运行 FFXIV_Uninstall.bat

## 相关链接

1. [Microsoft Visual C++ 可再发行程序包最新支持的下载](https://learn.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist?view=msvc-170)
2. [下载 .NET 8.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
