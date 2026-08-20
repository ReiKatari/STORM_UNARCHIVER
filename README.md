# ⚡ STORM UNARCHIVER

<p align="center">
  <img src="StormUnarchiver/Assets/app.png" alt="STORM UNARCHIVER Logo" width="160" height="160" style="border-radius: 24px; box-shadow: 0 8px 32px rgba(76, 201, 240, 0.3);" />
</p>

<p align="center">
  <strong>Мощный, легковесный и автоматический инструмент распаковки архивов в реальном времени для Windows.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-0078D4?style=for-the-badge&logo=windows11" alt="WinUI 3" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-00A4EF?style=for-the-badge&logo=windows" alt="Windows 10/11" />
  <img src="https://img.shields.io/badge/Formats-100%2B%20Supported-4ADE80?style=for-the-badge" alt="100+ Formats" />
  <img src="https://img.shields.io/badge/License-MIT-FBBF24?style=for-the-badge" alt="License MIT" />
</p>

---

## 🌟 О проекте

**STORM UNARCHIVER** — это современное высокопроизводительное настольное приложение для Windows, разработанное на **C# / .NET 8** и **WinUI 3 (Windows App SDK)**. 

Программа отслеживает указанные папки в реальном времени и **автоматически распаковывает любые входящие архивы** в заданные целевые директории без необходимости вручную открывать архиваторы.

---

## ✨ Ключевые возможности

- 📂 **Мульти-парный мониторинг каталогов**: Добавляйте любое количество пар папок «*Откуда* ➔ *Куда*» с независимым контролем.
- ⚡ **Многопоточная параллельная обработка**: Настраиваемое количество параллельных потоков распаковки (до 4+ потоков) для мгновенной обработки очередей.
- 📦 **Поддержка более 100 форматов архивов**: От классических `.zip`, `.rar`, `.7z` до современных `.zst`, `.tar.xz`, образов дисков `.iso`, `.dmg`, `.vhd` и пакетов приложений `.apk`, `.deb`, `.rpm`.
- 🔐 **Пароли и шифрование**: Автоматическая подстановка пароля для зашифрованных архивов с возможностью быстрого отображения/скрытия в UI.
- 🎯 **Быстрые пресеты и фильтры**:
  - Фильтр по белым спискам расширений и маскам исключений.
  - Быстрые пресеты: *Все форматы*, *Основные (ZIP, RAR, 7Z, TAR)*, *Образы дисков (ISO, VHD, DMG)*.
- 🛡️ **Защита от частичной записи (Smart File Lock Check)**: Система ожидает завершения загрузки или копирования файла перед началом распаковки.
- 🔄 **Автоповторы при сбоях (Retry Mechanism)**: Настраиваемое количество попыток распаковки с тайм-аутом.
- 🔔 **Нативная интеграция с Windows**:
  - Сворачивание в системный трей (System Tray) с контекстным меню и сменой иконки статуса (*Ожидание / Активен / Ошибка*).
  - Всплывающие уведомления Windows (Balloon / Toast).
  - Автозапуск при старте операционной системы (реестр Windows `Run`).
- 📊 **Интерактивный журнал активности**:
  - Мгновенная фильтрация: *Все / Успешно / Ошибки / Инфо*.
  - Живой поиск по логу.
  - Экспорт истории в `.txt` или `.csv`.
- 🎨 **Современный интерфейс WinUI 3**:
  - Dark Cyber/Neon тема, поддержка эффекта Mica.
  - Drag & Drop перетаскивание папок прямо в карточки.
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
3. Visual Studio 2022 (с рабочей нагрузкой *Разработка приложений для Windows*) или VS Code с C# Dev Kit

### Сборка из исходников

```bash
# Клонировать репозиторий
git clone https://github.com/ReiKatari/STORM_UNARCHIVER.git
cd STORM_UNARCHIVER

# Собрать проект
dotnet build StormUnarchiver/StormUnarchiver.csproj -c Release

# Запустить приложение
dotnet run --project StormUnarchiver/StormUnarchiver.csproj
```

### Публикация автономного (Self-Contained) бинарника

```bash
dotnet publish StormUnarchiver/StormUnarchiver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 📖 Руководство пользователя

1. **Добавление папки**: Нажмите кнопку **«Добавить пару»** и выберите папку-источник (например, `Downloads`) и целевую папку (например, `Extracted`). Также можно просто перетащить (Drag & Drop) папки в поля.
2. **Параметры**:
   - Включите **«Удалять архив»**, если хотите очищать источник после успешной распаковки.
   - Включите **«В трей»**, чтобы приложение работало незаметно в фоновом режиме.
   - Задайте **Пароль**, если скачиваете архивы с постоянным паролем.
3. **Запуск**: Нажмите **«Начать мониторинг»**. Приложение начнет отслеживать появление файлов и автоматически распаковывать их.

---

## 📄 Лицензия

Проект распространяется под лицензией **MIT License**. См. файл `LICENSE` для подробностей.
