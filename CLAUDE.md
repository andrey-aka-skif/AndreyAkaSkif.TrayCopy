# AndreyAkaSkif.TrayCopy

Утилита для Windows в трее: ЛКМ по иконке копирует текущую строку в буфер обмена,
ПКМ выбирает следующую, клик с Shift открывает окно настроек (СКМ не используется:
Windows 11 присылает её как ЛКМ). Отображаемое имя —
`TrayCopy`, всё остальное (решение, проекты, пространства имён, каталог данных) —
`AndreyAkaSkif.TrayCopy`.

Устройство репозитория, сборка и стиль XML-документации — в [CONTRIBUTING.md](./CONTRIBUTING.md).
Образец организации репозитория — `D:\.dev\common\AndreyAkaSkif.ServiceDefaults`.

## Разрешения проекта

В этом репозитории, в отличие от глобального правила, разрешены инфраструктура Claude
(этот файл, `.claude/` со скилами) и трейлер `Co-Authored-By` в коммитах.

## Стек и принятые решения

- .NET 10, `net10.0-windows` (задано в `Directory.Build.props`), Avalonia 12, CommunityToolkit.Mvvm.
- Версии пакетов — только в `Directory.Packages.props` (CPM); у `PackageReference` версий нет.
- Иконка в трее — **H.NotifyIcon** (core-пакет), а не `TrayIcon` Avalonia: встроенный жёстко
  открывает меню по ПКМ и не умеет уведомления. С H.NotifyIcon работает только `TrayIconHost`.
- Буфер обмена — Win32 через P/Invoke, с форматами `ExcludeClipboardContentFromMonitorProcessing`,
  `CanIncludeInClipboardHistory = 0`, `CanUploadToCloudClipboard = 0`, `Clipboard Viewer Ignore`
  (строка не попадает в Win+V, облачный буфер и сторонние менеджеры буфера). Владелец буфера —
  собственное скрытое окно сообщений `Win32Clipboard`, от трея не зависит.
- Настройки — `%APPDATA%\AndreyAkaSkif.TrayCopy\settings.json`; шифрование DPAPI и вид уведомления
  (своё окно / системное) переключаются в окне настроек.
- Тесты — xUnit v3 4.x в режиме Microsoft.Testing.Platform (`global.json`), без пакетов VSTest.
  Запуск: `dotnet test --solution AndreyAkaSkif.TrayCopy.slnx`. Без единого теста этот режим
  завершается с кодом 8, поэтому тестовый проект существует только вместе с тестами.
- Инсталлятор — Inno Setup, установка для текущего пользователя; скрипт
  `installer/AndreyAkaSkif.TrayCopy.iss` совместим с Inno Setup 6.3+ (UTF-8 без BOM; в образе
  `windows-latest` — 6.7.x, `iscc` не в PATH). Упаковывает `artifacts/publish/win-x64` после
  `dotnet publish -p:PublishProfile=win-x64`, версию берёт из exe. `AppId` не менять.
- Профиль публикации `*.pubxml` игнорируется `VisualStudio.gitignore` — в конце `.gitignore`
  для него исключение.
- Версии: локально `0.0.0-local` (`Directory.Build.props`), master — `X.Y.(Z+1)-dev.N` от
  последнего тега, релиз — `X.Y.Z` из тега `vX.Y.Z`; в сборку передаются через `-p:Version`.
- CI — `.github/workflows/ci.yml` на `windows-latest`; на master джоб `installer` выкладывает
  установщик артефактом. Выпуск — `publish.yml` по публикации Release.

## Форматирование

- UTF-8 без BOM, LF (`.gitattributes`, `.editorconfig`); файлы из шаблонов `dotnet new` приводятся
  к этому виду.
- `.slnx` — отступ 2 пробела: так его пишут `dotnet sln` и Visual Studio.

## Порядок работы

Этапы идут строго по одному, каждый — issue, ветка `<тип>/N-описание` от `origin/master` с
`--no-track`, PR с `Closes #N`:

0. рабочее пространство;
1. каркас трея и собственная иконка приложения;
2. модель и хранилище настроек, тестовый проект;
3. окно настроек;
4. действия трея: копирование, переключение, уведомления;
5. инсталлятор, публикация релиза, версия из тега.

Проверка перед коммитом: `dotnet build -c Release` без предупреждений (после этапа 2 —
и `dotnet test --solution AndreyAkaSkif.TrayCopy.slnx -c Release`). При правке установщика
или профиля публикации — ещё `dotnet publish` и `iscc`, если Inno Setup установлен.
