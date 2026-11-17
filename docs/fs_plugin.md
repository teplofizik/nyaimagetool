# Плагин файловой системы

Как добавить формат ФС, чтобы он работал и в `store`, и в layout (`build`,
`mkimage` → `store`), и в слотах NAND/диска.

## 1. Класс

Наследуйтесь от `NyaFs.ImageFormat.Plugins.Base.FilesystemPlugin`:

```csharp
class MyFsFilesystemPlugin : FilesystemPlugin
{
    public MyFsFilesystemPlugin()
        : base("fs.myfs", "myfs",
               Read: true, Write: true, InternalCompression: false)
    { }

    public override ImageFormat.Types.FsType FsType => ImageFormat.Types.FsType.Unknown;
    public override string[] Aliases => new[] { "my" };
    public override string[] Extensions => new[] { "myfs" };
}
```

`InternalCompression` — только для форматов, которые сами сжимают данные
(squashfs, ubifs, jffs2). Для gzip/xz снаружи используйте компрессор-плагины.

## 2. Обязательный контракт

| член | что делает |
| --- | --- |
| `DetectFilesystem(ImageData)` | проверка магии; учитывайте `Image.Offset` |
| `GetFilesystemReader(byte[])` | reader для `store`/`load`; верните `null`, если не ваш формат |
| `GetFilesystemWriter(LinuxFilesystem)` | writer на каждую запись; создавайте новый builder |
| `CheckFilesystem(LinuxFilesystem)` | может ли формат представить дерево (симлинки, uid/gid, xattr, размер имён) |
| `SupportWrite` | унаследован от флага `Write` в конструкторе |
| `FsType` | `FsType` для детекта в пайплайне; `Unknown`, если своего типа нет |
| `DisplayName`, `Aliases`, `Extensions` | CLI-имена и расширения |

`GetFilesystemWriter` не должен писать в файл: `BaseFsWriter(builder)`
вызывает `builder.GetFilesystemImage()` и отдаёт `RawStream`.

## 3. Опции

Декларируйте параметры в `Options`:

```csharp
public override FilesystemOption[] Options => new[]
{
    new FilesystemOption("compression", FilesystemOptionType.Enum, "gzip",
        "Data compression", new[] { "gzip", "lz4" }),
    new FilesystemOption("block-size", FilesystemOptionType.Size, "4096",
        "Block size")
};
```

Правила:

- канонические имена — `<format>.<param>` (`squashfs.compression`); без
  префикса layout принимает их, только когда `format` узла совпадает;
- значения складывайте в **публичные поля** `LinuxFilesystem`
  (`SquashFsCompression`, `FatType`, `Jffs2PageSize`, ...), а не в приватное
  состояние: layout и `set` создают/меняют один и тот же объект;
- переопределяйте `TryApplyOption`, только если нужна особая логика
  (например, `ubifs.container`); иначе базовый класс проверит тип и положит
  значение в `Fs.SetFilesystemOption`;
- зарезервировано ядром: `size`, `fs-size`, `fill`, `plan.*` — не объявляйте
  их как опции формата;
- неизвестная опция — ошибка с понятным текстом, не молчаливый игнор.

## 4. Размер — единое правило

`size` в layout — это **размер региона/итогового файла**, а не размер ФС.

- writer собирает **минимальный валидный образ**; не добивайте его до слота,
  eraseblock или `size` — это делает `PlanPacker`/контейнер;
- если формат имеет фиксированный размер тома (`ext`, `fat`, `f2fs`, MCU),
  уважайте `Fs.FsSize`: `> 0` — целевой размер тома, `0` — естественный
  размер/дефолт. При нехватке места возвращайте ошибку, а не обрезайте;
- content-sized форматы (`squashfs`, `ubifs`, `cramfs`, `erofs`, `romfs`,
  `cpio`, `tar`) `Fs.FsSize` игнорируют: регион добивается снаружи;
- исключение — форматы, где padding является частью формата (`jffs2`,
  `yaffs2`): добивайте до eraseblock сами и опишите это в документации;
- вывод должен быть детерминированным (без времени/рандома, если формат это
  допускает).

Layout передаёт `fs-size` только в `Fs.FsSize`; `size` в writer не попадает.

## 5. Содержимое и валидация

- дерево приходит в `LinuxFilesystem.Fs` (`Filesystem.Universal`);
- `CheckFilesystem` должен отклонить невыразимое: симлинки, uid/gid за
  пределами полей, xattr, слишком длинные имена, неподдерживаемые типы;
- не меняйте `Fs` в writer, если это не требуется; writer вызывается на
  каждом `store`;
- ошибки — через `out string Error` (валидация) или исключение с понятным
  текстом (builder); `build`/`store` покажут его пользователю.

## 6. Регистрация и тесты

- встроенный плагин — в `BuiltinPlugins`; внешний — DLL + `loadplugin`;
  override — через `Priority`;
- тесты минимум:
  1. round-trip: `GetFilesystemImage()` → `GetFilesystemReader()` → сверка
     файлов;
  2. standalone layout: `format: myfs`, `content.dir`, опции;
  3. слот `kind: filesystem` в NAND/диске (размер слота, padding);
  4. `fs-size`: том меньше/больше региона, слишком маленький регион;
  5. `layout validate` и `store ... all myfs`.

## 7. Пример

`SquashFsFilesystemPlugin` — опции → `Fs.SquashFsCompression`, writer
`new SquashFsBuilder(Fs.SquashFsCompression)`; `Ext4FilesystemPlugin` —
`Fs.FsSize` как размер тома; `FatFilesystemPlugin` — `Fs.FatType` и
`Fs.FsSize`; `Jffs2FilesystemPlugin` — опции `jffs2.*` и padding до
eraseblock внутри writer.
