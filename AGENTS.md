# AGENTS.md — nyaimagetool

Памятка по репозиторию CLI (`nyaimagetool`). Потребляет `NyaFs` как NuGet-пакет.

## Связь с другими репозиториями

- Разработка `NyaFs` ведётся в **`G:\Projects\Асик конфиг\nyaimagetool_dev`** (исходники + тесты + плагины). Там же полный `AGENTS.md` с деталями форматов и отладки.
- Здесь `NyaFs` подключается как пакет: `Include="NyaFs" Version="X"` во всех `*.csproj`. Источник — локальный feed `G:\Projects\local.nuget`.
- Публикуемая библиотека — **`G:\Projects\Асик конфиг\nyafs`** (`src/NyaFs`).

### Как обновить NyaFs здесь
1. В `nyafs` поднять `<Version>`, `dotnet pack ... -o G:\Projects\local.nuget`.
2. В этом репо заменить `Include="NyaFs" Version="старая"` → новая во всех `*.csproj`.
3. `dotnet build NyaImageTool.sln -c Debug -maxcpucount:1` и `dotnet test ... --no-build`.

## Сборка и тесты

```
dotnet build NyaImageTool.sln -c Debug -maxcpucount:1
dotnet test  NyaImageTool.sln -c Debug --no-build
```

- Ожидаемый результат: **134/134** (в dev — 142/142; разница — скрытые/приватные тесты).
- Регрессия читает corpus `F:\TEMP\Images` (env `NYAIMAGETOOL_IMAGES`) и сверяет с `NyaFsTests/Regression/golden.json`.
- Перегенерация golden: `$env:NYAIMAGETOOL_REGEN="1"; dotnet test ... --no-build`. **Всегда** бэкапить golden и сверять `Compare-Object` до/после (принимать только чистые добавления/осознанные изменения).

## Окружение (Windows / PowerShell)

- Пути с пробелами/кириллицей ломают парсер `.nyas` — копировать образы в `F:\TEMP`.
- `rg` нет → `Select-String`. `python -c @'...'@` ломает кавычки → скрипт в файл.
- Временные файлы — в `F:\TEMP` (не в `%TEMP%\opencode`).
- **`write`/`edit` + `bash` в одном сообщении идут параллельно** → сначала правки, потом сборка.
- PowerShell: `$var:` = диск → `${var}:`; `-shl` на byte переполняется → касты `[int]`.

## Архитектура (кратко)

Всё на плагинах: ядро `NyaFs` не знает форматов; детект/чтение/запись — через `NyaPlugin`-наследников (`CompositeImagePlugin`, `FilesystemPlugin`, `KernelPlugin`, `DevtreePlugin`, `CompressorPlugin`, `ServicePlugin`, `CommandPlugin`). Новая фича = новый плагин, без правки ядра. `ImagePipeline` — рекурсивная загрузка; `store` маршрутизируется через плагины; `NyaPlugin.Priority` — override.

## Отладка

1. Гипотеза → побайтовое сравнение с валидным эталоном (или лог oracle) → точечная правка → пересборка.
2. Для UBI/UBIFS oracle — Python `ubi_reader` (`ubireader_extract_files -l <img> -o out/`) на Ubuntu; для ext — `e2fsck -fn`.
3. После крупной правки — полный regression + diff golden.
4. Временные тесты удалять.
