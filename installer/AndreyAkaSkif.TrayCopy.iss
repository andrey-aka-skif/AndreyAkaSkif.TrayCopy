; Установщик TrayCopy для текущего пользователя, без прав администратора. Два варианта:
; со средой .NET и без неё (framework-dependent): второй меньше, но ставится, только если
; в системе есть среда .NET 10.
;
; Упаковывает результат публикации, поэтому сначала публикация, затем iscc. Со средой:
;   dotnet publish src/AndreyAkaSkif.TrayCopy -p:PublishProfile=win-x64
;   iscc installer/AndreyAkaSkif.TrayCopy.iss
; Без среды:
;   dotnet publish src/AndreyAkaSkif.TrayCopy -p:PublishProfile=win-x64-framework-dependent
;   iscc /DFrameworkDependent installer/AndreyAkaSkif.TrayCopy.iss
;
; Версия установщика берётся из опубликованного exe, параметра версии у iscc нет:
; установщик всегда называет ту версию, которую упаковывает. Скрипт в UTF-8 без BOM — нужен
; Inno Setup 6.3 или новее.

#define ExeName "AndreyAkaSkif.TrayCopy.exe"
#ifdef FrameworkDependent
  #define PublishProfile "win-x64-framework-dependent"
  #define OutputSuffix "-setup-framework-dependent"
#else
  #define PublishProfile "win-x64"
  #define OutputSuffix "-setup"
#endif
#define PublishDir AddBackslash(SourcePath) + "..\artifacts\publish\" + PublishProfile
#define ExeFile PublishDir + "\" + ExeName

; Встроенная функция, а не макрос из ISPPBuiltins.iss: макрос в Inno Setup 6 называется
; GetFileProductVersion, в 7 — GetFileProductVersionString
#define ProductVersion GetStringFileInfo(ExeFile, "ProductVersion")
#if ProductVersion == ""
  #pragma error "Нет опубликованного exe: сначала dotnet publish src/AndreyAkaSkif.TrayCopy -p:PublishProfile=" + PublishProfile
#endif
; SDK дописывает к версии продукта sha коммита (1.2.4-dev.5+<sha>); в имени файла и в
; «Установленных приложениях» он не нужен
#define AppVersion Copy(ProductVersion, 1, Pos("+", ProductVersion + "+") - 1)

[Setup]
; Идентификатор приложения для Windows и для обновлений поверх установленного. Не менять:
; с другим AppId новая версия встанет рядом со старой, а не заменит её. Общий для обоих
; вариантов: один ставится поверх другого
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
OutputBaseFilename=TrayCopy-{#AppVersion}{#OutputSuffix}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[InstallDelete]
; Приложение публикуется одним exe. Установка поверх версии, опубликованной папкой,
; оставила бы её файлы: Inno Setup не удаляет то, чего нет в новой версии. Нативные
; библиотеки удаляются вместе с остальными DLL и ставятся заново из [Files]
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.json"
Type: files; Name: "{app}\createdump.exe"

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

#ifdef FrameworkDependent
const
  DotNetDownloadUrl = 'https://dotnet.microsoft.com/download/dotnet/10.0';

{ Каталог .NET, в котором exe приложения будет искать среду, в том же порядке: переменные
  DOTNET_ROOT_X64 и DOTNET_ROOT, место установки из реестра (32-битное представление,
  так его пишет установщик .NET), каталог по умолчанию }
function DotNetRoot: String;
begin
  Result := GetEnv('DOTNET_ROOT_X64');
  if Result = '' then
    Result := GetEnv('DOTNET_ROOT');
  if Result = '' then
    RegQueryStringValue(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64',
      'InstallLocation', Result);
  if Result = '' then
    Result := ExpandConstant('{commonpf64}\dotnet');
end;

{ Есть ли среда .NET 10: каталог версии 10.* в shared\Microsoft.NETCore.App. Приложение
  нуждается только в ней, она входит и в .NET Desktop Runtime, и в .NET SDK }
function IsDotNet10Installed: Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(AddBackslash(DotNetRoot) + 'shared\Microsoft.NETCore.App\10.*', FindRec) then
    try
      repeat
        Result := (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0;
      until Result or not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
end;

{ Без среды .NET 10 установка не начинается: среда ставится на всю систему с правами
  администратора, а этот установщик их не запрашивает. В тихом режиме страница загрузки
  не открывается }
function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := IsDotNet10Installed;
  if not Result and (SuppressibleMsgBox(
    'Для этого установщика TrayCopy нужна среда .NET 10 (.NET Runtime 10, x64), а она ' +
    'не найдена.' + #13#10#13#10 +
    'Установите среду .NET и запустите установку снова или скачайте установщик ' +
    'TrayCopy-{#AppVersion}-setup.exe — в нём среда .NET уже есть.' + #13#10#13#10 +
    'Открыть страницу загрузки .NET?',
    mbError, MB_YESNO, IDNO) = IDYES) then
    ShellExec('open', DotNetDownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;
#endif
