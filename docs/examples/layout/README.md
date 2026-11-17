# Примеры сборки образов NAND по раскладке

Каталог показывает, как собирать **линейные** образы NAND (без OOB/ECC) двумя
способами:

- **декларативно** — `build <schema.yaml> <out>` (всё содержимое описано в схеме);
- **командами** — `mkimage` создаёт план, затем `use`/`import`/`mkfs`/`loaddir`/`file`
  наполняют слоты, `store` пишет результат.

Полное описание формата схемы, API плагинов и кэша: `docs/build_plan.md`.

## Требования

- Сборка всегда даёт линейный образ: OOB/ECC не генерируются. Полученный файл
  предназначен для прошивальщика (`nandwrite`, вендорский инструмент), который
  сам считает ECC.
- Ноды `content: { dir: ... }` собирают файловую систему в памяти; файлы читаются
  из указанных каталогов, промежуточные файлы не создаются.

## Что лежит в `inputs/`

Файлы/каталоги-заготовки (в репозиторий не входят, подготовьте свои):

| путь | назначение |
| ---- | ---------- |
| `inputs/bootloader.bin` | образ загрузчика (для secure boot — готовый зашифрованный блоб) |
| `inputs/tpl.bin` | образ tpl/TZ |
| `inputs/recovery.img`, `inputs/boot.img` | готовые Android boot образы |
| `inputs/Image` | ядро (raw/Image, zImage, gzip — определится автоматически) |
| `inputs/rootfs/` | каталог ramdisk (для cpio/ext/…) |
| `inputs/config_root/`, `inputs/nvdata_root/` | каталоги для UBIFS |
| `inputs/board.dtb` | device tree (опционально) |

## Примеры

| файл | что показывает |
| ---- | -------------- |
| `nand-empty.yaml` | пустая раскладка (256 МиБ `0xFF`) |
| `nand-raw-partitions.yaml` | raw-разделы, повторные копии (`repeat`) |
| `nand-fit-boot.yaml` | FIT: ядро из файла + ramdisk из каталога |
| `nand-android-boot.yaml` | Android boot image из ядра и ramdisk |
| `nand-ubifs-config.yaml` | UBIFS/UBI-раздел из каталога |
| `nand-full-s21pro.yaml` | полная раскладка Amlogic S21 Pro (bootloader/tpl/recovery/boot/config/nvdata) |
| `roundtrip.nyas` | `layout export` → `build` (восстановление из дампа) |
| `command-mode.nyas` | наполнение плана командами |

Сборка:

```
NyaImageTool.exe
> build nand-empty.yaml out-empty.bin
> build nand-raw-partitions.yaml out-raw.bin
> build nand-fit-boot.yaml out-fit.bin
> build nand-android-boot.yaml out-android.bin
> build nand-ubifs-config.yaml out-config.bin
```

Или одним скриптом (запускать из этого каталога):

```
NyaImageTool.exe build-all.nyas
```

## Размеры и выравнивание

- `offset` не обязателен: без него слот ставится последовательно по возрастанию.
- `size: auto` — размер содержимого, округлённый до размера eraseblock.
- Если `size` задан явно, содержимое должно в него влезть; остаток слота
  заливается `0xFF`.
- `repeat: { count, stride }` — несколько копий; `size` (или `auto`) задаёт
  размер **одной** копии, `stride` — расстояние между копиями. Область между
  копиями остаётся `0xFF`.

## Ограничения

- Только линейные образы (raw с OOB/ECC не пишется).
- Мультитомный UBI в одной ноде (`format: ubi` с несколькими томами) пока не
  собран; отдельные UBI-разделы (`format: ubifs`) работают.
- Сжатие ramdisk (`content.compression`) пока не применяется: используйте
  готовый `.cpio.gz` через `content.file`.
- Secure boot (AES/подпись) не пересобирается: загрузчик/tpl кладутся как есть.
- Готовые контейнеры (FIT/Android), полученные из `layout export`, пересобираются
  как immutable-файлы; для пофайловой сборки опишите их дочерними нодами.

## Диск (MBR/GPT)

`format: disk` собирает таблицу и раскладывает payload'ы по слотам; свободное
место — нули. Примеры: `disk-gpt.yaml` (GPT, primary+backup, CRC32, имена
разделов) и `disk-mbr.yaml` (4 раздела максимум, `mbr-type`).

```
> build disk-gpt.yaml disk.img
> build disk-mbr.yaml disk-mbr.img
```

## Вендорские контейнеры (шаблон)

`rkfw-template.yaml` и `swu-template.yaml` берут заголовок/loader (RKFW) или
CPIO+`sw-description` (SWUpdate) из `inputs/update.img` / `inputs/package.swu`,
а из раскладки — только заменяемые/добавляемые записи. Без `options.template`
эти форматы не собираются.

## Standalone filesystems

`fs-squashfs.yaml` and `fs-fat.yaml` are images with a filesystem at the root:
`format` is the filesystem, `content.dir` is the payload and `options` carry the
format parameters (compression, fat type, size, jffs2/erofs/udf settings).

```
> build fs-squashfs.yaml root.squashfs
> build fs-fat.yaml efi.img
> layout validate fs-squashfs.yaml
```
