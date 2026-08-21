<p align="center">
  <img src="StormUnarchiver/Assets/app.png" alt="STORM UNARCHIVER Logo" width="160" height="160" style="border-radius: 28px; box-shadow: 0 12px 40px rgba(76, 201, 240, 0.35);" />
</p>

<h1 align="center">
  <span style="color:#4CC9F0; font-weight:900;">STORM</span> UNARCHIVER
  <br/>
  <sub style="font-size:16px; font-weight:normal; opacity:0.8;">v0.2.0 • High-Performance Real-Time Archive Automation for Windows</sub>
</h1>

<p align="center">
  <strong>Мощный, легковесный и автоматический инструмент распаковки архивов в реальном времени для Windows.</strong>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge&logo=dotnet" alt=".NET 8" /></a>
  <a href="https://learn.microsoft.com/en-us/windows/apps/winui/winui3/"><img src="https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-0078D4?style=for-the-badge&logo=windows11" alt="WinUI 3" /></a>
  <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-00A4EF?style=for-the-badge&logo=windows" alt="Windows 10/11" /></a>
  <img src="https://img.shields.io/badge/Formats-100%2B%20Supported-4ADE80?style=for-the-badge" alt="100+ Formats" />
  <img src="https://img.shields.io/badge/Version-v0.2.0-4CC9F0?style=for-the-badge" alt="v0.2.0" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-FBBF24?style=for-the-badge" alt="License MIT" /></a>
</p>

---

## 🌟 О проекте

**STORM UNARCHIVER** — это современное высокопроизводительное настольное приложение для Windows, разработанное на **C# / .NET 8** и **WinUI 3 (Windows App SDK)**. 

Программа отслеживает указанные папки в реальном времени и **автоматически распаковывает любые входящие архивы** в целевые директории без необходимости вручную запускать сторонние архиваторы.

---

## ✨ Ключевые возможности

### ⚡ Производительность и фоновый мониторинг
- 📂 **Мульти-парный мониторинг каталогов**: Добавляйте любое количество пар папок «*Откуда* ➔ *Куда*» с независимым контролем и перетаскиванием (Drag & Drop).
- ⚡ **Многопоточная параллельная обработка**: Настраиваемое количество параллельных потоков распаковки (до 4+ потоков) для мгновенной обработки очередей.
- 🛡️ **Защита от частичной записи (Smart File Lock Check)**: Ожидание полного завершения загрузки или копирования файла перед распаковкой.
- 🔄 **Автоповторы при сбоях (Retry Mechanism)**: Настраиваемое количество попыток распаковки с задержкой при временной блокировке файла.
- 🍃 **Эко-режим и I/O Throttling (CPU & SSD Protection)**: Фоновый режим с пониженным приоритетом потоков ввода-вывода, предотвращающий перегрев SSD и лаги в играх/тяжелых приложениях.

### 🔑 Словари паролей и шифрование
- 🔐 **Менеджер словарей паролей (Password Dictionary / Brute-force Fallback)**:
  - Хранение списка доверенных и популярных паролей.
  - Поддержка импорта внешних текстовых словарей (`.txt` wordlist) в один клик.
  - Автоматический перебор паролей при встрече защищенного архива с отображением подобранного ключа в журнале.
  - Переключатель отображения/скрытия основного пароля в интерфейсе.

### 📦 Форматы и вложенные архивы
- 📦 **Поддержка более 100 форматов архивов**: От классических `.zip`, `.rar` (включая RAR5), `.7z` до современных `.zst` (Zstandard), `.tar.xz`, образов дисков `.iso`, `.dmg`, `.vhd` и пакетов приложений `.apk`, `.deb`, `.rpm`.
- 🔁 **Интеллектуальная рекурсивная распаковка (Recursive Unpack)**: Автоматическая распаковка архивов внутри архивов (например, `.zip` внутри `.tar.gz`) с защитой от циклических вложений.
- 🎯 **Быстрые пресеты и фильтры**:
  - Быстрый выбор: *Все форматы (100+)*, *Основные (ZIP, RAR, 7Z, TAR)*, *Образы дисков (ISO, VHD, DMG)*.
  - Белые списки расширений и маски исключений (`*_part*`, `*.tmp`).

### 🔔 Системная интеграция и интерфейс
- 🔔 **Глубокая интеграция с Windows**:
  - Сворачивание в системный трей (System Tray) с контекстным меню и индикацией статуса (*Ожидание / Активен / Ошибка*).
  - Всплывающие уведомления Windows (Toast / Balloon notifications).
  - Автозапуск вместе со стартом операционной системы (реестр Windows `Run`).
- 📊 **Интерактивный журнал активности**:
  - Мгновенная фильтрация: *Все / Успешно / Ошибки / Инфо*.
  - Живой поиск по логу и экспорт истории в `.txt` или `.csv`.
- 🎨 **Современный интерфейс WinUI 3**:
  - Dark Cyber/Neon тема, поддержка эффекта Mica.
  - Быстрый переход в проводник кнопками «Открыть в проводнике».

---

## 🗂️ Поддерживаемые форматы (100+)

| Категория | Поддерживаемые форматы |
| :--- | :--- |
| **Популярные архивы** | `.zip`, `.rar` (v4/v5), `.7z`, `.tar`, `.gz`, `.bz2`, `.xz`, `.tgz`, `.tbz`, `.txz`, `.s7z`, `.lz`, `.lzma` |
| **Продвинутое сжатие** | `.zst`, `.zstd`, `.lz4`, `.lzo`, `.lzm`, `.lzx`, `.brotli`, `.pea`, `.sqx`, `.uha`, `.sea`, `.arc`, `.arj` |
| **Образы дисков & VM** | `.iso`, `.img`, `.vhd`, `.vhdx`, `.vmdk`, `.qcow2`, `.dmg`, `.wim`, `.bin`, `.cue`, `.mds`, `.nrg` |
| **Пакеты ПО & Установщики** | `.apk`, `.deb`, `.rpm`, `.cab`, `.msi`, `.appx`, `.msix`, `.jar`, `.war`, `.xpi`, `.pkg`, `.crx` |
| **Многотомные архивы** | `.001`, `.002`, `.z01`, `.z02`, `.r00`, `.r01`, `.part01.rar` |
| **Комиксы и книги** | `.cbz`, `.cbr`, `.cbt`, `.cb7`, `.cba` |

---

## 🛠️ Технологический стек

- **Ядро**: [.NET 8](https://dotnet.microsoft.com/) (`net8.0-windows10.0.22621.0`)
- **UI Framework**: [WinUI 3 / Windows App SDK 1.8](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- **Архивный движок**: [SharpCompress](https://github.com/adamhathcock/sharpcompress) (с расширениями Zstandard, Brotli, Tar, Zip, Rar)
- **Архитектура**: MVVM, [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Системные вызовы**: Windows Interop API (Shell_NotifyIconW, Win32 Windowing, Registry AutoStart)

---

## 🚀 Сборка и запуск

### Требования
1. Windows 10 (версия 1809+) или Windows 11
2. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
3. Visual Studio 2022 (с компонентом *Разработка приложений для Windows*) или VS Code с C# Dev Kit

### Сборка из исходников

### 📁 Структура проекта

- **`Sources\`** — Полные исходные коды приложения (`StormUnarchiver`) и установщика (`Installer`).
- **`Assembling\`** — Готовая скомпилированная программа, готовая к запуску (`StormUnarchiver.exe`).
- **`Files\`** — Автономные установщики программы (`STORM_UNARCHIVER_v{версия}_Setup.exe`) с сохранением всех версий.

### 🔨 Автоматическая сборка релиза и установщика

Запустите скрипт `Build_Release.bat` или выполните команду:
```powershell
.\Build_Release.ps1
```
Скрипт автоматически:
1. Скомпилирует и обновит готовую программу в каталоге `Assembling\`.
2. Соберет единый установщик `.exe` в каталоге `Files\` с ярлыками на Рабочем столе, меню «Пуск» и записями в реестре.
3. Сохранит все предыдущие версии инсталляторов в папке `Files\`.

### Ручная сборка и запуск

```bash
# Запустить приложение из исходников
dotnet run --project Sources/StormUnarchiver/StormUnarchiver.csproj

# Собрать релиз в папку Assembling
dotnet publish Sources/StormUnarchiver/StormUnarchiver.csproj -c Release -r win-x64 --self-contained false -o Assembling
```

---

## 📖 Руководство пользователя

1. **Добавление папки**: Нажмите кнопку **«Добавить пару»** и укажите папку-источник (например, `Downloads`) и целевую папку (например, `Extracted`). Также поддерживается быстрое перетаскивание (Drag & Drop).
2. **Настройка параметров**:
   - Включите **«Вложенные»**, если скачиваете архивы с вложенными под-архивами.
   - Включите **«Эко-режим»**, если хотите снизить нагрузку на процессор и диск.
   - Нажмите иконку **🔑** рядом с полем пароля, чтобы добавить список паролей или загрузить файл словаря.
3. **Запуск**: Нажмите **«Начать мониторинг»**. Приложение начнет отслеживать появление файлов и автоматически распаковывать их в фоне.

---

## 📄 Лицензия

Проект распространяется под лицензией **MIT License**. См. файл [`LICENSE`](LICENSE) для подробностей.
