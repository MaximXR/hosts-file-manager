# Черновик следующего релиза / Next Release Draft

> Этот файл используется для фиксации изменений во время активной разработки перед публикацией новой версии.
> При подготовке релиза эти записи переносятся в `CHANGELOG.md`.

### Русский
#### Исправлено
- **Наложение элементов в шапке (переключатель языка)**: устранено перекрытие выпадающего списка языков подписью при смене языка. Реализован строгий динамический метод компоновки шапки `LayoutHeader()`, учитывающий реальную ширину текста метки (`PreferredWidth`) и предотвращающий расширение `AutoSize` вправо поверх комбобокса.
- **Отображение символа амперсанда в названии**: для заголовка шапки `lblAppInfo` отключена обработка мнемоник (`UseMnemonic = false`), благодаря чему знак `&` в "Hosts Launcher & Manager" отображается корректно, а не скрывается как акселератор Windows.
- **Кодировка OEM в выводе Планировщика задач**: исправлена ошибка отображения статуса задачи на русскоязычных системах Windows (вместо поврежденных символов `(ŕ®в©ў®)` статус теперь корректно декодируется из OEM CP866 как `(Готово)`).
- **Обрезание ссылки GeoHide**: ссылка на сайт GeoHide теперь позиционируется с учетом правого края группы, предотвращая обрезание URL при стандартных габаритах окна.
- **Z-порядок шапки и вкладок**: исправлен порядок докинга контролов формы (`pnlHeader.SendToBack()`), гарантирующий чистое разделение верхней панели и вкладок без артефактов наложения.

### English
#### Fixed
- **Header Language Selector Overlap**: eliminated visual clipping and overlapping where the language label expanded over the dropdown upon language switch. Implemented explicit `LayoutHeader()` positioning taking label width (`PreferredWidth`) into account.
- **Ampersand Display in Title**: disabled mnemonic interpretation (`UseMnemonic = false`) on `lblAppInfo`, properly rendering the `&` symbol in "Hosts Launcher & Manager".
- **OEM Codepage Encoding for Task Scheduler**: resolved character corruption in task status on localized Windows systems (`(ŕ®в©ў®)` corrupted output now correctly decodes from OEM CP866 as clean text).
- **GeoHide Link Right-Clipping**: GeoHide project link is now positioned relative to the groupbox right edge, preventing URL text clipping.
- **Header & Tab Z-Order Docking**: corrected form docking order (`pnlHeader.SendToBack()`), ensuring clear boundary between the header and tabs.
