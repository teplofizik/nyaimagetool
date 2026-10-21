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
- [ ] Образ скопирован в путь без пробелов/кириллицы (например `F:\TEMP\img`).
- [ ] Зафиксированы: ОС тестера, клиент (WinSCP/Linux mount/tftp), модель
      устройства, версия прошивки, SHA256 образа.
- [ ] Базовый прогон тестов (если есть dev-репозиторий):
      `dev 200/200`, `nyaimagetool 148/148`.

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

## 6. Общие проверки после правок

- [ ] `store` в исходном формате, `load` заново, `export`/сравнение.
- [ ] xattr/права/uid/gid не потеряны (если проверяемый образ их содержит).
- [ ] При падении/повторном запуске не осталось занятых портов.
- [ ] Логи не содержат секретов.
- [ ] Повторный полный прогон тестов зелёный (единичный golden-flake
      `bug-0002` — перепроверить повторным запуском, не менять golden).

## 7. Что считать ошибкой

Прикладывайте:

1. Команду/скрипт и клиент.
2. Ожидаемое и фактическое поведение.
3. Консольный вывод/лог (можно `*> file.log`).
4. SHA256 образа до/после.
5. Версию ОС/клиента и модель устройства.

## 8. Известные ограничения

- SFTP-сервер: принимает любые креды, нет нормальной аутентификации.
- NFS: NFSv3, без auth/locking, нет hardlink/MKNOD, portmapper 111 может
  требовать прав администратора.
- TFTP: без auth; `loaded:fs` собирает ФС в память на каждый запрос.
- Auth: ssh-agent не поддержан; Windows DPAPI для файла профилей не сделан.
- Remote: живой SSH/SCP/SFTP проверяется на устройстве; unit-тесты покрывают
  transfer-слой, auth-store и парсеры.
