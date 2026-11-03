; Inno Setup betiği — derleme: ISCC.exe installer\CiroApp.iss  (önce publish\windows üretilmiş olmalı)
; Uygulama kendi .NET çalışma zamanını ve Windows App SDK'yı taşır. Temiz bir bilgisayarda eksik olabilecek
; WebView2 ve Visual C++ çalışma kitaplığı, yoksa kurulum sırasında Microsoft'tan indirilip kurulur.
#define AppName "Ciro"
#define AppVersion "1.0.0"
#define AppExe "CiroApp.exe"

[Setup]
AppId={{7D3B6C1E-5A42-4F0B-9C6E-2B8F1A4D9E33}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.17763
OutputDir=output
OutputBaseFilename=Ciro-Kurulum-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstüne kısayol oluştur"; GroupDescription: "Ek görevler:"; Flags: unchecked

[Files]
Source: "..\publish\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Ciro'yu şimdi başlat"; Flags: nowait postinstall skipifsilent

[Code]
const
  WebView2Url = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';
  WebView2File = 'MicrosoftEdgeWebview2Setup.exe';
  VCRedistUrl = 'https://aka.ms/vs/17/release/vc_redist.x64.exe';
  VCRedistFile = 'vc_redist.x64.exe';
  WebView2Key = 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

var
  DownloadPage: TDownloadWizardPage;
  NeedWebView2: Boolean;
  NeedVCRedist: Boolean;

function WebView2Installed: Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(HKLM32, WebView2Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
  if not Result then
    Result := RegQueryStringValue(HKCU, WebView2Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function VCRedistInstalled: Boolean;
var
  Installed: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and (Installed = 1);
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID <> wpReady then
    Exit;

  NeedWebView2 := not WebView2Installed;
  NeedVCRedist := not VCRedistInstalled;
  if not (NeedWebView2 or NeedVCRedist) then
    Exit;

  DownloadPage.Clear;
  if NeedWebView2 then
    DownloadPage.Add(WebView2Url, WebView2File, '');
  if NeedVCRedist then
    DownloadPage.Add(VCRedistUrl, VCRedistFile, '');

  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
    except
      if DownloadPage.AbortedByUser then
        Log('Gerekli bileşenlerin indirilmesi kullanıcı tarafından iptal edildi.')
      else
        SuppressibleMsgBox('Gerekli bileşenler indirilemedi. İnternet bağlantınızı kontrol edip kurulumu yeniden çalıştırın.' + #13#10 + GetExceptionMessage, mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    DownloadPage.Hide;
  end;
end;

procedure RunPrerequisite(const FileName, Parameters, Title: String);
var
  ResultCode: Integer;
begin
  // Yönetici onayı (UAC) istenir; reddedilirse uygulama açılamayabileceği için kullanıcı uyarılır.
  if not ShellExec('runas', ExpandConstant('{tmp}\' + FileName), Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    SuppressibleMsgBox(Title + ' kurulamadı. Ciro açılmazsa bu bileşeni elle kurun.', mbError, MB_OK, IDOK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    if NeedVCRedist then
      RunPrerequisite(VCRedistFile, '/install /quiet /norestart', 'Visual C++ çalışma kitaplığı');
    if NeedWebView2 then
      RunPrerequisite(WebView2File, '/silent /install', 'Microsoft WebView2');
  end;
end;
