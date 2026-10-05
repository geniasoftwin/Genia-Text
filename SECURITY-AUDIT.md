# Security audit — GeniaText 0.7.0-beta.1

Статический аудит восстановленной/улучшенной кодовой базы. Он дополняет, но не заменяет динамическое тестирование на Windows.


## 0.7.0-beta.1 delta

Beta.1 is a promotion-only freeze of the tested alpha.14 codebase. No production security boundary, recovery path, storage format, hotkey, paste, or network behavior changed. The frozen Core hashes remain unchanged.
## 0.7.0-alpha.14 delta
- Backup timestamp presentation changed from `dd.MM.yyyy HH:mm` to `dd.MM.yyyy HH:mm:ss`.
- No new file access, write path, network access, clipboard operation, hotkey behavior, or restore behavior was added.
- Frozen Core files remain byte-identical to 0.6.5 Final RC1.

## 0.7.0-alpha.13 delta
- Manual backup deletion reuses the existing Backups path-confinement boundary and additionally accepts only `phrases-*.json` files.
- Deletion is explicit, confirmed, and does not mutate the live `phrases.json`.

Сохраняется исправление alpha.12: метаданные `CreatedAtLabel`, `Reason` и вычисляемое read-only свойство `SizeLabel` используются только для чтения, поэтому WPF не пытается записывать значения обратно в объект резервной копии.

Перечисление каталога и разбор JSON выполняются вне WPF UI-потока, первая копия не выбирается автоматически, устаревший предпросмотр отменяется. JSON читается прямо из `FileStream`; путь ограничен каталогом `Backups`, лимиты размера/числа/полей сохранены. Для ручного удаления добавлен ровно один новый write-side путь: после явного подтверждения может быть удалён только выбранный файл `phrases-*.json` внутри `Backups`. Живой `phrases.json`, настройки и корзина этим методом недоступны.

Кнопка `Редактор` в Picker остаётся переименованной в `Фразы`, а окно — в `Управление фразами`. Сеть, clipboard, hotkey, foreground API, схема фраз и замороженный Core не изменены.

## 0.7.0-alpha.8 delta

Корзина и просмотр копий получили независимые точки входа. Для определения состояния кнопки `Корзина` bounded/fail-closed `RecycleBinService.Load()` вызывается только при открытии редактора и только при включённом модуле; запуск программы и основное окно выбора фраз этого файла не читают. Ошибка чтения не трактуется как пустая корзина: кнопка остаётся доступной для просмотра предупреждения, а запись по-прежнему заблокирована. Новых сетевых, clipboard, hotkey или foreground-операций нет.

Компактные однострочные элементы Editor Pro и запрет вертикальной прокрутки в однострочных TextBox являются только XAML-изменениями представления. Многострочные поля явно сохраняют `Auto`-прокрутку; логика хранения, вставки и Core не затронута.

## 0.7.0-alpha.7 delta

Изменение ограничено XAML-представлением read-only полей `Название`/`Категория` и пользовательской терминологией. Внутренняя прокрутка однострочных полей удалена; обработчики, данные, восстановление, файлы, сеть, hotkey, clipboard и вставка не изменены. Технические имена `PickerWindow` сохранены, поэтому Core не затронут.

## 0.7.0-alpha.6 delta

Safety Pack добавляет только локальные, выключенные по умолчанию функции восстановления. `trash.json` ограничен 64 МБ/10 000 записей и записывается атомарно; при ошибке чтения исходник не карантинится и дальнейшая запись блокируется. Архивирование завершается до удаления живой фразы. В alpha.6 просмотр резервных копий принимал только канонический путь внутри `Backups`, ограничивал размер/число/длины полей и сам не писал данные; alpha.13 отдельно добавляет подтверждённое удаление выбранной копии. Полное восстановление и подтверждённый импорт проходят через существующий `PhraseStore.Import`, поэтому сохраняются его лимиты, GUID-нормализация и backup-before-import.

Предпросмотр импорта только читает и классифицирует данные. Он не считается новым доверенным путём записи: окончательная проверка остаётся в `PhraseStore`. Восстановление одной фразы клонирует объект и заменяет конфликтующий GUID. Новых сетевых API, P/Invoke, clipboard/foreground операций, фоновых задач и телеметрии нет. При выключенных флагах сервисы не создаются и файлы не открываются.

Косметические изменения уменьшают padding полей и выбирают явный 32 px источник иконки; они не меняют обработчики ввода.

## 0.7.0-alpha.5 delta

Изменения ограничены XAML: общий `WindowChrome`, системные оконные команды, компактные размеры и исправленная передача `DisplayMemberPath` в `ContentPresenter`. Кнопки заголовка используют встроенные `SystemCommands`; новых P/Invoke, обработчиков, файловых или сетевых операций нет. C#-код, hotkey, clipboard, вставка, хранение и модули не изменены.

## 0.7.0-alpha.4 delta

Изменения ограничены общим XAML-словарём Windows 7-inspired темы и его применением к существующим элементам. Новых обработчиков, привязок к данным, файловых или сетевых операций нет. C#-код, hotkey, clipboard, вставка, хранение и модули не изменены.

## 0.7.0-alpha.3 delta

Изменения ограничены XAML-оформлением: нулевые радиусы углов, цельные строки списков с разделителями и уменьшенная тень Picker. Code-behind, обработчики, данные, hotkey, clipboard и вставка не изменены.

## 0.7.0-alpha.2 delta

Изменение ограничено локальными XAML-стилями полей Picker: фиксированная высота и вертикальное выравнивание содержимого. Код обработки поиска, фильтрации, hotkey, clipboard, вставки и хранения данных не изменён.

## 0.7.0-alpha.1 delta

Editor Pro работает только с уже загруженной локальной коллекцией фраз. Модуль не добавляет сеть, внешние DLL, чтение процессов, clipboard или автоматическое удаление. Поиск дублей хранит нормализованный текст только в памяти на время операции. Пакетная категория ограничена тем же пределом 256 символов, который применяется при импорте.

Стандартные файлы Core, отвечающие за hotkey, Picker, paste и безопасное хранение, совпадают с проверенной базой 0.6.5 Final RC1; контрольные SHA-256 перечислены в `CORE-CONTRACT.md`.

## Краткий итог

Критических/RCE-проблем в текущей архитектуре не обнаружено. GeniaText работает локально, сетевого функционала нет, импорт использует `System.Text.Json`, пользовательские фразы не отправляются наружу. Главные риски находятся вокруг clipboard/фокуса Windows и целостности локальных JSON-файлов.

### MEDIUM -> mitigated: race при восстановлении clipboard

Временный текст GeniaText помечается уникальным внутренним marker, а операция дополнительно сверяет `GetClipboardSequenceNumber`. Старое содержимое восстанавливается только если clipboard всё ещё принадлежит текущей операции. Если пользователь/другая программа успели скопировать новое значение, GeniaText его не перезаписывает.

### MEDIUM -> mitigated: Ctrl+V может уйти не в исходное приложение

Точное сравнение HWND оказалось слишком строгим для некоторых приложений и в ранней восстановленной версии мешало нормальной вставке. В 0.6.5 используется компромиссный guard: непосредственно перед `SendInput` foreground-window должен принадлежать **тому же процессу**, что и окно, из которого был вызван GeniaText. Если пользователь реально переключился в другое приложение и вернуть target не удалось, автоматическая вставка отменяется, а выбранный текст остаётся в clipboard для ручного Ctrl+V.

Ограничение Windows остаётся: UIPI может блокировать ввод из обычного процесса в elevated-приложение. Это должно обрабатываться как безопасный отказ, а не обходиться повышением прав GeniaText.

### MEDIUM/LOW -> fixed: I/O ошибка не считается повреждённым JSON

`phrases.json` отправляется в quarantine только при `JsonException`. `IOException` и `UnauthorizedAccessException` не переименовывают исходный пользовательский файл и блокируют опасную перезапись до следующего успешного запуска.

### LOW -> fixed: resource exhaustion при импорте

Действуют лимиты: файл до 20 МБ, до 10 000 фраз, title до 512, category до 256, text одной фразы до 1 000 000 символов. Duplicate/empty GUID нормализуются.

### LOW -> mitigated: частично отправленный SendInput

Win32 `SendInput` теоретически может принять только часть массива событий. Если это случается, 0.6.5 делает best-effort отправку `V up` и `Ctrl up`, чтобы уменьшить риск логически «залипшего» Ctrl.

### LOW -> fixed/mitigated: конкурентные paste-транзакции

PasteService сериализует операции через `SemaphoreSlim`, а Picker блокирует повторный запуск вставки до окончания текущей операции. Быстрые повторные Enter/Space/двойные клики не должны запускать параллельные clipboard-транзакции.

### LOW: portable mode и права каталога

Portable mode заранее проверяет возможность записи, копирует данные атомарно и меняет marker только после успешного переноса. Portable-start не должен создавать `%APPDATA%\GeniaText` автоматически.

### INFO: подключаемые функции локальны и выключены по умолчанию

- Smart Templates преобразуют только локальный текст выбранной фразы.
- Advanced Search работает локально по текущей коллекции.
- Usage History хранит только `UseCount`/`LastUsedAt` в `phrases.json`.
- App Profiles читают только имя процесса исходного окна и сопоставляют его локальным `process=category` правилам.
- Ни один модуль не добавляет сеть/телеметрию.
- Корзина, просмотр копий и import preview читают только локальные явно выбранные/настроенные данные и не участвуют в запуске, Picker или вставке, пока выключены.

### INFO: PDB не должен входить в публичный релиз

PDB облегчает reverse engineering и раскрывает структуру исходников. Release/portable scripts публикуют без PDB; private symbols можно хранить отдельно.

### INFO: Authenticode всё ещё рекомендуется перед публичным распространением

Неподписанный EXE не является уязвимостью сам по себе, но Windows не может криптографически подтвердить издателя. После стабилизации 0.6.5 стоит подписывать публичные релизы.

## Обязательная динамическая проверка

См. `TEST-PLAN.md`: clipboard race, быстрый Alt+Tab, elevated targets, locked clipboard, malformed/oversized JSON, read-only folders, hotkey conflicts, long-running tray/sleep-resume and per-module regression tests.

## 0.6.5 final hardening additions

- `settings.json` now follows the same fail-closed write policy as `phrases.json`: if an existing settings file cannot be safely read because of I/O/ACL problems, GeniaText uses in-memory defaults for the session but blocks settings writes instead of overwriting the source.
- Startup loading rejects unexpectedly large local settings/phrase database files before deserializing them.
- App-profile rule text is bounded to avoid pathological parsing/search input.
- Portable and framework-dependent ZIP build scripts generate SHA-256 checksum sidecars. This helps integrity verification, but does not replace Authenticode signing or a trusted distribution channel.
- Unhandled UI/process/background task failures are logged without phrase text or clipboard content so field crashes are diagnosable.
