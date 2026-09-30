# История изменений / Changelog

Все заметные изменения в проекте Hosts Launcher & Manager документируются в этом файле.
Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/) и придерживается [Семантического версионирования](https://semver.org/lang/ru/).

---

## [1.2.1] - 2026-09-30

### Русский
#### Добавлено
- **Кнопка прямого открытия hosts в шапке окна**: в шапку менеджера добавлена кнопка `[📄 Открыть hosts...]`, доступная на всех вкладках и запускающая системное окно Windows «Открыть с помощью...» для быстрого выбора любого редактора.
- **Логическое разделение на 3 отдельных блока ярлыков**: параметры разбиты на 3 четкие группы («1. Ярлык открытия hosts в редакторе», «2. Ярлык быстрого обновления hosts (в 1 клик)» и «3. Ярлык панели управления Hosts Manager»).
- **Единая иконка приложения и значок обновления**: нативные значки Win32 вшиты в сборку, наследуются ярлыками, отображаются на панели задач и в заголовках окон.
- **Модульная архитектура и набор автотестов**: исходный код структурирован по пространствам имен (`Models`, `Localization`, `Services`, `UI`). Внедрен автоматический тестовый комплект `test.bat` (10 тестов), валидирующий паритет локализации, работу с блоками hosts, параметры XML планировщика и заполненность UI-контролов.

#### Изменено
- **Единые размеры кнопок и строгое выравнивание**: все три кнопки создания ярлыков приведены к абсолютно одинаковым размерам (605 × 34 px), поля ввода и выпадающие списки центрированы по единой направляющей (X = 225, W = 400).
- **Компактный редизайн списка подписок**: оптимизирована компоновка списка источников (высота строки 32px, зебра-фон, бейджи `[🌐 Сайт]` и `[📄 hosts]`, всплывающие подсказки). Все 5 встроенных пресетов помещаются на экране без вертикальной полосы прокрутки.
- **Оптимизация геометрии окна**: высота окна уменьшена с 805px до 725px без потери читаемости, сокращены лишние отступы в блоке GeoHide.

### English
#### Added
- **Global Header "Open hosts..." Button**: embedded `[📄 Open hosts...]` button in the window header, available from all tabs to launch Windows "Open with..." dialog directly.
- **3 Logical Shortcut Blocks**: reorganized Tab 1 into 3 distinct sections ("1. Shortcut to open hosts in editor", "2. Fast hosts update shortcut (1-click)", and "3. Hosts Manager panel shortcut").
- **Unified Native Application & Sync Icons**: multi-resolution Win32 icons embedded in binary builds and applied to shortcuts and taskbar.
- **Modular Architecture & Automated Test Suite**: code divided into clean namespaces with 10 automated unit/UI tests executed before compilation.

#### Changed
- **Equal Shortcut Button Sizes & Pixel-Perfect Alignment**: all three shortcut creation buttons have identical dimensions (605 × 34 px) with aligned input fields (X = 225, W = 400).
- **Compact Subscriptions UI Redesign**: streamlined custom providers list with sleek 32px rows, zebra styling, compact badges `[🌐 Сайт]` / `[📄 hosts]`, and tooltips. All 5 default presets fit without a vertical scrollbar.
- **Window Geometry Optimization**: reduced total form height from 805px to 725px, removing excess whitespace while retaining clean information hierarchy.

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
