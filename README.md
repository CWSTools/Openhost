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

如果本机安装了 Visual Studio Build Tools，并包含“使用 C++ 的桌面开发”组件，脚本也会自动构建并打包 Win32 原生配置工具 `OpenHostSettings.exe`。

可选参数：

```powershell
.\scripts\build-package.ps1 -Configuration Debug
.\scripts\build-package.ps1 -Runtime win-x64 -SelfContained
.\scripts\build-package.ps1 -NoRestore
.\scripts\build-package.ps1 -SkipSettingsUi
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

## Win32 配置界面

如果包里包含 `OpenHostSettings.exe`，可以直接运行它进行图形化配置。

### 打开界面

把 zip 解压后，进入解压目录，双击：

```text
OpenHostSettings.exe
```

也可以在 PowerShell 中运行：

```powershell
.\OpenHostSettings.exe
```

`OpenHostSettings.exe` 需要和 `OpenHost.exe` 放在同一个目录。它只负责配置，不接管文件打开逻辑；真正的文件中转仍由 `OpenHost.exe` 完成。

### 界面功能

界面使用 MFC/Windows 原生控件，提供：

- PowerPoint / Word / Excel / PDF 的打开目标选择
- 保存配置
- 注册或刷新文件关联
- 查询 Office/WPS 位置
- 打开配置目录

### 使用步骤

1. 在 PowerPoint、Word、Excel、PDF 四个下拉框中选择打开目标。
2. 点击 `保存配置`，把选择写入：

```text
%LOCALAPPDATA%\OpenHost\config.json
```

3. 点击 `注册关联`，把 OpenHost 注册为 `.ppt`、`.pptx` 等文件的打开方式。
4. 之后双击支持的文件时，会先进入 `OpenHost.exe`，再按配置转交给 Office、WPS 或系统默认程序。

### 按钮说明

- `保存配置`：只保存当前下拉框选择，不修改文件关联。
- `注册关联`：注册或刷新文件关联，通常安装后点一次即可。
- `查询位置`：扫描 Office/WPS 安装位置，并写入：

```text
%LOCALAPPDATA%\OpenHost\app-locations.json
```

- `配置目录`：打开 `%LOCALAPPDATA%\OpenHost`，方便查看 `config.json` 和 `app-locations.json`。

### 注意

- 第一次使用建议先点 `保存配置`，再点 `注册关联`。
- 如果移动了解压目录，需要重新运行 `OpenHostSettings.exe` 并点击 `注册关联`。
- `OpenHostSettings.exe` 是配置工具；卸载或不用 UI 时，`OpenHost.exe` 仍可通过协议命令配置。

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
