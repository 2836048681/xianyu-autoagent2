# Xianyu AutoAgent v1.0

建议将下面内容直接作为 GitHub Release 描述使用。

## 更新摘要

- 新增 WinUI 3 桌面端，统一账号配置、运行控制、登录与日志查看
- 支持最多 5 个账号隔离运行
- 支持内嵌 WebView2 登录和浏览器兜底登录
- 修复安装后无法启动的问题，补齐桌面端运行所需 XAML / PRI 资源
- 重做桌面端 UI，提升可读性并降低界面卡顿
- 完善 Windows 打包与安装流程，统一使用 Inno Setup 安装包分发

## 本次发布包含

- `XianyuAutoAgent_1.0_Setup.exe`

## 安装说明

1. 下载并运行安装包
2. 首次启动后新增账号
3. 配置 `API_KEY`、`MODEL_BASE_URL`、`MODEL_NAME`
4. 通过内嵌登录或兜底登录获取并保存 Cookie
5. 启动账号开始运行

## 升级说明

- 建议覆盖安装最新版本
- 用户账号数据保存在 `%APPDATA%\XianyuAutoAgent\accounts\`，不会随安装目录覆盖丢失

## 已知限制

- 当前仅提供 Windows x64 安装包
- 需要本地可用的 WebView2 运行环境
- 大量实时日志输出时，低性能机器仍可能出现轻微刷新开销

## 适合粘贴到 GitHub Releases 的简版

```text
Xianyu AutoAgent v1.0

更新内容：
- 新增 WinUI 3 桌面端
- 支持最多 5 个账号隔离运行
- 支持内嵌 WebView2 登录 / 浏览器兜底登录
- 修复安装后无法启动的问题
- 重做 UI，提升可读性并减少卡顿
- 完善 Windows 安装包构建与分发流程

安装包：
- XianyuAutoAgent_1.0_Setup.exe
```
