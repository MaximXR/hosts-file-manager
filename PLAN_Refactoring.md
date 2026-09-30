# План рефакторинга и создания автотестов (Hosts Launcher & Manager)

## Цель
1. Устранить критический баг пустых текстов интерфейса (отсутствие вызова `ApplyLocalization()` при инициализации формы).
2. Разбить монолитный 1800+ строчный `src/HostsManager.cs` на чистые независимые модули с разделением ответственности (Models, Services, Localization, UI).
3. Создать полноценный набор автоматических тестов (`tests/TestSuite.cs`, `test.bat`), проверяющий:
   - Полноту и целостность словарей локализации (RU/EN).
   - **Обязательное наличие непустого текста на ВСЕХ контролах формы при старте и переключении языков (прямая защита от бага на скриншоте)**.
   - Изоляцию и сохранение пользовательских записей в hosts при вставке/обновлении/удалении блоков.
   - Корректность работы с XML планировщика (внедрение режима `RunOnlyIfIdle`).
   - Сериализацию конфигурации.
4. Встроить автотесты в пайплайн сборки (`build.bat`): если хоть один тест падает, сборка и упаковка релиза немедленно блокируются.

---

## Архитектура модулей

### 1. `src/Models/AppConfig.cs`
- `AppConfig`: основные настройки (редактор, права, регион GeoHide, язык, расписание).
- `CustomProviderConfig`: модель подписки (имя, URL, SiteUrl, LastHash, LastUpdated, Enabled).
- JSON сериализация/десериализация.

### 2. `src/Localization/L10n.cs`
- Статический класс `L10n`.
- Словари `Ru` и `En` со 100% паритетом ключей.
- Метод `T(string key, params object[] args)`.
- Текущий язык `CurrentLang` ("ru" / "en").

### 3. `src/Services/HostsService.cs`
- `UpdateHosts(AppConfig config, bool isSilent)`: алгоритм безопасной синхронизации.
- `ReplaceBlock(...)`, `RemoveBlock(...)`: строгая изоляция блоков `# === BEGIN HOSTS-MANAGER MANAGED BLOCK [...] ===`.
- `ComputeHash(...)`: вычисление SHA256 хеша содержимого.
- `FlushDnsSafe()`: сброс DNS через `DnsFlushResolverCache` в `dnsapi.dll` + `ipconfig /flushdns`.

### 4. `src/Services/SchedulerService.cs`
- `CheckTaskStatus(...)`: опрос `schtasks.exe /query /fo csv /v` и парсинг состояния, следующего запуска и режима простоя.
- `CreateTask(...)`: создание задачи автообновления с выбранным расписанием и внедрением флага `RunOnlyIfIdle` через XML.
- `DeleteTask(...)`: удаление задачи.
- `OpenTaskScheduler(...)`: открытие `taskschd.msc` и фокусировка на задаче.

### 5. `src/UI/MainForm.cs`
- Класс формы `MainForm`.
- Метод `InitUI()`: инициализация контролов.
- Метод `ApplyLocalization()`: **вызывается при создании формы** и при каждом переключении `cboLanguage`.
- Обработчики событий (создание ярлыков, добавление источников, ручная синхронизация, переключение задачи планировщика).

### 6. `src/HostsManagerProgram.cs`
- Точка входа `Program.Main(string[] args)`.
- Обработка аргументов `/update-now`, `/update-silent`, запуск `MainForm`.

### 7. `tests/TestSuite.cs` + `test.bat`
- Автотесты без внешних тяжелых зависимостей (компилируются штатным `csc.exe`).
- Тесты:
  1. `Test_L10n_AllKeysNonEmptyAndSymmetric`
  2. `Test_UI_AllControlsHaveText_OnStartup`
  3. `Test_UI_DynamicLanguageSwitching`
  4. `Test_Hosts_ManagedBlockIsolation`
  5. `Test_Hosts_ManagedBlockRemoval`
  6. `Test_Hosts_HashComparison`
  7. `Test_Scheduler_XmlIdleInjection`
  8. `Test_Config_Serialization`

---

## План внедрения
1. Создать модули в `src/Models/`, `src/Localization/`, `src/Services/`, `src/UI/`.
2. Создать `src/HostsManagerProgram.cs`.
3. Создать `tests/TestSuite.cs` и `test.bat`.
4. Обновить `build.bat` для вызова `test.bat` перед сборкой.
5. Запустить тесты, убедиться в 100% успехе (зеленый статус).
6. Проверить `dist-win-unpacked/HostsManager.exe` и запустить форму.
