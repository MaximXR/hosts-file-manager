# История изменений / Changelog

Все заметные изменения в проекте Hosts Launcher & Manager документируются в этом файле.
Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/) и придерживается [Семантического версионирования](https://semver.org/lang/ru/).

---

## [1.3.0] - 2026-09-30

### Русский
#### Добавлено
- **Раздельное хранение каталога пресетов (`presets.json`) и настроек (`config.json`)**: предустановленные подписки вынесены в отдельный файл `presets.json`. При обновлении программы файл `presets.json` обновляется разработчиком, а пользовательские добавленные источники и персональные настройки в `config.json` гарантированно не затираются.
- **Умный диалог управления пресетами (3 сценария сброса)**: нажатие на кнопку «Восстановить пресеты» теперь открывает модальное окно с гибким выбором действия:
  1. *Восстановить стандартные пресеты* — возвращает удаленные системные пресеты, бережно сохраняя все добавленные пользователем источники.
  2. *Удалить только добавленные источники* — очищает пользовательские ссылки без сброса официальных подписок.
  3. *Полный сброс к заводским настройкам* — полностью возвращает исходный набор источников.
- **Защита от автоматического воскрешения пресетов**: удаленные пользователем официальные пресеты фиксируются в `RemovedPresetIds` в `config.json` и не появляются самопроизвольно при перезапуске программы, пока пользователь явно не выберет их восстановление.
- **Адаптивный резиновый интерфейс (динамическое растяжение)**: окно программы теперь поддерживает свободное изменение размеров пользователем (размеры сохраняются в `config.json`). Блок подписок автоматически растягивается, заполняя всю доступную высоту, при этом элементы шапки и нижняя панель сохраняют идеальную фиксацию.
- **12-й автоматический тест**: расширен комплекс автотестов (`TestSuite.cs`), проверяющий изоляцию `presets.json`, round-trip сериализацию и все 3 сценария восстановления.

#### Исправлено
- **Наложение элементов в шапке (переключатель языка)**: устранено перекрытие выпадающего списка языков подписью при смене языка. Реализован строгий динамический метод компоновки шапки `LayoutHeader()`, учитывающий реальную ширину текста метки (`PreferredWidth`) и предотвращающий расширение `AutoSize` вправо поверх комбобокса.
- **Отображение символа амперсанда в названии**: для заголовка шапки `lblAppInfo` отключена обработка мнемоник (`UseMnemonic = false`), благодаря чему знак `&` в "Hosts Launcher & Manager" отображается корректно, а не скрывается как акселератор Windows.
- **Кодировка OEM в выводе Планировщика задач**: исправлена ошибка отображения статуса задачи на русскоязычных системах Windows (вместо поврежденных символов `(ŕ®в©ў®)` статус теперь корректно декодируется из OEM CP866 как `(Готово)`).
- **Обрезание ссылки GeoHide**: ссылка на сайт GeoHide теперь позиционируется с учетом правого края группы, предотвращая обрезание URL при стандартных габаритах окна.
- **Z-порядок шапки и вкладок**: исправлен порядок докинга контролов формы (`pnlHeader.SendToBack()`), гарантирующий чистое разделение верхней панели и вкладок без артефактов наложения.

### English
#### Added
- **Separated Preset Catalog (`presets.json`) & User Config (`config.json`)**: official subscriptions catalog is decoupled into a dedicated `presets.json` file. When updating the application, `presets.json` can be cleanly updated without touching custom user sources or preferences in `config.json`.
- **Smart Preset Management Dialog (3 Reset Scenarios)**: clicking "Restore presets" now opens a dedicated modal dialog offering granular control:
  1. *Restore standard presets* — restores missing official subscriptions while preserving all user-added custom sources.
  2. *Remove only custom sources* — clears added custom URLs without touching official subscriptions.
  3. *Full reset to factory defaults* — completely resets the provider list to default presets.
- **Preset Resurrecting Protection**: deleted official presets are tracked in `RemovedPresetIds` inside `config.json`, preventing them from reappearing on subsequent app launches unless explicitly restored.
- **Adaptive Resizable Interface (Dynamic Expansion)**: window dimensions can now be freely resized by the user (persisted in `config.json`). The custom providers container automatically expands to fill available height while preserving fixed header and footer margins.
- **12th Automated Test**: expanded test suite (`TestSuite.cs`) with comprehensive verification of `presets.json` isolation, round-trip serialization, and all 3 reset actions.

#### Fixed
- **Header Language Selector Overlap**: eliminated visual clipping and overlapping where the language label expanded over the dropdown upon language switch. Implemented explicit `LayoutHeader()` positioning taking label width (`PreferredWidth`) into account.
- **Ampersand Display in Title**: disabled mnemonic interpretation (`UseMnemonic = false`) on `lblAppInfo`, properly rendering the `&` symbol in "Hosts Launcher & Manager".
- **OEM Codepage Encoding for Task Scheduler**: resolved character corruption in task status on localized Windows systems (`(ŕ®в©ў®)` corrupted output now correctly decodes from OEM CP866 as clean text).
- **GeoHide Link Right-Clipping**: GeoHide project link is now positioned relative to the groupbox right edge, preventing URL text clipping.
- **Header & Tab Z-Order Docking**: corrected form docking order (`pnlHeader.SendToBack()`), ensuring clear boundary between the header and tabs.

---

## [1.2.1] - 2026-09-30

### Русский
#### Добавлено
- **Нативные системные уведомления Windows**: вместо модальных всплывающих окон при синхронизации hosts отправляется ненавязчивое push-уведомление в Центр уведомлений Windows (через системный `NotifyIcon.ShowBalloonTip`), не блокирующее интерфейс.
- **Кнопка прямого открытия hosts в шапке окна**: в шапку менеджера добавлена кнопка `[📄 Открыть hosts...]`, доступная на всех вкладках и запускающая системное окно Windows «Открыть с помощью...» для быстрого выбора любого редактора.
- **Логическое разделение на 3 отдельных блока ярлыков**: параметры разбиты на 3 четкие группы («1. Ярлык открытия hosts в редакторе», «2. Ярлык быстрого обновления hosts (в 1 клик)» и «3. Ярлык панели управления Hosts Manager»).
- **Единая иконка приложения и значок обновления**: нативные значки Win32 вшиты в сборку, наследуются ярлыками, отображаются на панели задач и в заголовках окон.
- **Модульная архитектура и набор автотестов**: исходный код структурирован по пространствам имен (`Models`, `Localization`, `Services`, `UI`). Внедрен автоматический тестовый комплект `test.bat` (10 тестов), валидирующий паритет локализации, работу с блоками hosts, параметры XML планировщика и заполненность UI-контролов.

#### Изменено
- **Уточнение текста регионов и однострочная верстка**: для России фраза о задержке заменена на точную «Россия (RU) — рекомендуется» (в EN: «Russia (RU) — recommended»). Все три радио-переключателя регионов размещены в одну компактную горизонтальную линию.
- **Отображение всех пресетов без скролла**: высота области каталога увеличена до 192px — теперь все 5 встроенных пресетов (GitHub520, StevenBlack, WindowsSpyBlocker, AdAway, Dan Pollock) полностью видны одновременно без полосы прокрутки.
- **Оптимальное позиционирование кнопки синхронизации**: кнопка «Синхронизировать hosts сейчас» опущена ниже, эффективно заполняя полезное пространство окна, и идеально обрамлена со статусной строкой.
- **Единые размеры кнопок и строгое выравнивание**: все три кнопки создания ярлыков приведены к абсолютно одинаковым размерам (605 × 34 px), поля ввода и выпадающие списки центрированы по единой направляющей (X = 225, W = 400).

### English
#### Added
- **Native Windows Toast Notifications**: hosts synchronization dispatches non-blocking native notifications directly to the Windows Notification / Action Center instead of modal popups.
- **Global Header "Open hosts..." Button**: embedded `[📄 Open hosts...]` button in the window header, available from all tabs to launch Windows "Open with..." dialog directly.
- **3 Logical Shortcut Blocks**: reorganized Tab 1 into 3 distinct sections ("1. Shortcut to open hosts in editor", "2. Fast hosts update shortcut (1-click)", and "3. Hosts Manager panel shortcut").
- **Unified Native Application & Sync Icons**: multi-resolution Win32 icons embedded in binary builds and applied to shortcuts and taskbar.
- **Modular Architecture & Automated Test Suite**: code divided into clean namespaces with 10 automated unit/UI tests executed before compilation.

#### Changed
- **Precise Region Wording & Single-Line Layout**: replaced misleading server latency remark with "Russia (RU) — recommended", placing all 3 region radio buttons on a single horizontal row.
- **All 5 Presets Fully Visible Without Scrolling**: expanded subscriptions list panel to 192px so all 5 built-in sources fit concurrently with zero scrollbar.
- **Lowered Sync Button Positioning**: "Sync hosts now" button lowered into the lower window section, perfectly framed above the bottom boundary.
- **Equal Shortcut Button Sizes & Pixel-Perfect Alignment**: all three shortcut creation buttons have identical dimensions (605 × 34 px) with aligned input fields (X = 225, W = 400).

---

## [1.2.0] - 2026-09-30

### Русский
#### Добавлено
- **Мультиязычность и локализация (i18n)**: полная поддержка русского и английского языков (`🇷🇺 Русский` / `🇬🇧 English`) с мгновенным переключением интерфейса, диалоговых окон и карточек на лету. Выбранный язык сохраняется в `config.json`.
- **Новый встроенный пресет Dan Pollock**: добавлен популярный и активный источник блокировки нежелательного контента (`someonewhocares.org`) с прямым переходом на сайт проекта.
- **Аудит актуальности источников подписок**: выполнена проверка активности всех встроенных репозиториев (GitHub520, StevenBlack, WindowsSpyBlocker, AdAway, GeoHide, Dan Pollock). Все проекты поддерживаются авторами; актуализированы ссылки на официальные домашние страницы.
- **Улучшенный мониторинг Планировщика**: статус отображает признак выполнения при простое компьютера (`[режим простоя ПК]`) и время следующего срабатывания.

#### Изменено
- Обновлен файл документации `README.md` в соответствии с инфостилем и высокими стандартами проектов Antigravity.

### English
#### Added
- **Full Localization & i18n**: bilingual support for Russian and English (`🇷🇺 Русский` / `🇬🇧 English`) with instantaneous on-the-fly UI switching across all controls, dialogs, and cards. Language preference is preserved in `config.json`.
- **New Built-in Dan Pollock Preset**: added the popular, well-maintained ad & spyware blocklist source (`someonewhocares.org`) with direct project website access.
- **Source Health & Activity Audit**: verified active maintenance for all bundled presets (GitHub520, StevenBlack, WindowsSpyBlocker, AdAway, GeoHide, Dan Pollock); updated official website endpoints.
- **Enhanced Task Scheduler Status**: live display of computer idle state constraint (`[computer idle mode]`) and upcoming execution schedule.

#### Changed
- Redesigned `README.md` following infostyle principles and reference repository architecture.

---

## [1.1.0] - 2026-09-30

### Русский
#### Добавлено
- **Ярлык обновления в 1 клик**: создание ярлыка на Рабочем столе для тихой синхронизации подписок и сброса DNS-кэша без открытия интерфейса (`/update-now`).
- **Ярлык менеджера**: создание прямого ярлыка на запуск панели управления `HostsManager.exe`.
- **Режим простоя (Idle Condition)**: фоновое обновление теперь запускается только при отсутствии активности пользователя (не мешает онлайн-играм, работе и просмотру видео).
- **Прямые ссылки на сайты проектов**: для подписок (GitHub520, Windows SpyBlocker, AdAway, StevenBlack) добавлены официальные страницы с подробным описанием вместо открытия сырых списков правил.
- **Карточный интерфейс подписок**: замена стандартного ListView на удобные карточки в едином стиле с GeoHide, устраняющие случайные переключения чекбоксов.
- **Архитектура версионирования**: внедрен `version.json`, автоматическая сборка релизных `.zip` архивов и ведение `CHANGELOG.md`.

#### Изменено
- Оптимизирована привязка иконок ярлыков через библиотеку `shell32.dll`.
- Расписание автообновления теперь настраивается прямо из графического интерфейса с поддержкой интервалов и события входа в систему.

---

### English
#### Added
- **1-Click Update Shortcut**: create a Desktop shortcut for silent background subscription updates and DNS flush (`/update-now`).
- **HostsManager Shortcut**: fast Desktop launcher for the configuration GUI.
- **Idle Condition Support**: scheduled updates run strictly when the system is idle, preventing network or CPU interference during gaming or active work.
- **Official Project Website Links**: direct navigation to project homepages (GitHub520, Windows SpyBlocker, AdAway, StevenBlack) instead of raw text rule lists.
- **Card-based Subscriptions UI**: replaced table view with clean cards matching the GeoHide layout to eliminate accidental checkbox clicks.
- **Versioning & Release Pipeline**: introduced `version.json`, automated portable `.zip` creation in `build.bat`, and bilingual `CHANGELOG.md`.

#### Changed
- Improved shortcut icon resolution from `shell32.dll`.
- Scheduler frequency selection integrated directly into the GUI.

---

## [1.0.0] - 2026-09-29

### Русский
- Первый публичный релиз `OpenHostsFile.exe` и `HostsManager.exe`.
- Мгновенный запуск системного файла `hosts` из панели задач Windows в выбранном редакторе (Блокнот, Notepad++, VS Code).
- Интеграция антиблокировочного сервиса GeoHide (регионы RU, EU, US).
- Изоляция правил в маркированных блоках с сохранением пользовательских ручных записей.
- Автоматическое резервное копирование `hosts.bak`.
- 100% Portable хранение без использования реестра Windows (`config.json`).

### English
- Initial public release of `OpenHostsFile.exe` and `HostsManager.exe`.
- Fast taskbar launcher for Windows `hosts` file in custom editors (Notepad with UAC, Notepad++, VS Code).
- GeoHide anti-censorship subscription provider integration (RU, EU, US regions).
- Isolated managed blocks preserving manual user hosts entries.
- Automatic safety backup to `hosts.bak`.
- 100% portable configuration without Windows registry pollution (`config.json`).
