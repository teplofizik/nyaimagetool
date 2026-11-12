# Ручное тестирование (независимый прогон)

Чек-лист для проверки живых сценариев: SFTP, NFS, TFTP, auth-профили и
remote-клиенты. Отмечайте пункты `[x]`; при ошибке приложите команду, лог,
версию клиента/ОС и хеш образа.

## 0. Артефакты и окружение

- [ ] Используется сборка `G:\Projects\Асик конфиг\nyaimagetool_test`
      (self-contained win-x64) или dev `NyaImageTool\bin\Debug\net10.0`.
- [ ] Запуск из папки приложения (важно: интерактивный shell ищет `plugins`
      относительно текущего каталога):
      ```powershell
      cd "G:\Projects\Асик конфиг\nyaimagetool_test"
      .\NyaImageTool.exe
      ```
- [ ] Плагины загружены: `service sftp info`, `service nfs info`,
      `service tftp info` отвечают, а не пишут `Invalid service name`.
- [ ] `version` выводит версии NyaImageTool/NyaFs/NyaLZO/NyaXZ и список плагинов
      (в скрипте и в интерактивном режиме).
- [ ] Образ скопирован в путь без пробелов/кириллицы (например `F:\TEMP\img`).
- [ ] Зафиксированы: ОС тестера, клиент (WinSCP/Linux mount/tftp), модель
      устройства, версия прошивки, SHA256 образа.
- [ ] Базовый прогон тестов (если есть dev-репозиторий):
      `dev 304/304`, `nyaimagetool 254/254`.

## 1. SFTP-сервер (NyaFsSftp)

- [ ] `service sftp set port 2222`, `service sftp start`, `service sftp info`.
- [ ] Подключение WinSCP/FileZilla/OpenSSH к `sftp://host:2222`.
- [ ] Удержание сессии без действий **не менее 60 секунд** — соединение не
      рвётся (проверка keepalive).
- [ ] Просмотр корня, вход в подкаталоги, скачивание файла.
- [ ] Создание файла, запись/дозапись, переименование, удаление.
- [ ] Создание/удаление каталога.
- [ ] Симлинк: создать, прочитать target, удалить.
- [ ] Проверить права/uid/gid после `chmod`/`chown` (если клиент умеет).
- [ ] После правок выполнить `store <out> ramfs <format>` и убедиться, что
      изменения попали в образ.
- [ ] Перезагрузить образ и проверить сохранённые файлы.

Ограничение: встроенный SFTP-сервер принимает любые учётные данные; используйте
его только в доверенной сети.

## 2. NFS-сервер (NyaFsNfs)

Сначала безопасный вариант на высоких портах:

- [ ] `service nfs set port 12049`
- [ ] `service nfs set mountport 12048`
- [ ] `service nfs set portmap 0`
- [ ] `service nfs start`, `service nfs info`
- [ ] Linux:
      ```bash
      sudo mkdir -p /mnt/nya
      sudo mount -t nfs -o nfsvers=3,proto=tcp,port=12049,mountport=12048,nolock \
        192.168.1.10:/ /mnt/nya
      ```
- [ ] `ls`, `cat`, копирование файла из `/mnt/nya`.
- [ ] Создание/запись/переименование/удаление файла.
- [ ] Создание/удаление каталога, симлинк.
- [ ] Видимость прав/uid/gid (`ls -ln`).
- [ ] После правок `store` и перезагрузка образа.
- [ ] `umount /mnt/nya`.

Дополнительно:

- [ ] Вариант с portmapper на 111 (нужны права/свободный порт 111):
      `service nfs set portmap 111`, `service nfs start`,
      `mount -t nfs -o nfsvers=3,proto=tcp,nolock host:/ /mnt/nya`.
- [ ] Windows NFS-клиент (Services for NFS), если доступен.
- [ ] Проверить, что при занятом порте/недоступном portmapper сервер
      сообщает об ошибке, а не падает молча.
- [ ] Проверить отказ для NFSv4/неизвестной версии (должен быть понятный
      protocol mismatch, не зависание).

Ограничения: нет аутентификации и file locking; используйте `nolock` и одного
писателя. `LINK`/`MKNOD` не поддержаны.

## 3. Auth-профили и remote-клиенты

- [ ] `remoteauth add dev <login> <password>`
- [ ] `remoteauth addkey devkey <login> <keyfile> [passphrase]`
- [ ] `remoteauth list`, `show`, `path`, `remove`.
- [ ] `remoteget ssh host @dev - /remote /local`
- [ ] `remoteput ssh host @dev - /local /remote`
- [ ] `remoteget scp/sftp` с тем же профилем.
- [ ] `remoteget ftp host @dev - ...` и legacy `ftpget host @dev - ...`.
- [ ] Legacy `copysshfrom host @dev - ...`.
- [ ] Профиль с приватным ключом (OpenSSH/PEM, с passphrase и без).
- [ ] Негативные сценарии:
  - [ ] несуществующий профиль `@missing`;
  - [ ] неверный ключ/passphrase;
  - [ ] недоступный хост;
  - [ ] протокол без плагина.
- [ ] Проверить, что пароли/парольные фразы **не** печатаются в логах и не
      попадают в `.nyas`-скрипты.
- [ ] Проверить `NYAFS_AUTH_FILE` и права файла (Unix `0600`).

## 4. TFTP-раздача образов

- [ ] `service tftp set port 12069`
- [ ] `service tftp set dir F:\TEMP\tftp`
- [ ] `tftpimage add zImage loaded:kernel`
- [ ] `tftpimage add board.dtb loaded:dtb`
- [ ] `tftpimage add rootfs.cpio loaded:fs:cpio`
- [ ] `tftpimage add local.bin F:\TEMP\local.bin`
- [ ] `tftpimage list`
- [ ] `service tftp start`
- [ ] Скачать с устройства/клиента `zImage` — совпадает с kernel.
- [ ] Скачать `board.dtb` — валидный DTB.
- [ ] Скачать `rootfs.cpio` — начинается с `070701` (CPIO newc).
- [ ] Скачать `local.bin` — совпадает побайтово.
- [ ] Изменить файл в ФС через SFTP/NFS, снова скачать `rootfs.cpio` —
      изменение присутствует.
- [ ] `tftpimage remove`, `tftpimage list`, повторная загрузка удалённого имени.
- [ ] Fallback: файл из `dir` без mapping.
- [ ] `tftpput` пишет только в `dir`.
- [ ] Попытка запроса с `../` не выходит за пределы `dir`.
- [ ] `service tftp set image name=loaded:fs` (альтернативный синтаксис).

Ограничение: TFTP без аутентификации; используйте в доверенной сети.

## 5. CramFS: little/big endian

- [ ] Загрузить обычный (little-endian) CramFS; `info`/`export` работают.
- [ ] Загрузить big-endian CramFS; определить, что формат CramFS, `export` читается.
- [ ] Для BE-образа первые 4 байта: `28 CD 3D 45`; для LE: `45 3D CD 28`.
- [ ] Изменить файл, `store ... ramfs cramfs`, перезагрузить, проверить содержимое.
- [ ] `set ramfs cramfs.endian big`, `store ... ramfs cramfs` — на выходе BE.
- [ ] `set ramfs cramfs.endian little` — на выходе LE.
- [ ] Проверить `cramfsck`/7-Zip на итоговом образе, если инструмент доступен.
- [ ] Проверить uid/gid (uid 16-bit, gid 8-bit) и длинные имена (<=252 байта).

## 6. ISO9660 (CD-образы)

- [ ] Загрузить ISO, созданный `genisoimage -R`: имена Rock Ridge, права,
      uid/gid, симлинки читаются.
- [ ] Загрузить ISO, созданный `genisoimage -J` (Joliet без RR): имена читаются.
- [ ] Загрузить ISO, созданный `genisoimage -R -J`: приоритет Rock Ridge.
- [ ] `store out.iso ramfs iso9660`, затем `load out.iso` и `export`.
- [ ] Проверить `7z l`, `isoinfo -R -l`, `mount -t iso9660 -o loop`.
- [ ] Проверить симлинк, uid/gid/mode и вложенные каталоги.
- [ ] Проверить длинные имена и ограничение RR (при необходимости).
- [ ] Убедиться, что boot/El Torito/UDF не затрагиваются.

## 7. exFAT (SD/USB)

- [ ] Загрузить образ SD-карты с exFAT; имена/каталоги/файлы читаются.
- [ ] `set ramfs size 64M`, `store out.img ramfs exfat`, затем `load out.img`.
- [ ] `fsck.exfat out.img` — clean.
- [ ] `mount -t exfat -o loop out.img /mnt/exfat` — файлы доступны.
- [ ] Проверить вложенные каталоги, длинные UTF-16 имена и пустые файлы.
- [ ] Проверить размер больше 64M (например 512M) и нехватку места.

## 8. EROFS

- [ ] Загрузить несжатый EROFS, созданный `mkfs.erofs`; имена/каталоги/файлы читаются.
- [ ] Проверить flat-inline файл больше одного блока.
- [ ] Проверить symlink и режимы/uid/gid.
- [ ] Проверить сжатые образы LZ4, LZMA, DEFLATE, Zstd: `big.txt` совпадает с исходником.
- [ ] Проверить full-index (`-E legacy-compress`) и compact-index образы.
- [ ] Проверить big pcluster (`-C 8192`), `ztailpacking`, chunk-based (`--chunksize`), whole-file (`-E all-fragments`) и tail (`-E fragments`) fragments.
- [ ] Сверить вывод с `dump.erofs -s/-S/-e` и распакованными файлами.
- [ ] `store out.erofs ramfs erofs`; `fsck.erofs out.erofs` завершается с кодом 0.
- [ ] Загрузить записанный образ, `export` и сравнить файлы с исходными (включая большой каталог).
- [ ] Для повторяющихся данных проверить сжатие: `dump.erofs` показывает Layout 1 и on-disk size меньше исходного.
- [ ] Для несжимаемых данных проверить fallback на plain-экстенты.
- [ ] Для big pcluster проверить Layout 1, `compr_cfgs big_pcluster` и физический экстент больше одного блока.
- [ ] Для ztailpacking проверить, что последний pcluster не занимает data-блок и `fsck.erofs` проходит.
- [ ] Для whole-file fragments проверить `packed nid`, feature `fragments` и чтение файлов из packed inode.
- [ ] Для xattrs проверить `fsck.erofs --xattrs`, inline/shared/long-prefix атрибуты, чтение root/файловых атрибутов через API.
- [ ] Проверить CLI-опции `set ramfs filesystem erofs`, `erofs.fragments`, `erofs.tail-fragments`, `erofs.shared-xattrs`, `erofs.compression-level`, `erofs.xattr-prefix`.

## 9. UDF

- [ ] `load` UDF-образа, созданного `mkudffs`; проверить `hello.txt`, вложенный каталог и symlink.
- [ ] Сравнить содержимое с исходным деревом.
- [ ] `fsck`/`mount` образа (Ubuntu) — без ошибок.
- [ ] `store out.udf ramfs udf`; `mount -t udf -o loop,ro out.udf` и сравнить файлы.

## 10. F2FS: образы больше 512 МБ

- [ ] `set ramfs size 600M`, `store ... ramfs f2fs` — образ создаётся.
- [ ] `fsck.f2fs -f --dry-run <image>` (Ubuntu/WSL) — `Done`, без ошибок.
- [ ] `fsck.f2fs -l <image>` — geometry/section/segment counts выглядят разумно.
- [ ] Загрузить образ обратно в NyaImageTool, `export`, сравнить файлы.
- [ ] Проверить 1G/2G образ (если есть место): `set ramfs size 1G`, `fsck.f2fs`.
- [ ] Убедиться, что старые образы <=512M по-прежнему собираются и проходят `fsck`.
- [ ] Проверить, что при нехватке места/некорректном размере ошибка понятная, без падения.

## 11. Общие проверки после правок

- [ ] `store` в исходном формате, `load` заново, `export`/сравнение.
- [ ] xattr/права/uid/gid не потеряны (если проверяемый образ их содержит).
- [ ] При падении/повторном запуске не осталось занятых портов.
- [ ] Логи не содержат секретов.
- [ ] Повторный полный прогон тестов зелёный (единичный golden-flake
      `bug-0002` — перепроверить повторным запуском, не менять golden).

## 12. Что считать ошибкой

Прикладывайте:

1. Команду/скрипт и клиент.
2. Ожидаемое и фактическое поведение.
3. Консольный вывод/лог (можно `*> file.log`).
4. SHA256 образа до/после.
5. Версию ОС/клиента и модель устройства.

## 13. Известные ограничения

- SFTP-сервер: принимает любые креды, нет нормальной аутентификации.
- NFS: NFSv3, без auth/locking, нет hardlink/MKNOD, portmapper 111 может
  требовать прав администратора.
- TFTP: без auth; `loaded:fs` собирает ФС в память на каждый запрос.
- Auth: ssh-agent не поддержан; Windows DPAPI для файла профилей не сделан.
- Remote: живой SSH/SCP/SFTP проверяется на устройстве; unit-тесты покрывают
  transfer-слой, auth-store и парсеры.
