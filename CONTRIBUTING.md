# Разработка

Внутреннее устройство репозитория: как приложение собирается и чем проверяется.

## Требования

Windows и .NET SDK 10. Приложение собирается под `net10.0-windows` (задано в
[Directory.Build.props](./Directory.Build.props)) и опирается на Win32 API, поэтому
собирается и запускается только под Windows.

```shell
dotnet restore
```

## Структура

```
assets/icon/                          исходники иконки приложения и скрипт сборки app.ico
src/AndreyAkaSkif.TrayCopy/           приложение, Avalonia
tests/AndreyAkaSkif.TrayCopy.Tests/   тесты, xUnit v3
```

Версии пакетов заданы централизованно, в
[Directory.Packages.props](./Directory.Packages.props); в `.csproj` у `PackageReference`
версий нет.

## Сборка и запуск

```shell
dotnet build --configuration Release
```

```shell
dotnet run --project src/AndreyAkaSkif.TrayCopy
```

В Debug-сборку подключается поддержка Avalonia DevTools (`AvaloniaUI.DiagnosticsSupport`),
в Release её нет.

Главного окна у приложения нет: после запуска оно живёт иконкой в трее. Windows 11 прячет
новые иконки в область «^» — иконку можно перетащить на панель задач. Вторая копия при
работающей первой сразу завершается. Пока нет окна настроек, приложение закрывается средним
кликом по иконке.

## Зависимости

Сервисы приложения создаёт контейнер Microsoft.Extensions.DependencyInjection; регистрации
собраны в [App.axaml.cs](./src/AndreyAkaSkif.TrayCopy/App.axaml.cs). Контейнер проверяет
регистрации при построении, создаёт сервисы при первом обращении и освобождает их при
выходе из приложения. Параметры сервисов передаются через Options pattern
(`Microsoft.Extensions.Options`): класс параметров со значениями по умолчанию
регистрируется `AddOptions<T>()`, сервис получает `IOptions<T>`.

## Тесты

```shell
dotnet test --solution AndreyAkaSkif.TrayCopy.slnx
```

Тесты написаны на xUnit v3 и запускаются в режиме Microsoft.Testing.Platform: он задан в
[global.json](./global.json), пакетов VSTest в проекте нет. Тестовая сборка — исполняемый
файл. Если в проекте нет ни одного теста, `dotnet test` в этом режиме завершается с
кодом 8, поэтому тестовый проект без тестов не держится.

## Настройки

Записи и выбор текущей записи — неизменяемый `EntryList` в
[Entries](./src/AndreyAkaSkif.TrayCopy/Entries): текущая запись всегда из списка, следующая
после последней — первая. Настройки (`AppSettings`) включают список записей, вид
уведомления и защиту значений.

Текущие настройки держит `SettingsService`. Потребители берут `Current` в момент
использования, а не хранят копию; `Update` сохраняет изменение, заменяет `Current` и
поднимает событие `Changed`.

Сохраняет настройки реализация `ISettingsStore`. Интерфейс и общее для любых способов
хранения (`ValueProtector`) лежат в
[Settings/Persistence](./src/AndreyAkaSkif.TrayCopy/Settings/Persistence), каждый способ —
в своей подпапке. Модель о способе хранения не знает. Нынешняя реализация,
`JsonSettingsStore` из `Persistence/Json`, хранит настройки в файле, путь к которому задаёт
`JsonSettingsStoreOptions`; по умолчанию это
`%APPDATA%\AndreyAkaSkif.TrayCopy\settings.json`:

```json
{
  "version": 1,
  "notification": "popup",
  "protection": "dpapi",
  "selected": "github",
  "entries": [
    { "name": "github", "value": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA…" }
  ]
}
```

- `notification` — вид уведомления: `popup` (своё окно) или `system` (системное).
- `protection` — `dpapi`: значения зашифрованы DPAPI для текущего пользователя Windows и
  записаны в base64; `none` — открытый текст. При сохранении в другом режиме
  переписываются все значения.
- `selected` — имя текущей записи, у пустого списка `null`; если записи с таким именем
  нет, текущей становится первая.
- Порядок `entries` — порядок переключения записей.

Все поля обязательны. Файл перезаписывается целиком, через временный `settings.json.tmp`.
Нечитаемый файл (повреждённый, без обязательного поля, зашифрованный другим пользователем
или на другом компьютере, записанный в неизвестной версии формата) переносится рядом в
`settings.json.<дата-время>.bak`, а настройки начинаются с пустого списка.

## Иконка

Иконка приложения — [app.ico](./src/AndreyAkaSkif.TrayCopy/Assets/app.ico): значок exe и
иконка в трее, которая берёт из файла кадр под текущий масштаб экрана. Файл собирается из
SVG в [assets/icon](./assets/icon) и хранится в репозитории; сборка решения и CI его не
пересобирают.

| Кадр | Исходник | Где виден |
|---|---|---|
| 16, 20, 24 px | `icon-16.svg`, `icon-20.svg`, `icon-24.svg` | трей при масштабе 100, 125, 150 % |
| 32, 40, 48, 64, 256 px | `icon.svg` | трей при 200–250 %, Explorer, «Пуск» |

Мелкие кадры нарисованы отдельно, каждый по пиксельной сетке своего размера: при
растяжении общего рисунка тонкие детали расплываются. Правка рисунка — в соответствующих
SVG, затем пересборка:

```shell
dotnet run assets/icon/build-icon.cs
```

Скрипт — file-based app .NET 10; версия пакета `Svg.Skia`, которым он рисует SVG, задана
в `Directory.Packages.props`. Ключ `--preview <файл.png>` дополнительно сохраняет лист
всех кадров на светлом и тёмном фоне (каталог `artifacts/` в `.gitignore`):

```shell
dotnet run assets/icon/build-icon.cs -- --preview artifacts/icon-preview.png
```

## CI

Воркфлоу [ci.yml](./.github/workflows/ci.yml) собирает решение и прогоняет тесты на пуш в
любую ветку. Раннер — `windows-latest`: сборка под `net10.0-windows` требует Windows, а
тесты хранилища настроек — DPAPI.

Версии экшенов и пакетов поднимает dependabot
([dependabot.yml](./.github/dependabot.yml)); мажоры — вручную.

## Стиль XML-документации

`<summary>` типа и метода пишется в изъявительном наклонении, третьим лицом: тип
говорит, чем он является или что предоставляет («Предоставляет…», «Представляет…»,
«Определяет…»), метод — что он делает («Копирует…», «Сохраняет…», «Выбирает…»). Ни
инфинитива («Скопировать…»), ни отглагольного существительного («Копирование строки…»).

Глагол-действие принадлежит имени метода, а `<summary>` говорит, что метод делает.
Того же требует и Microsoft: правила описания символов вынесены в
[.NET API docs wiki](https://github.com/dotnet/dotnet-api-docs/wiki/Summary).

> [!NOTE]
> Begin with a present-tense, third-person verb, except for exception classes,
> enum members, abstract members, and virtual members.
>
> — [dotnet/dotnet-api-docs wiki, Summary](https://github.com/dotnet/dotnet-api-docs/wiki/Summary)

Описания свойств, полей, констант и элементов перечислений — именные: «Имя записи»,
«Путь к файлу настроек».

Точка в конце `<summary>` не ставится: описания здесь — короткие именные и глагольные
группы, а не предложения. Это осознанное расхождение с
[рекомендациями по XML-тегам](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags).
