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
assets/icon/                  исходники иконки приложения и скрипт сборки app.ico
src/AndreyAkaSkif.TrayCopy/   приложение, Avalonia
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

Воркфлоу [ci.yml](./.github/workflows/ci.yml) собирает решение на пуш в любую ветку.
Раннер — `windows-latest`: сборка под `net10.0-windows` требует Windows.

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
