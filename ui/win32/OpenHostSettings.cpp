#include <afxwin.h>
#include <afxcmn.h>
#include <afxdialogex.h>
#include <shellapi.h>
#include <shlobj.h>

#include <array>
#include <string>
#include <vector>

namespace
{
constexpr int kWindowWidth = 500;
constexpr int kWindowHeight = 380;
constexpr int kMargin = 22;
constexpr int kLabelWidth = 118;
constexpr int kComboWidth = 190;
constexpr int kRowHeight = 38;

constexpr int kIdPowerPoint = 1001;
constexpr int kIdWord = 1002;
constexpr int kIdExcel = 1003;
constexpr int kIdPdf = 1004;
constexpr int kIdSave = 2001;
constexpr int kIdRegister = 2002;
constexpr int kIdQuery = 2003;
constexpr int kIdOpenConfig = 2004;

struct Entry
{
    const wchar_t* key;
    const wchar_t* label;
    int comboId;
};

constexpr std::array<Entry, 4> kEntries{{
    {L"powerpoint", L"PowerPoint", kIdPowerPoint},
    {L"word", L"Word", kIdWord},
    {L"excel", L"Excel", kIdExcel},
    {L"pdf", L"PDF", kIdPdf},
}};

constexpr std::array<const wchar_t*, 3> kTargets{{L"Office", L"WPS", L"System"}};

void AppendWord(std::vector<BYTE>& data, WORD value)
{
    data.push_back(static_cast<BYTE>(value & 0xff));
    data.push_back(static_cast<BYTE>((value >> 8) & 0xff));
}

void AppendString(std::vector<BYTE>& data, const wchar_t* value)
{
    while (*value)
    {
        AppendWord(data, static_cast<WORD>(*value++));
    }

    AppendWord(data, 0);
}

void AlignDword(std::vector<BYTE>& data)
{
    while ((data.size() % sizeof(DWORD)) != 0)
    {
        data.push_back(0);
    }
}

std::vector<BYTE> CreateDialogTemplate()
{
    std::vector<BYTE> data(sizeof(DLGTEMPLATE));
    auto* dialogTemplate = reinterpret_cast<DLGTEMPLATE*>(data.data());
    dialogTemplate->style = WS_POPUP | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX |
                            DS_CENTER | DS_MODALFRAME | DS_SETFONT;
    dialogTemplate->dwExtendedStyle = 0;
    dialogTemplate->cdit = 0;
    dialogTemplate->x = 0;
    dialogTemplate->y = 0;
    dialogTemplate->cx = 305;
    dialogTemplate->cy = 225;

    AppendWord(data, 0);
    AppendWord(data, 0);
    AppendString(data, L"OpenHost 设置");
    AppendWord(data, 9);
    AppendString(data, L"MS Shell Dlg 2");
    AlignDword(data);
    return data;
}

CString GetModuleDirectory()
{
    wchar_t path[MAX_PATH]{};
    GetModuleFileNameW(nullptr, path, MAX_PATH);
    CString result(path);
    const int pos = result.ReverseFind(L'\\');
    return pos < 0 ? CString(L".") : result.Left(pos);
}

CString CombinePath(const CString& left, const CString& right)
{
    if (left.IsEmpty())
    {
        return right;
    }

    const wchar_t last = left[left.GetLength() - 1];
    if (last == L'\\' || last == L'/')
    {
        return left + right;
    }

    return left + L"\\" + right;
}

CString GetLocalAppDataPath()
{
    wchar_t path[MAX_PATH]{};
    if (SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, path)))
    {
        return path;
    }

    return GetModuleDirectory();
}

CString GetConfigDirectory()
{
    return CombinePath(GetLocalAppDataPath(), L"OpenHost");
}

CString GetConfigPath()
{
    return CombinePath(GetConfigDirectory(), L"config.json");
}

CString ReadTextFile(const CString& path)
{
    HANDLE file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE)
    {
        return L"";
    }

    LARGE_INTEGER size{};
    if (!GetFileSizeEx(file, &size) || size.QuadPart <= 0 || size.QuadPart > 1024 * 1024)
    {
        CloseHandle(file);
        return L"";
    }

    std::string bytes(static_cast<size_t>(size.QuadPart), '\0');
    DWORD bytesRead = 0;
    const BOOL ok = ReadFile(file, bytes.data(), static_cast<DWORD>(bytes.size()), &bytesRead, nullptr);
    CloseHandle(file);
    if (!ok || bytesRead == 0)
    {
        return L"";
    }

    bytes.resize(bytesRead);
    const int length = MultiByteToWideChar(CP_UTF8, 0, bytes.data(), static_cast<int>(bytes.size()), nullptr, 0);
    if (length <= 0)
    {
        return L"";
    }

    CString text;
    MultiByteToWideChar(CP_UTF8, 0, bytes.data(), static_cast<int>(bytes.size()), text.GetBuffer(length), length);
    text.ReleaseBuffer(length);
    return text;
}

CString FindTargetInConfig(const CString& json, const CString& key)
{
    const CString keyToken = L"\"" + key + L"\"";
    int keyPos = json.Find(keyToken);
    if (keyPos < 0)
    {
        return L"System";
    }

    int colonPos = json.Find(L":", keyPos + keyToken.GetLength());
    if (colonPos < 0)
    {
        return L"System";
    }

    int quoteStart = json.Find(L"\"", colonPos + 1);
    if (quoteStart < 0)
    {
        return L"System";
    }

    int quoteEnd = json.Find(L"\"", quoteStart + 1);
    if (quoteEnd < 0)
    {
        return L"System";
    }

    return json.Mid(quoteStart + 1, quoteEnd - quoteStart - 1);
}

int TargetToIndex(const CString& target)
{
    for (int i = 0; i < static_cast<int>(kTargets.size()); ++i)
    {
        if (target.CompareNoCase(kTargets[static_cast<size_t>(i)]) == 0)
        {
            return i;
        }
    }

    return 2;
}

CString ToUtf8JsonString(const CString& wide)
{
    const int length = WideCharToMultiByte(CP_UTF8, 0, wide, -1, nullptr, 0, nullptr, nullptr);
    if (length <= 1)
    {
        return L"";
    }

    CStringA utf8;
    WideCharToMultiByte(CP_UTF8, 0, wide, -1, utf8.GetBuffer(length - 1), length - 1, nullptr, nullptr);
    utf8.ReleaseBuffer(length - 1);
    return CString(utf8);
}
}

class COpenHostSettingsDialog final : public CDialogEx
{
public:
    COpenHostSettingsDialog()
    {
        m_template = CreateDialogTemplate();
        InitModalIndirect(reinterpret_cast<LPCDLGTEMPLATE>(m_template.data()));
    }

protected:
    BOOL OnInitDialog() override
    {
        CDialogEx::OnInitDialog();
        SetWindowTextW(L"OpenHost 设置");
        CreateFonts();
        MoveWindowCentered();
        CreateControls();
        LoadConfig();
        return TRUE;
    }

    void OnDestroy()
    {
        if (m_titleFont.GetSafeHandle())
        {
            m_titleFont.DeleteObject();
        }

        CDialogEx::OnDestroy();
    }

    afx_msg void OnSave()
    {
        SetStatus(SaveConfig() ? L"配置已保存。" : L"保存失败。");
    }

    afx_msg void OnRegister()
    {
        SetStatus(RunOpenHost(L"openhost://register") ? L"文件关联已注册。" : L"注册失败。");
    }

    afx_msg void OnQuery()
    {
        SetStatus(RunOpenHost(L"openhost://query-apps") ? L"位置查询已完成。" : L"查询失败。");
    }

    afx_msg void OnOpenConfig()
    {
        CreateDirectoryW(GetConfigDirectory(), nullptr);
        ShellExecuteW(m_hWnd, L"open", GetConfigDirectory(), nullptr, nullptr, SW_SHOWNORMAL);
    }

    DECLARE_MESSAGE_MAP()

private:
    void CreateFonts()
    {
        LOGFONTW logFont{};
        CFont* dialogFont = GetFont();
        if (dialogFont && dialogFont->GetLogFont(&logFont))
        {
            logFont.lfHeight = -20;
            logFont.lfWeight = FW_SEMIBOLD;
            wcscpy_s(logFont.lfFaceName, L"MS Shell Dlg 2");
            m_titleFont.CreateFontIndirectW(&logFont);
        }
    }

    void MoveWindowCentered()
    {
        const int x = (GetSystemMetrics(SM_CXSCREEN) - kWindowWidth) / 2;
        const int y = (GetSystemMetrics(SM_CYSCREEN) - kWindowHeight) / 2;
        MoveWindow(x, y, kWindowWidth, kWindowHeight);
    }

    void CreateControls()
    {
        m_title.Create(L"OpenHost 设置", WS_CHILD | WS_VISIBLE | SS_LEFT, CRect(kMargin, 16, 280, 48), this);
        m_title.SetFont(m_titleFont.GetSafeHandle() ? &m_titleFont : GetFont());

        m_hint.Create(L"选择每类文件转交给哪个程序打开。", WS_CHILD | WS_VISIBLE | SS_LEFT, CRect(kMargin, 54, 430, 78), this);
        m_hint.SetFont(GetFont());

        int y = 94;
        for (size_t i = 0; i < kEntries.size(); ++i)
        {
            auto& label = m_labels[i];
            auto& combo = m_combos[i];
            const auto& entry = kEntries[i];

            label.Create(entry.label, WS_CHILD | WS_VISIBLE | SS_LEFT, CRect(kMargin, y + 6, kMargin + kLabelWidth, y + 30), this);
            label.SetFont(GetFont());

            combo.Create(
                WS_CHILD | WS_VISIBLE | WS_TABSTOP | CBS_DROPDOWNLIST | WS_VSCROLL,
                CRect(kMargin + kLabelWidth, y, kMargin + kLabelWidth + kComboWidth, y + 160),
                this,
                entry.comboId);
            combo.SetFont(GetFont());
            for (const auto* target : kTargets)
            {
                combo.AddString(target);
            }

            y += kRowHeight;
        }

        constexpr int buttonTop = 258;
        constexpr int buttonHeight = 32;
        m_save.Create(L"保存配置", WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, CRect(kMargin, buttonTop, 116, buttonTop + buttonHeight), this, kIdSave);
        m_register.Create(L"注册关联", WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, CRect(126, buttonTop, 220, buttonTop + buttonHeight), this, kIdRegister);
        m_query.Create(L"查询位置", WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, CRect(230, buttonTop, 324, buttonTop + buttonHeight), this, kIdQuery);
        m_openConfig.Create(L"配置目录", WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, CRect(334, buttonTop, 428, buttonTop + buttonHeight), this, kIdOpenConfig);

        m_save.SetFont(GetFont());
        m_register.SetFont(GetFont());
        m_query.SetFont(GetFont());
        m_openConfig.SetFont(GetFont());

        m_status.Create(L"就绪", WS_CHILD | WS_VISIBLE | SS_LEFT | SS_SUNKEN, CRect(kMargin, 306, 456, 334), this);
        m_status.SetFont(GetFont());
    }

    void LoadConfig()
    {
        const CString json = ReadTextFile(GetConfigPath());
        for (size_t i = 0; i < kEntries.size(); ++i)
        {
            const CString target = FindTargetInConfig(json, kEntries[i].key);
            m_combos[i].SetCurSel(TargetToIndex(target));
        }
    }

    CString ComboTarget(size_t index) const
    {
        CString value = L"System";
        const int selection = m_combos[index].GetCurSel();
        if (selection >= 0)
        {
            m_combos[index].GetLBText(selection, value);
        }

        return value;
    }

    bool SaveConfig()
    {
        const CString configDir = GetConfigDirectory();
        if (!CreateDirectoryW(configDir, nullptr) && GetLastError() != ERROR_ALREADY_EXISTS)
        {
            return false;
        }

        CString json;
        json += L"{\n";
        json += L"  \"OpenMethodPreferences\": {\n";
        for (size_t i = 0; i < kEntries.size(); ++i)
        {
            json += L"    \"";
            json += kEntries[i].key;
            json += L"\": \"";
            json += ComboTarget(i);
            json += L"\"";
            json += (i + 1 == kEntries.size() ? L"\n" : L",\n");
        }
        json += L"  }\n";
        json += L"}\n";

        const int length = WideCharToMultiByte(CP_UTF8, 0, json, -1, nullptr, 0, nullptr, nullptr);
        if (length <= 1)
        {
            return false;
        }

        std::string utf8(static_cast<size_t>(length - 1), '\0');
        WideCharToMultiByte(CP_UTF8, 0, json, -1, utf8.data(), length - 1, nullptr, nullptr);

        HANDLE file = CreateFileW(GetConfigPath(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (file == INVALID_HANDLE_VALUE)
        {
            return false;
        }

        DWORD bytesWritten = 0;
        const BOOL ok = WriteFile(file, utf8.data(), static_cast<DWORD>(utf8.size()), &bytesWritten, nullptr);
        CloseHandle(file);
        return ok && bytesWritten == utf8.size();
    }

    bool RunOpenHost(const wchar_t* argument) const
    {
        const CString openHostPath = CombinePath(GetModuleDirectory(), L"OpenHost.exe");
        if (GetFileAttributesW(openHostPath) == INVALID_FILE_ATTRIBUTES)
        {
            AfxMessageBox(L"未在当前目录找到 OpenHost.exe。", MB_ICONERROR);
            return false;
        }

        CString commandLine;
        commandLine.Format(L"\"%s\" \"%s\"", static_cast<LPCWSTR>(openHostPath), argument);

        STARTUPINFOW startupInfo{};
        startupInfo.cb = sizeof(startupInfo);
        PROCESS_INFORMATION processInfo{};

        const BOOL ok = CreateProcessW(
            nullptr,
            commandLine.GetBuffer(),
            nullptr,
            nullptr,
            FALSE,
            0,
            nullptr,
            GetModuleDirectory(),
            &startupInfo,
            &processInfo);
        commandLine.ReleaseBuffer();

        if (!ok)
        {
            return false;
        }

        WaitForSingleObject(processInfo.hProcess, INFINITE);
        DWORD exitCode = 1;
        GetExitCodeProcess(processInfo.hProcess, &exitCode);
        CloseHandle(processInfo.hThread);
        CloseHandle(processInfo.hProcess);
        return exitCode == 0;
    }

    void SetStatus(const wchar_t* text)
    {
        m_status.SetWindowTextW(text);
    }

    std::vector<BYTE> m_template;
    CFont m_titleFont;
    CStatic m_title;
    CStatic m_hint;
    std::array<CStatic, 4> m_labels;
    std::array<CComboBox, 4> m_combos;
    CButton m_save;
    CButton m_register;
    CButton m_query;
    CButton m_openConfig;
    CStatic m_status;
};

BEGIN_MESSAGE_MAP(COpenHostSettingsDialog, CDialogEx)
    ON_BN_CLICKED(kIdSave, &COpenHostSettingsDialog::OnSave)
    ON_BN_CLICKED(kIdRegister, &COpenHostSettingsDialog::OnRegister)
    ON_BN_CLICKED(kIdQuery, &COpenHostSettingsDialog::OnQuery)
    ON_BN_CLICKED(kIdOpenConfig, &COpenHostSettingsDialog::OnOpenConfig)
    ON_WM_DESTROY()
END_MESSAGE_MAP()

class COpenHostSettingsApp final : public CWinApp
{
public:
    BOOL InitInstance() override
    {
        INITCOMMONCONTROLSEX controls{};
        controls.dwSize = sizeof(controls);
        controls.dwICC = ICC_WIN95_CLASSES | ICC_STANDARD_CLASSES;
        InitCommonControlsEx(&controls);

        CWinApp::InitInstance();
        COpenHostSettingsDialog dialog;
        m_pMainWnd = &dialog;
        dialog.DoModal();
        return FALSE;
    }
};

COpenHostSettingsApp theApp;

extern int AFXAPI AfxWinMain(HINSTANCE instance, HINSTANCE previousInstance, LPTSTR commandLine, int showCommand);

extern "C" int WINAPI WinMain(HINSTANCE instance, HINSTANCE previousInstance, LPSTR, int showCommand)
{
    return AfxWinMain(instance, previousInstance, GetCommandLineW(), showCommand);
}
