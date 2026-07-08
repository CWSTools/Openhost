# OpenHost

OpenHost 是一个 Windows 文件打开中转器。它可以把自己注册为 `.ppt`、`.pptx` 等文件的打开方式，收到文件路径后，再按本地配置转交给 Microsoft Office、WPS 或系统默认程序。

OpenHost 不提供界面，不进行交互，也不依赖 CWSTool。

## 支持的文件

- PowerPoint：`.ppt`、`.pptx`
- Word：`.doc`、`.docx`
- Excel：`.xls`、`.xlsx`
- PDF：`.pdf`

## 构建和打包

在仓库根目录运行：

```powershell
.\scripts\build-package.ps1
```

脚本会执行 Release 发布，并生成：

```text
dist\OpenHost-win-x64.zip
```

zip 内包含 `OpenHost.exe`、运行依赖、`README.md` 和示例脚本。

可选参数：

```powershell
.\scripts\build-package.ps1 -Configuration Debug
.\scripts\build-package.ps1 -Runtime win-x64 -SelfContained
.\scripts\build-package.ps1 -NoRestore
```

## 安装或更新

把 zip 解压到一个固定目录，然后执行注册：

```powershell
.\OpenHost.exe "openhost://register"
```

注册写入当前用户注册表 `HKCU\Software\Classes`，通常不需要管理员权限。

注册后，支持的文件打开方式会指向：

```text
OpenHost.exe "%1"
```

也就是文件先进入 OpenHost，再由 OpenHost 转给 Office、WPS 或系统默认程序。

## 切换打开目标

直接使用协议：

```powershell
.\OpenHost.exe "openhost://set-open-method?type=powerpoint&target=WPS"
.\OpenHost.exe "openhost://set-open-method?type=pptx&target=Office"
.\OpenHost.exe "openhost://set-open-method?type=pdf&target=System"
```

`type` 支持分类名和扩展名：

```text
powerpoint, word, excel, pdf, ppt, pptx, doc, docx, xls, xlsx
```

`target` 支持：

```text
Office, WPS, System
```

也可以使用示例脚本：

```powershell
.\examples\switch-open-method.ps1 -Target WPS -Types powerpoint
.\examples\switch-open-method.ps1 -Target Office -Types ppt,pptx
.\examples\switch-open-method.ps1 -Target System -Types pdf
```

配置保存位置：

```text
%LOCALAPPDATA%\OpenHost\config.json
```

## 查询 Office 和 WPS 位置

运行：

```powershell
.\OpenHost.exe "openhost://query-apps"
```

OpenHost 会扫描注册表 App Paths 和常见安装目录，然后写入：

```text
%LOCALAPPDATA%\OpenHost\app-locations.json
```

## 手动打开文件

直接传文件路径：

```powershell
.\OpenHost.exe "D:\demo\slides.pptx"
```

使用协议 URL：

```powershell
.\OpenHost.exe "openhost://open?file=D:\demo\slides.pptx"
```

## 说明

- 重复执行 `openhost://register` 可以刷新文件关联。
- 注册时会清理旧的 `cwstool` 和 `CWSOpenHost.*` 当前用户注册项。
- 如果配置的 Office/WPS 程序找不到，OpenHost 会以失败退出，不弹界面。
