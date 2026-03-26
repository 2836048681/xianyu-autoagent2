# Xianyu AutoAgent

<div align="center">

闲鱼多账号自动化值守工具，集成 Python 服务、WinUI 3 桌面端、扫码登录、实时日志与 Windows 安装包分发。

[![Python](https://img.shields.io/badge/Python-3.10%2B-3776AB?logo=python&logoColor=white)](https://www.python.org/)
[![WinUI 3](https://img.shields.io/badge/WinUI%203-Windows-0078D4)](https://learn.microsoft.com/windows/apps/winui/)
[![Release](https://img.shields.io/github/v/release/2836048681/xianyu-autoagent2)](https://github.com/2836048681/xianyu-autoagent2/releases)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)

</div>

## 项目简介

本项目面向闲鱼消息自动化处理场景，提供一套可落地的桌面化运行方案：

- Python 后台服务负责消息处理与自动化逻辑
- WinUI 3 桌面端负责多账号配置、启动控制、登录与日志查看
- 支持最多 5 个账号隔离运行
- 支持内嵌 WebView2 登录与浏览器兜底登录
- 支持一键构建 Windows 安装包

参考项目：

- 原始仓库：[shaxiu/XianyuAutoAgent](https://github.com/shaxiu/XianyuAutoAgent)

当前仓库：

- GitHub：[2836048681/xianyu-autoagent2](https://github.com/2836048681/xianyu-autoagent2)

## 功能特性

| 模块 | 说明 |
| --- | --- |
| 多账号管理 | 最多 5 个账号隔离运行，配置与数据彼此独立 |
| 桌面控制台 | 统一管理 API Key、模型地址、模型名称、Cookie |
| 登录能力 | 支持内嵌 WebView2 登录与浏览器兜底登录 |
| 实时日志 | 启停状态、运行日志、账号日志路径一体展示 |
| 打包发布 | 支持 Python worker、WinUI 桌面端、Inno Setup 安装包 |
| 安装可运行 | 安装包已补齐 XAML / PRI 资源，避免安装后无法启动 |

## 界面预览

![UI Preview](./images/ui.png)

## 项目结构

```text
.
├─ main.py                         # Python 主服务入口
├─ worker_cli.py                   # 后台 worker CLI
├─ prompts/                        # 提示词模板
├─ utils/                          # 通用工具
├─ scripts/                        # 辅助脚本
├─ chrome/                         # 离线 Chrome 运行时
├─ chromedriver/                   # chromedriver
├─ desktop/
│  └─ XianyuDesktopApp/            # WinUI 3 桌面端
├─ installer/
│  └─ inno/setup.iss               # Inno Setup 安装脚本
├─ build_worker.ps1                # 构建 Python worker
├─ build_desktop.ps1               # 发布桌面端
├─ build_all.ps1                   # 一键生成安装包
└─ release_notes.md                # GitHub Releases 发布说明模板
```

## 环境要求

### Python

- Python 3.10 及以上，推荐 3.11

安装依赖：

```powershell
pip install -r requirements.txt
```

### Windows 桌面端

- Windows 10 / 11 x64
- .NET SDK 8
- WinUI 3 / Windows App SDK 构建环境
- Inno Setup 6

## 快速开始

### 1. 启动 Python 主服务

```powershell
python main.py
```

### 2. 构建并运行桌面端

```powershell
cd desktop\XianyuDesktopApp
dotnet build -c Release -p:Platform=x64
```

本地调试可直接运行：

```powershell
.\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\XianyuDesktopApp.exe
```

### 3. 生成安装包

```powershell
.\build_all.ps1
```

生成结果：

```text
installer\inno\Output\XianyuAutoAgent_1.0_Setup.exe
```

## 桌面端使用流程

1. 启动桌面端
2. 点击“新增账号”
3. 填写 `API_KEY`、`MODEL_BASE_URL`、`MODEL_NAME`
4. 使用“内嵌登录”或“浏览器兜底登录”获取 Cookie
5. 保存配置后启动账号

账号数据会自动写入：

```text
%APPDATA%\XianyuAutoAgent\accounts\<account_name>\
```

每个账号独立保存：

- `.env`
- `prompts/`
- `data/`
- `logs/`
- `webview2/`

## 构建说明

### 构建 Python worker

```powershell
.\build_worker.ps1
```

输出：

```text
dist\XianyuWorker.exe
```

### 发布桌面端

```powershell
.\build_desktop.ps1
```

输出：

```text
desktop\publish\app\XianyuDesktopApp\
```

### 完整生成安装包

```powershell
.\build_all.ps1
```

输出：

```text
installer\inno\Output\XianyuAutoAgent_1.0_Setup.exe
```

## 常见问题

### 安装后无法启动

当前版本的打包脚本已经额外补齐以下资源：

- `App.xbf`
- `MainWindow.xbf`
- `XianyuDesktopApp.pri`
- `Controls / Services / Styles` 下的 XAML 编译资源

相关逻辑位于：

- [build_desktop.ps1](./build_desktop.ps1)
- [installer/inno/setup.iss](./installer/inno/setup.iss)

### UI 看不清、发白、界面偏卡

当前桌面端已切换为：

- 高对比浅色实体卡片
- 低特效样式
- 关闭重玻璃 / Mica 背景链路

### 登录失败

可依次尝试：

- 内嵌登录
- 浏览器兜底登录

获取成功后 Cookie 会自动写回当前账号目录。

## Release

最新安装包下载：

- [GitHub Releases](https://github.com/2836048681/xianyu-autoagent2/releases)

## 免责声明

本项目仅供学习与交流使用，请自行评估平台规则、账号风险与合规要求。
