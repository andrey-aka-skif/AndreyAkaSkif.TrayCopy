; Установщик TrayCopy для текущего пользователя, без прав администратора.
;
; Упаковывает результат публикации по профилю win-x64, поэтому сначала:
;   dotnet publish src/AndreyAkaSkif.TrayCopy -p:PublishProfile=win-x64
; затем:
;   iscc installer/AndreyAkaSkif.TrayCopy.iss
;
; Версия установщика берётся из опубликованного exe, параметров у iscc нет: установщик
; всегда называет ту версию, которую упаковывает. Скрипт в UTF-8 без BOM — нужен
; Inno Setup 6.3 или новее.

#define ExeName "AndreyAkaSkif.TrayCopy.exe"
#define PublishDir AddBackslash(SourcePath) + "..\artifacts\publish\win-x64"
#define ExeFile PublishDir + "\" + ExeName

; Встроенная функция, а не макрос из ISPPBuiltins.iss: макрос в Inno Setup 6 называется
; GetFileProductVersion, в 7 — GetFileProductVersionString
#define ProductVersion GetStringFileInfo(ExeFile, "ProductVersion")
#if ProductVersion == ""
  #error Нет опубликованного exe: сначала dotnet publish src/AndreyAkaSkif.TrayCopy -p:PublishProfile=win-x64
#endif
; SDK дописывает к версии продукта sha коммита (1.2.4-dev.5+<sha>); в имени файла и в
; «Установленных приложениях» он не нужен
#define AppVersion Copy(ProductVersion, 1, Pos("+", ProductVersion + "+") - 1)

[Setup]
; Идентификатор приложения для Windows и для обновлений поверх установленного. Не менять:
; с другим AppId новая версия встанет рядом со старой, а не заменит её
AppId={{82E8D980-9023-4FE1-848F-2E51F176DF80}
AppName=TrayCopy
AppVersion={#AppVersion}
AppPublisher=andrey-aka-skif
AppPublisherURL=https://github.com/andrey-aka-skif/AndreyAkaSkif.TrayCopy
AppSupportURL=https://github.com/andrey-aka-skif/AndreyAkaSkif.TrayCopy/issues
AppUpdatesURL=https://github.com/andrey-aka-skif/AndreyAkaSkif.TrayCopy/releases
VersionInfoVersion={#GetVersionNumbersString(ExeFile)}

; Установка для текущего пользователя: {autopf} — %LocalAppData%\Programs, ярлык — в
; «Пуске» этого пользователя, UAC не запрашивается
PrivilegesRequired=lowest
DefaultDirName={autopf}\TrayCopy
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Мьютекс, которым приложение держит единственную копию (SingleInstance): пока оно
; запущено, установщик и деинсталлятор просят его закрыть
AppMutex=Local\AndreyAkaSkif.TrayCopy

SetupIconFile=..\src\AndreyAkaSkif.TrayCopy\Assets\app.ico
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName=TrayCopy

OutputDir=..\artifacts\installer
OutputBaseFilename=TrayCopy-{#AppVersion}-setup
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
; Отладочные символы не ставятся: нативные .pdb Skia и HarfBuzz весят около 100 МБ
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Ярлык в «Пуске» при работающем приложении открывает окно настроек
Name: "{autoprograms}\TrayCopy"; Filename: "{app}\{#ExeName}"

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,TrayCopy}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  ApprovalKey = 'Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run';
  AutostartValue = 'AndreyAkaSkif.TrayCopy';

{ Снимает автозапуск установленной копии: значение в Run и отметку Диспетчера задач.
  Правило то же, что в приложении (RunKeyAutostart): значение с путём другой копии,
  например отладочной сборки, не трогается. Настройки в %APPDATA% остаются }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
begin
  if CurUninstallStep <> usUninstall then
    Exit;

  if RegQueryStringValue(HKCU, RunKey, AutostartValue, Command) and
     (CompareText(Command, '"' + ExpandConstant('{app}\{#ExeName}') + '"') = 0) then
  begin
    RegDeleteValue(HKCU, RunKey, AutostartValue);
    RegDeleteValue(HKCU, ApprovalKey, AutostartValue);
  end;
end;
