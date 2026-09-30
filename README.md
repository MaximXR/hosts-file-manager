<div align="center">
  <img src="resources/icon.png" width="128" height="128" alt="Hosts Launcher Logo" />
  <h1>Hosts Launcher & Manager</h1>
  <p><b>Быстрый доступ к hosts из панели задач Windows, автосинхронизация подписок и умный планировщик</b></p>
</div>

[Русский](#русский) | [English](#english)

> ⚡ **Легковесная портативная утилита для Windows 10/11**: Мгновенный доступ к системному файлу hosts из панели задач, автоматические подписки (GeoHide, блокировщики рекламы и телеметрии) и тихий планировщик обновлений.
> 
> **Lightweight portable utility for Windows 10/11**: Instant taskbar access to the system hosts file, automated subscriptions (GeoHide, ad & telemetry blockers), and silent background scheduler.

---

## Русский

**Hosts Launcher & Manager** — портативный набор инструментов для Windows, позволяющий открывать системный файл hosts в один клик из панели задач в выбранном редакторе с правами администратора, управлять проверенными подписками правил обхода и блокировки рекламы и автоматически синхронизировать их в фоновом режиме.

👉 **[Скачать HostsLauncher-v1.2.1-portable.zip](https://github.com/MaximXR/hosts-launcher/releases/latest)** • Портативно • Без установки • Открытый исходный код

---

### Какие проблемы решает утилита?

1. **Блокировка сохранения в Блокноте (ошибка доступа UAC)**  
   Стандартный Блокнот Windows не умеет повышать права в процессе работы. Пользователь открывает файл `hosts`, вносит изменения, нажимает `Ctrl+S` и получает ошибку «Отказано в доступе» либо Блокнот сохраняет бесполезный файл `hosts.txt`. `OpenHostsFile.exe` запускает редактор сразу с правами администратора, поэтому файл сохраняется без сбоев.

2. **Поиск пути в системных каталогах**  
   Файл `hosts` спрятан в системной директории `C:\Windows\System32\drivers\etc\hosts`. Искать его через Проводник или вводить путь вручную каждый раз неудобно. Утилита создает ярлык с системной иконкой для Рабочего стола или закрепления на панели задач Windows в 1 клик.

3. **Забытый сброс DNS-кэша**  
   После ручной правки hosts операционная система и браузеры продолжают использовать старые закэшированные IP-адреса. Утилита автоматически сбрасывает кэш через прямой вызов Windows API (`DnsFlushResolverCache` в `dnsapi.dll` + `ipconfig /flushdns`).

4. **Потеря личных записей при обновлении подписок**  
   Большинство сторонних скриптов заменяют файл hosts целиком, стирая ручные локальные записи разработчиков (`localhost`, рабочие стенды, тестовые домены). Встроенный менеджер изолирует каждую подписку в строгий блок `# === BEGIN HOSTS-MANAGER MANAGED BLOCK ===` и сохраняет персональные строки нетронутыми. При отключении источника удаляется только его блок. Перед перезаписью создается резервная копия `hosts.bak`.

5. **Сетевые лаги во время онлайн-игр и работы (Режим простоя)**  
   Обычные планировщики обновляют списки в произвольный момент времени. Задача автообновления в Планировщике Windows создается с системным условием `RunOnlyIfIdle` — фоновая синхронизация выполняется исключительно во время простоя компьютера.

---

### Основные возможности

* 🌍 **Двуязычный интерфейс (i18n):** мгновенное переключение языка (`🇷🇺 Русский` / `🇬🇧 English`) прямо в шапке окна без перезапуска программы. Настройка сохраняется в `config.json`.
* 🚀 **Генератор ярлыков для панели задач:**
  * **Ярлык на открытие hosts:** выбор редактора (системный Блокнот с UAC, Notepad++, VS Code или любой указанный `.exe`) и системной иконки (`shell32.dll #0`, Блокнот или Notepad++).
  * **Ярлык «Обновить hosts в 1 клик» (`/update-now`):** тихая синхронизация активных подписок и сброс DNS прямо с Рабочего стола с быстрым системным уведомлением.
  * **Ярлык панели управления:** быстрый запуск настроек `HostsManager.exe`.
* 🌐 **Каталог проверенных подписок:**
  * **GeoHide:** выбор региона маршрутизации (`Россия (RU)`, `Европа (EU)` или `США (US)`) и прямой переход на официальный сайт сервиса.
  * **GitHub520:** ускорение доступа к ресурсам GitHub.
  * **StevenBlack Unified:** комплексный черный список рекламы, трекеров и вредоносных сайтов.
  * **WindowsSpyBlocker:** блокировка встроенной телеметрии и сбора данных Windows.
  * **AdAway:** мобильный и десктопный черный список рекламных серверов.
  * **Dan Pollock (`someonewhocares.org`):** регулярно обновляемый список блокировки спама и трекинга.
  * **Добавление собственных источников:** поддержка любых URL с сырыми правилами hosts (raw `.txt`) и ссылкой на сайт проекта.
* ⏱️ **Управление Планировщиком Windows:**
  * Выбор периодичности: каждый день (в 09:00 или 14:00), каждые 6 / 12 часов, при входе в систему или при простое ПК.
  * Опция **«💤 Обновлять только при простое компьютера»** (`RunOnlyIfIdle`).
  * Кнопка создания и удаления задачи в 1 клик с динамическим отображением статуса (`🟢 Задача активна` / `⚪ Задача не создана`).
  * Кнопка быстрого открытия оснастки `taskschd.msc` с автоматическим наведением на задачу.
* 📦 **100% Portable:** настройки хранятся в `config.json` рядом с `.exe`. Программа не оставляет записей в реестре Windows.

---

### Архитектура проекта

```text
HostsLauncher/
├── src/
│   ├── Models/AppConfig.cs        # Конфигурация, пресеты, JSON
│   ├── Localization/L10n.cs       # Словари RU/EN, переводчик
│   ├── Services/HostsService.cs   # Парсинг hosts, изоляция блоков, сброс DNS
│   ├── Services/SchedulerService.cs # Планировщик Windows, RunOnlyIfIdle
│   ├── UI/MainForm.cs             # Графический интерфейс Windows Forms
│   ├── HostsManagerProgram.cs     # Точка входа HostsManager.exe
│   └── Program.cs                 # Точка входа OpenHostsFile.exe (быстрый раннер)
├── tests/
│   └── TestSuite.cs               # Автоматический тестовый комплекс (10 тестов)
├── dist-win-unpacked/             # Готовые бинарники для работы
│   ├── OpenHostsFile.exe          # Легковесный раннер (запуск редактора с UAC)
│   ├── HostsManager.exe           # Графическая панель управления
│   └── config.json                # Портативная конфигурация (без реестра Windows)
├── dist/                          # Скомпилированные релизные zip-архивы
│   └── HostsLauncher-v1.2.0-portable.zip
├── test.bat                       # Скрипт сборки и запуска автотестов
├── build.bat                      # Сборка с автотестами и упаковка в dist/
├── version.json                   # Единый источник версии проекта (SemVer)
├── CHANGELOG.md                   # Двуязычная история изменений (RU / EN)
├── LICENSE                        # Лицензия MIT
├── .gitignore
└── README.md
```

---

### Установка и запуск

1. Скачайте архив **[HostsLauncher-v1.2.0-portable.zip](https://github.com/MaximXR/hosts-launcher/releases/latest)**.
2. Распакуйте содержимое архива в любую постоянную папку (например, `C:\Tools\HostsLauncher` или `%USERPROFILE%\Tools\HostsLauncher`).
3. Запустите `HostsManager.exe`, настройте удобный редактор и нажмите кнопку **«🚀 Создать ярлык для файла hosts»**.
4. Нажмите правой кнопкой мыши по созданному ярлыку на Рабочем столе ➔ **«Закрепить на панели задач»**.

> 💡 **Сборка из исходников:**  
> Для компиляции проекта не требуются сторонние SDK или тяжелая Visual Studio. Запустите `build.bat` в корне проекта — он использует штатный компилятор C# (`csc.exe`) из состава .NET Framework 4.x, присутствующий в Windows 10/11 по умолчанию.

---

### Рекомендуемые расширения-компаньоны

* **[Antigravity Chat Manager](https://github.com/MaximXR/Antigravity-Chat-Manager)** — визуальный менеджер истории диалогов, поиск по сессиям и очистка диска от мусора ИИ в Antigravity.
* **[Antigravity Plugin Manager](https://github.com/MaximXR/Antigravity-Plugin-Manager)** — панель управления плагинами, правилами, навыками и MCP-серверами для Antigravity.

---

## English

**Hosts Launcher & Manager** is a portable Windows utility suite designed to launch the system hosts file in your preferred text editor with Administrator privileges in a single click from the Windows taskbar, manage curated anti-censorship and ad-blocking subscriptions, and synchronize rules automatically in the background.

👉 **[Download HostsLauncher-v1.2.1-portable.zip](https://github.com/MaximXR/hosts-launcher/releases/latest)** • Portable • No installation required • Open Source

---

### Problems Solved by Hosts Launcher

1. **Notepad Save Denied Error (UAC Elevation)**  
   Default Windows Notepad cannot elevate privileges after opening. When editing `C:\Windows\System32\drivers\etc\hosts`, pressing `Ctrl+S` fails with an «Access Denied» error or forces Notepad to save a useless `hosts.txt` copy. `OpenHostsFile.exe` launches editors with elevated Administrator rights upfront, ensuring smooth saves.

2. **Hunting Deep File Paths in Windows**  
   The `hosts` file is buried deep inside `C:\Windows\System32\drivers\etc\hosts`. Manually opening File Explorer and typing paths is tedious. The launcher generates a desktop shortcut with a native Windows shell icon, ready to be pinned to the taskbar in 1 click.

3. **Stale DNS Resolution Cache**  
   After modifying hosts, the Windows resolver and web browsers frequently keep cached obsolete IP mappings. The manager automatically flushes DNS records using native Windows API (`DnsFlushResolverCache` in `dnsapi.dll` + `ipconfig /flushdns`).

4. **Loss of Custom Domain Mappings During Subscription Updates**  
   Most third-party tools replace the entire hosts file, erasing local development entries (`localhost`, custom vhosts, and private server aliases). The built-in sync engine encloses each subscription inside dedicated `# === BEGIN HOSTS-MANAGER MANAGED BLOCK ===` boundaries, preserving manual user entries untouched. Disabling a provider removes only its managed block. Safety backup `hosts.bak` is generated prior to every write.

5. **Network Latency During Active Gaming and Work (Idle Mode)**  
   Standard task schedulers run updates at arbitrary moments, causing network jitter. Hosts Manager configures Windows Task Scheduler with the native `RunOnlyIfIdle` condition — updates run strictly when the workstation is idle.

---

### Key Features

* 🌍 **Bilingual Interface (i18n):** instant on-the-fly language switching (`🇷🇺 Русский` / `🇬🇧 English`) directly from the header without restarting. Settings are saved to `config.json`.
* 🚀 **Taskbar Shortcut Generator:**
  * **Hosts Shortcut:** choose your editor (Notepad with UAC, Notepad++, VS Code, or custom `.exe`) and shell icon (`shell32.dll #0`, Notepad, or Notepad++).
  * **1-Click Sync Shortcut (`/update-now`):** silent background subscription refresh and DNS flush from your desktop with a native completion notification.
  * **Hosts Manager Shortcut:** fast access to the configuration GUI.
* 🌐 **Curated Subscription Catalog:**
  * **GeoHide:** choose your routing region (`Russia (RU)`, `Europe (EU)`, or `USA (US)`) with direct links to the official service website.
  * **GitHub520:** accelerated access to GitHub CDN and releases.
  * **StevenBlack Unified:** consolidated ad, tracker, and malware blocklist.
  * **WindowsSpyBlocker:** blocks Windows telemetry and data collection domains.
  * **AdAway:** ad-serving domains blocklist.
  * **Dan Pollock (`someonewhocares.org`):** active and maintained spam and tracking filter.
  * **Custom Sources:** add any raw `.txt` hosts endpoint with custom names and homepage URLs.
* ⏱️ **Task Scheduler Integration:**
  * Configurable schedules: daily (09:00 or 14:00), every 6 / 12 hours, at Windows logon, or when idle.
  * **«💤 Update only when computer is idle»** (`RunOnlyIfIdle`) toggle.
  * 1-click task creation and removal with real-time status reporting (`🟢 Task active` / `⚪ Task not created`).
  * Direct launch button for Windows `taskschd.msc` snap-in focused on the task.
* 📦 **100% Portable:** all settings reside in `config.json` next to the executable. No registry keys or background daemons left behind.

---

### Project Architecture

```text
HostsLauncher/
├── src/
│   ├── Models/AppConfig.cs        # Configuration, presets, JSON
│   ├── Localization/L10n.cs       # RU/EN dictionaries, translator
│   ├── Services/HostsService.cs   # Hosts parsing, block isolation, DNS flush
│   ├── Services/SchedulerService.cs # Windows Task Scheduler, RunOnlyIfIdle
│   ├── UI/MainForm.cs             # Windows Forms GUI
│   ├── HostsManagerProgram.cs     # HostsManager.exe entrypoint
│   └── Program.cs                 # OpenHostsFile.exe entrypoint (fast runner)
├── tests/
│   └── TestSuite.cs               # Automated test suite (10 tests)
├── dist-win-unpacked/             # Compiled binaries ready to run
│   ├── OpenHostsFile.exe          # Lightweight runner (launches editor with UAC)
│   ├── HostsManager.exe       # Graphical control panel
│   └── config.json                # Portable configuration (without registry)
├── dist/                          # Packaged release archives
│   └── HostsLauncher-v1.2.0-portable.zip
├── test.bat                       # Test compilation and execution script
├── build.bat                      # Build script with test gates and packaging
├── version.json                   # Semantic version SSOT
├── CHANGELOG.md                   # Bilingual changelog (RU / EN)
├── LICENSE                        # MIT License
├── .gitignore
└── README.md
```

---

### Installation and Usage

1. Download **[HostsLauncher-v1.2.0-portable.zip](https://github.com/MaximXR/hosts-launcher/releases/latest)**.
2. Extract the archive into a permanent folder (e.g. `C:\Tools\HostsLauncher` or `%USERPROFILE%\Tools\HostsLauncher`).
3. Run `HostsManager.exe`, select your preferred editor, and click **«🚀 Create hosts file shortcut»**.
4. Right-click the newly generated Desktop shortcut ➔ **«Pin to taskbar»**.

> 💡 **Building from Source:**  
> No external SDKs or Visual Studio installations are needed. Run `build.bat` in the repository root — it compiles using the built-in C# compiler (`csc.exe`) included with Windows .NET Framework 4.x.

---

### Recommended Companion Extensions

* **[Antigravity Chat Manager](https://github.com/MaximXR/Antigravity-Chat-Manager)** — visual AI conversation history manager, session search, and disk cleaner for Antigravity.
* **[Antigravity Plugin Manager](https://github.com/MaximXR/Antigravity-Plugin-Manager)** — visual control panel for plugins, rules, skills, and MCP servers in Antigravity.

---

### License

Distributed under the [MIT License](LICENSE).
