# Черновик следующего релиза / Next Release Draft

> Этот файл используется для фиксации изменений во время активной разработки перед публикацией новой версии.
> При подготовке релиза эти записи переносятся в `CHANGELOG.md`.

### Русский
#### Добавлено
- **Раздельное хранение каталога пресетов (`presets.json`) и настроек (`config.json`)**: предустановленные подписки вынесены в отдельный файл `presets.json`. При обновлении программы файл `presets.json` может обновляться разработчиком, а пользовательские добавленные источники и персональные настройки в `config.json` гарантированно не затираются.
- **Умный диалог управления пресетами (3 сценария сброса)**: нажатие на кнопку «Восстановить пресеты» теперь открывает модальное окно с гибким выбором действия:
  1. *Восстановить стандартные пресеты* — возвращает удаленные системные пресеты, бережно сохраняя все добавленные пользователем источники.
  2. *Удалить только добавленные источники* — очищает пользовательские ссылки без сброса официальных подписок.
  3. *Полный сброс к заводским настройкам* — полностью возвращает исходный набор источников.
- **Защита от автоматического воскрешения пресетов**: удаленные пользователем официальные пресеты фиксируются в `RemovedPresetIds` в `config.json` и не появляются самопроизвольно при перезапуске программы, пока пользователь явно не выберет их восстановление.
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
- **12th Automated Test**: expanded test suite (`TestSuite.cs`) with comprehensive verification of `presets.json` isolation, round-trip serialization, and all 3 reset actions.

#### Fixed
- **Header Language Selector Overlap**: eliminated visual clipping and overlapping where the language label expanded over the dropdown upon language switch. Implemented explicit `LayoutHeader()` positioning taking label width (`PreferredWidth`) into account.
- **Ampersand Display in Title**: disabled mnemonic interpretation (`UseMnemonic = false`) on `lblAppInfo`, properly rendering the `&` symbol in "Hosts Launcher & Manager".
- **OEM Codepage Encoding for Task Scheduler**: resolved character corruption in task status on localized Windows systems (`(ŕ®в©ў®)` corrupted output now correctly decodes from OEM CP866 as clean text).
- **GeoHide Link Right-Clipping**: GeoHide project link is now positioned relative to the groupbox right edge, preventing URL text clipping.
- **Header & Tab Z-Order Docking**: corrected form docking order (`pnlHeader.SendToBack()`), ensuring clear boundary between the header and tabs.
