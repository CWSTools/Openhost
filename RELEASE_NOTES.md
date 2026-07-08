# OpenHost 发布说明

## OpenHost v0.1

OpenHost 是一个轻量的 Windows 文件打开中转器。它可以注册为 `.ppt`、`.pptx`、`.doc`、`.docx`、`.xls`、`.xlsx`、`.pdf` 等文件的打开方式，并根据配置自动转交给 Microsoft Office、WPS 或系统默认程序。

本版本提供纯中转主程序 `OpenHost.exe` 和 MFC 原生配置界面 `OpenHostSettings.exe`。

## 主要特性

- 支持 PowerPoint、Word、Excel、PDF 文件中转打开。
- 支持 Microsoft Office / WPS / System 三种打开目标。
- 提供 MFC 原生 Windows 配置界面。
- 支持一键注册或刷新文件关联。
- 支持查询 Office 和 WPS 安装位置。
- 不依赖 CWSTool，不使用 IPC，不弹出中转交互界面。
- 配置保存在当前用户目录，不需要管理员权限。

## 下载内容

发行包：

```text
OpenHost-win-x64.zip
```

压缩包内包含：

```text
OpenHost.exe
OpenHostSettings.exe
README.md
examples/switch-open-method.ps1
```

## 安装方法

1. 下载并解压 `OpenHost-win-x64.zip` 到一个固定目录。
2. 运行 `OpenHostSettings.exe`。
3. 在界面中选择 PowerPoint / Word / Excel / PDF 的打开目标。
4. 点击 `保存配置`。
5. 点击 `注册关联`。

完成后，双击支持的文件时会先进入 `OpenHost.exe`，再自动转交给配置的程序。

## 命令行注册

也可以不使用 UI，直接运行：

```powershell
.\OpenHost.exe "openhost://register"
```

切换打开方式示例：

```powershell
.\OpenHost.exe "openhost://set-open-method?type=powerpoint&target=WPS"
.\OpenHost.exe "openhost://set-open-method?type=pptx&target=Office"
.\OpenHost.exe "openhost://set-open-method?type=pdf&target=System"
```

## 配置文件

配置保存位置：

```text
%LOCALAPPDATA%\OpenHost\config.json
```

Office/WPS 查询结果：

```text
%LOCALAPPDATA%\OpenHost\app-locations.json
```

## 注意事项

- 如果移动了解压目录，需要重新点击 `注册关联`。
- `OpenHostSettings.exe` 需要和 `OpenHost.exe` 放在同一个目录。
- 如果选择 Office 或 WPS，但系统中找不到对应程序，OpenHost 会打开失败。
- 注册文件关联写入 `HKCU\Software\Classes`，通常不需要管理员权限。
- 重复执行注册会刷新关联，并清理旧的 `cwstool` / `CWSOpenHost.*` 当前用户注册项。

## 构建

从源码构建发行包：

```powershell
.\scripts\build-package.ps1
```

构建产物：

```text
dist\OpenHost-win-x64.zip
```
