# Xianyu AutoAgent

面向闲鱼场景的自动化值守项目，支持：

- Python 主服务自动处理消息与上下文
- 多账号隔离运行
- WinUI 3 桌面端配置与控制台
- 内嵌 WebView2 / 浏览器兜底登录
- 离线打包与 Windows 安装包分发

仓库地址：

- 原始参考项目：[shaxiu/XianyuAutoAgent](https://github.com/shaxiu/XianyuAutoAgent)
- 当前仓库：[2836048681/xianyu-autoagent2](https://github.com/2836048681/xianyu-autoagent2)

## 项目结构

```text
.
├─ main.py                         # Python 服务入口
├─ worker_cli.py                   # 后台 worker CLI
├─ gui_app.py                      # 旧 GUI 入口
├─ prompts/                        # 提示词模板
├─ utils/                          # 环境、日志等通用工具
├─ scripts/                        # 辅助脚本
├─ chrome/                         # 离线 Chrome 运行时
├─ chromedriver/                   # chromedriver
├─ desktop/
│  └─ XianyuDesktopApp/            # WinUI 3 桌面端
├─ installer/
│  └─ inno/setup.iss               # Inno Setup 脚本
├─ build_worker.ps1                # 构建 Python worker
├─ build_desktop.ps1               # 发布桌面端
└─ build_all.ps1                   # 一键生成安装包
```

## 功能概览

### 1. 主服务能力

- 自动处理闲鱼消息
- 上下文记忆与提示词路由
- 多专家场景提示词支持
- 独立账号数据目录

### 2. 桌面端能力

- 新增/切换账号
- 编辑 API Key、模型地址、模型名、Cookie
- 启动/停止账号
- 扫码登录更新 Cookie
- 实时查看运行日志
- 检查 GitHub Release 更新

### 3. 打包与安装

- Python worker 打包为 `XianyuWorker.exe`
- WinUI 3 桌面端自包含发布
- Inno Setup 生成可安装的 `Setup.exe`
- 安装包内置运行所需 XAML 资源、离线浏览器和驱动

## 运行环境

### Python

- Python 3.10+，推荐 3.11

安装依赖：

```powershell
pip install -r requirements.txt
```

### 桌面端

- Windows 10/11 x64
- .NET SDK 8
- WinUI 3 / Windows App SDK 构建环境
- Inno Setup 6（生成安装包时需要）

## 配置说明

账号配置最终会写入：

```text
%APPDATA%\XianyuAutoAgent\accounts\<account_name>\
```

每个账号独立保存：

- `.env`
- `prompts/`
- `data/`
- `logs/`
- `webview2/`

常用配置项：

- `API_KEY`
- `MODEL_BASE_URL`
- `MODEL_NAME`
- `COOKIES_STR`

## 本地开发

### 1. 运行 Python 主服务

```powershell
python main.py
```

### 2. 运行桌面端

```powershell
cd desktop\XianyuDesktopApp
dotnet build -c Release -p:Platform=x64
```

调试桌面端时建议直接运行生成的可执行文件：

```powershell
.\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\XianyuDesktopApp.exe
```

## 构建

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

### 生成完整安装包

```powershell
.\build_all.ps1
```

输出：

```text
installer\inno\Output\XianyuAutoAgent_1.0_Setup.exe
```

## 安装说明

安装完成后可直接启动 `XianyuDesktopApp.exe`。

如果你是从源码重新打包，当前版本已经额外处理了安装后启动所需资源：

- `App.xbf`
- `MainWindow.xbf`
- `XianyuDesktopApp.pri`
- `Controls/Services/Styles` 下的 XAML 编译资源

这部分逻辑在：

- [build_desktop.ps1](./build_desktop.ps1)
- [installer/inno/setup.iss](./installer/inno/setup.iss)

## 常见问题

### 1. 安装后无法启动

优先检查安装目录中是否存在：

- `XianyuDesktopApp.pri`
- `App.xbf`
- `MainWindow.xbf`

当前仓库版本的打包脚本已经包含这些资源。

### 2. UI 发白、看不清、界面卡

当前桌面端已切换为：

- 高对比浅色实体卡片
- 低特效样式
- 关闭重玻璃/Mica 背景链路

如果仍然卡顿，优先检查日志区刷新频率和后台 worker 输出量。

### 3. 登录失败

可先尝试：

- 内嵌登录
- 浏览器兜底登录

Cookie 获取成功后会自动写回当前账号目录。

## 发布建议

推荐使用 GitHub Releases 分发安装包：

1. 运行 `.\build_all.ps1`
2. 上传 `installer\inno\Output\XianyuAutoAgent_1.0_Setup.exe`
3. 在 Release 说明中标注版本变化

## 免责声明

本项目仅供学习与交流使用，请自行评估平台规则、账号风险与合规要求。
