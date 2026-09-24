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
