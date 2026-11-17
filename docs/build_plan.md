# Сборка композитных образов по раскладке (build plan)

Документ описывает механизм «создать образ с нуля»: описать раскладку образа
(разделы/элементы, их размеры и смещения), наполнить её содержимым и собрать
итоговый образ — без исходного образа-шаблона.

Механизм не знает конкретных форматов. Формат предоставляет плагин, поэтому
схема работает и для новых файловых систем/контейнеров: достаточно загрузить
плагин, реализующий контракт из раздела «API плагинов».

## Цели

- Собрать линейный образ (NAND/MTD без OOB/ECC), образ диска (MBR/GPT),
  вендорские композитные прошивки (RKFW, SWUpdate, ...) и т.п. по описанию.
- Использовать уже существующую парадигму редактирования: `use`, `import`,
  `mkfs`, `dir`, `file`, `loaddir`, `set`, `store`.
- Не писать промежуточные файлы: содержимое разделов собирается в памяти,
  на диск попадает только финальный образ.
- Допускать параллельную сборку нескольких образов: состояние на сборку своё,
  общий только read-only кэш.

## Не цели

- Регенерация raw-образов NAND с OOB/ECC — отдельная задача (нужен профиль
  ECC/скрамблера контроллера).
- Обход secure boot: ключи/подписи должны быть предоставлены отдельно.
- Вендорская служебная информация, которую нельзя вывести из раскладки,
  задаётся явно в `options` плагина.

## Основные понятия

- **Plan** — дерево `ImageBuildPlan`. Корень — образ целиком, дети — разделы
  или элементы контейнера.
- **Слот** — метаданные размещения ноды внутри родителя: `offset`, `size`,
  `align`, `repeat`, `fill`. Для простых форматов («блобы по смещениям»)
  слоты пакуются хелпером `PlanPacker`; сложные форматы используют их сами.
- **Нода** — элемент плана: `name`, `kind` (`kernel`, `filesystem`, `devtree`,
  `raw`, `container`), `format` (имя формата/плагина), `options`, `children`,
  `content`.
- **content** — источник содержимого ноды (см. ниже). Если `content` нет,
  нода наполняется командами (`use` + `import`/`mkfs`/`loaddir`/`file`).
- **immutable** — флаг политики кэша/бандла: содержимое не меняется между
  сборками и может адресоваться по хэшу.

## Схема (YAML; JSON принимается тоже)

Оба формата парсятся в одну модель. YAML удобен для ручного редактирования
(комментарии, якоря/алиасы). `schema` — версия схемы; неизвестные поля
сохраняются (forward compatibility), чтобы схему можно было расширять.

```yaml
schema: 1
format: nand                      # формат корня (резолвится через плагины)
options:                          # опции плагина корня
  page: 2048
  oob: 128
  block: 131072
  total: 0x10000000
children:
  - name: bootloader
    offset: 0x0
    size: 0x200000
    kind: raw
    immutable: true
    repeat: { count: 8, stride: 0x40000 }
    content: { blob: "sha256:..." }

  - name: os
    offset: 0x2200000
    size: auto
    kind: container
    format: fit
    children:
      - { name: kernel,  kind: kernel,     content: { file: "vmlinuz" } }
      - { name: ramdisk, kind: filesystem, format: cpio,
          content: { dir: "rootfs", compression: gzip } }
      - { name: devtree, kind: devtree,    content: { file: "board.dtb" } }

  - name: config
    offset: 0x4200000
    size: auto
    kind: container
    format: ubi
    options: { peb: 131072, page: 2048 }
    children:
      - { name: config_data, kind: filesystem, format: ubifs,
          content: { dir: "config_root" } }

  - name: spare
    size: 0x200000
    content: { empty: true }
```

### Поля слота

| поле | смысл |
| ---- | ----- |
| `name` | имя ноды/раздела (используется в путях `use`/`import`) |
| `offset` | смещение в родителе; можно не задавать — автоукладка |
| `size` | размер слота; `auto` — округлить содержимое по правилам формата |
| `align` | выравнивание автоукладки (по умолчанию — выравнивание формата) |
| `repeat` | `{ count, stride }` — несколько копий на одинаковом расстоянии |
| `fill` | чем заполнять свободное место (по умолчанию `0xFF`) |
| `kind` | `kernel`, `filesystem`, `devtree`, `raw`, `container` |
| `format` | формат содержимого/контейнера (имя плагина) |
| `options` | произвольные ключи, интерпретируются плагином |
| `immutable` | политика кэша/бандла |
| `content` | источник содержимого |

### Варианты content

| content | смысл | статус |
| ------- | ----- | ------ |
| `{ file: path }` | байты файла как есть (работает и для контейнеров) | реализовано |
| `{ empty: true }` | оставить стёртым (`fill`) | реализовано |
| `{ dir: path, fs: name }` | собрать ФС из каталога (файлы/каталоги/симлинки) | реализовано |
| `{ blob: "sha256:<hex>" }` | взять из `<schema>.blobs`, каталога схемы или локального кэша | реализовано |
| нет `content` | нода наполняется командами | реализовано |

Дополнительно `content` может содержать `compression`, `exclude` и другие
ключи — их читает соответствующий плагин/писатель.

Если нода с `content` затем редактируется командой, команда перекрывает
источник; в `layout export` попадёт уже результат правок.

## Команды

| команда | назначение |
| ------- | ---------- |
| `layout export <image> <schema.yaml>` | экспорт раскладки; immutable-содержимое кладётся в `<schema>.blobs/` | реализовано |
| `layout validate <schema.yaml>` | проверка схемы без сборки | реализовано |
| `mkimage <format> <schema.yaml>` | создать план (слоты) для дальнейшего редактирования командами | реализовано |
| `build <schema.yaml> <out>` | собрать образ по схеме (декларативный путь) | реализовано |
| `pack <schema.yaml> <bundle>` | упаковать схему и кэшированные блобы в бандл | планируется |
| `use`, `import`, `mkfs`, `dir`, `file`, `loaddir`, `set`, `store` | существующие команды редактирования плана |

`store` для корня формата, поддерживающего layout, вызывает
`CompositeImagePlugin.Build`, который различает два режима: «шаблон + замены»
(есть `Root.Raw`) и «с нуля по слотам» (`Root.Raw == null`).

## Два пути, один план

**Декларативный** (всё в схеме):

```
build nand.yaml out.bin
```

**Командный** (схема только как каркас):

```
mkimage nand nand.yaml
use all/os/kernel
import vmlinuz
use all/os/ramdisk
mkfs
loaddir / rootfs
file /etc/serial 644 0 0
use all/os/devtree
import board.dtb
use all/config/config_data
mkfs
loaddir / config_root
store out.bin
```

Пути адресуются через план: `all/<format>/<имя>/...`; сегменты, совпадающие с
форматом/именем контейнера (`fit`, `ubi`, ...) и `all`, можно опускать.

Смешивание: неизменяемые части описывают через `content`, изменяемые собирают
командами.

## Кэш и бандлы

- Ссылки `blob: sha256:<hex>` разрешаются в порядке: бандл → кэш → исходный
  `file` (если он указан). При попадании файла его хэш вычисляется и блоб
  добавляется в кэш.
- Бандл — самодостаточный файл на базе `NyaPackage` (gzip+tar):
  `PACKAGE` (метаданные, `kind: build`), `data/schema.yaml`,
  `data/blobs/<sha256>`, при необходимости `data/overlay/`.
- Кэшируются и производные результаты (например, собранные ФС/UBI/FIT):
  ключ = хэш поддерева схемы + версия инструмента + параметры.
- Детерминизм: UUID (GPT/ext), временные метки и `image_seq` UBI задаются
  seed'ом (`options.seed`), иначе побайтовое сравнение и кэш теряют смысл.
- Конкурентность: кэш только читается; запись атомарная (временный файл +
  rename) по ключу. Каждая сборка использует свой процессор/план.

## API плагинов

Ядро предоставляет конверт и слоты; форматы реализуют своё поведение.

```csharp
public class PlanDocument { /* дерево PlanElement: name, slot, kind, format, options, content, children */ }

public interface IPlanContentSource
{
    // file / dir / blob:sha256 -> байты (кэш и бандл скрыты от плагина)
    byte[] Read(PlanContent Content, out string Error);
    IEnumerable<(string Path, byte[] Data)> ReadDirectory(PlanContent Content);
}

public class CompositeImagePlugin
{
    public virtual bool SupportLayout => false;
    public virtual bool CreatePlan(PlanDocument Doc, out string Error) => false; // создать слоты
    // Build(Blob, out Error) вызывается и для Root.Raw != null (шаблон),
    // и для Root.Raw == null (с нуля по слотам).
}
```

Хелпер `PlanPacker` покрывает простые форматы: принимает список слотов
(`offset/size/align/repeat/fill`) и байты, возвращает образ. Форматы со своей
структурой (RKFW, SWUpdate, CIMG) собирают тело сами, используя слоты как
подсказки.

Новые форматы не требуют правок ядра: плагин объявляет `SupportLayout`,
реализует `CreatePlan` и layout-режим `Build` — команды `mkimage`/`build`/
`layout` начинают работать автоматически. Формат только для чтения
(`SupportWrite == false`) при попытке сборки даёт явную ошибку.

## Правила и валидация

- Слоты не должны пересекаться; `offset + size*repeat` не выходит за родителя.
- `align` по умолчанию — из опций формата (например, eraseblock для NAND).
- `size: auto` — размер содержимого, округлённый по правилам формата.
- Переполнение слота — ошибка, а не молчаливое усечение.
- `layout validate` проверяет схему, слоты и наличие плагинов/писателей.
- После сборки рекомендуется round-trip: `load` результата через пайплайн и
  сверка обнаруженного дерева с ожидаемым.

## Диск (MBR/GPT)

`format: disk` собирает таблицу разделов и размещает payload'ы по слотам;
образ заполняется нулями (это диск, а не flash).

```yaml
schema: 1
format: disk
options: { table: gpt, align: 1048576, seed: myboard }   # table: mbr | gpt
children:
  - { name: boot, offset: 1048576, size: 16M, kind: raw, content: { file: inputs/boot.img } }
  - { name: root, offset: 18M, size: 32M, kind: raw, content: { file: inputs/rootfs.ext4 } }
```

- `table` — `gpt` по умолчанию; MBR ограничен 4 разделами.
- `total` — размер диска; по умолчанию последний раздел + `align`.
- `align` — выравнивание авто-слотов (по умолчанию 1 МиБ).
- `size: auto` — длина payload'а, округлённая до 512 байт.
- `seed` — детерминированные GPT GUID'ы (иначе меняются между сборками).
- Дочерние: `mbr-type` (по умолчанию `0x83`), `type-guid` (по умолчанию
  Linux data `0FC63DAF-...`), `partition-guid` (по умолчанию из `seed`+имя).
- GPT-имена разделов берутся из `name`; пишутся primary и backup копии
  таблицы и заголовков, CRC32 считается по entries/header.

## Вендорские контейнеры (шаблон)

Для RKFW/SWUpdate заголовки и служебные структуры берутся из образца:
`options.template` (путь относительно схемы или абсолютный). В раскладке
перечисляются только записи, которые нужно заменить/добавить.

```yaml
schema: 1
format: rkfw
options: { template: sample/update.img }
children:
  - { name: rootfs, kind: raw, content: { file: rootfs.ext4 } }
```

- **RKFW**: сохраняются RKFW-заголовок, loader и метаданные RKAF; payload'ы
  заменяются по имени, новые имена добавляются новыми разделами
  (`MiniLoaderAll.bin` заменяется как отдельная запись). Размеры/offset/хвост
  пересчитываются.
- **SWUpdate**: CPIO-архив сохраняется; записи заменяются по имени, новые
  добавляются; `sha256` в `sw-description` обновляются автоматически. Если
  нужен новый файл в `sw-description`, задайте его текст отдельной нодой
  `sw-description` — хеши будут пересчитаны. Подпись пересоздаётся только при
  наличии `swu.sign-key` (существующая иначе помечается устаревшей).
- Без `options.template` сборка невозможна: генерация вендорских заголовков с
  нуля не поддерживается (см. «Открытые вопросы»).

## Ограничения

- Raw NAND (OOB/ECC) не пишется: только линейные образы.
- Secure boot: подписи/шифрование — отдельный слой (KeyStore/FIT-подпись).
- Вендорские служебные структуры, не выводимые из раскладки, задаются
  `options` или отдельными билдерами.
- Некоторые существующие писатели ориентированы на файл; для сборки в памяти
  используются их варианты без имени файла (`BaseFsWriter(builder)` и т.п.).

## Реализовано (v1)

- слоты на нодах плана (`offset/size/align/repeat/fill/immutable/content`);
- YAML/JSON-схема (`PlanDocument`), `schema: 1`, неизвестные ключи сохраняются;
- API плагинов: `SupportLayout`, `CreatePlan`, `Build(Blob, Plugins, ...)`;
- `PlanPacker` (слоты, выравнивание, повторы, `0xFF`, проверка перекрытий);
- активная нода: `use` + `GetFs/GetKernel/GetDevTree`, `MarkDirty` по ветке;
- `mkimage`, `build`, `layout export`, `layout validate`;
- `content.file`, `content.dir` (каталог → ФС в памяти), `content.blob` (локальный кэш), `content.empty`;
- NAND: `CreatePlan` + layout-режим писателя (raw и filesystem-слоты);
- Диск: `CreatePlan` + layout-режим (`DiskImageBuilder`, MBR и GPT с
  protective MBR, primary/backup заголовками, CRC32 и GPT-именами);
- RKFW/SWUpdate: шаблонный режим (`options.template`), замена/добавление
  записей, пересчёт хешей и размеров;
- `layout export` пишет immutable-содержимое в `<schema>.blobs/<sha256>` и
  ссылается на него через `content.file`, поэтому экспорт → `mkimage` → `store`
  воспроизводит образ побайтово (проверено на 256 МиБ дампе Amlogic).
- Контейнеры **FIT** и **Android** собираются из дочерних нод (`PlanMaterializer`):
  ядро/ramdisk/DTB можно задавать файлами и каталогами, без промежуточных файлов;
  при экспорте они по-прежнему сохраняются как immutable-файл и пересобираются как есть.
- Мультитомный UBI в одной ноде (`format: ubi` + несколько томов) — следующий этап;
  отдельные UBI-разделы (`format: ubifs`) собираются уже сейчас.

## Этапы

1. Ядро: слоты на нодах, `PlanDocument` и YAML/JSON, `CreatePlan`,
   `PlanPacker`, `IPlanContentSource`, команды `layout`/`mkimage`/`build`,
   активная нода для `use`/`GetFs`/`MarkDirty`.
2. NAND: `SupportLayout`, `CreatePlan`, layout-режим писателя; проверка на
   реальном дампе (`layout export` → rebuild → сравнение карты и содержимого).
3. Диск (MBR/GPT) — сделано.
4. FIT/Android/CIMG/UBI (в основном обвязка `CreatePlan`).
5. Бандлы и кэш на `NyaPackage`, YAML как обязательный вход.
6. Vendor-форматы (RKFW, SWUpdate, ...).

## Открытые вопросы

- Хранить ли производные (ФС/UBI/FIT) в кэше по умолчанию или только по флагу.
- `!include`/мультидокументные шаблоны YAML.
- Политика `immutable` при изменении содержимого (ошибка или новый блоб).
- Единый `seed` на образ или на ноду.
