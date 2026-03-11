# TeamSpeak Repair Tool

Portable Windows utility for fixing broken **TeamSpeak 3 link handling** after installing or removing newer TeamSpeak versions (5/6).

Портативная утилита Windows для исправления работы **ссылок TeamSpeak 3 (`ts3server://`)** после установки или удаления новых версий TeamSpeak (5/6).

---

# 🇬🇧 English

## What this tool does

The application contains **two actions**:

### 1. Remove TeamSpeak

Performs a complete cleanup:

- searches for installed TeamSpeak products
- runs the **standard uninstallers**
- uses timeout protection for hanging uninstallers
- stops leftover TeamSpeak and MSI processes
- exports related registry keys (`.reg`)
- backs up user data folders
- removes broken protocol and file associations
- removes leftover TeamSpeak folders
- saves a detailed log
- creates a ZIP archive with results

### 2. Protocol Fix

Use this **only if needed** after reinstalling TeamSpeak 3.

This action:

- removes conflicting `ts3server://` user associations
- re-registers `ts3server://` for TeamSpeak 3
- restarts Windows Explorer

Usually after **cleanup + reinstall TS3**, this step is **not required**.

---

## Why this tool exists

New TeamSpeak versions may override the `ts3server://` protocol handler.

After uninstalling them Windows may keep broken registry bindings, causing:

- browser links not opening TeamSpeak 3
- `ts3server://` doing nothing
- browsers asking to open the application but nothing happens

This tool automates the cleanup and recovery process.

---

## Recommended workflow

1. Run **1. Remove TeamSpeak**
2. Reboot Windows
3. Install **TeamSpeak 3**
4. Launch TS3 once
5. Test:

Win + R
ts3server://voice.teamspeak.com


6. If links still do not work → run **2. Protocol Fix**

---

## Features

- portable
- WinForms GUI
- system theme UI
- administrator launch
- detailed log window
- registry backup
- user data backup
- ZIP archive of results
- standard uninstallers used first
- protocol repair for TeamSpeak 3

---

## Build

Requirements:

- Windows 10 / 11
- .NET 8 SDK
- x64

Build portable EXE:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

Output:
```
bin/Release/net8.0-windows/win-x64/publish/TSRepairTool.exe
```

Project structure
```
TSRepairTool
│
├─ TSRepairTool.csproj
├─ Program.cs
├─ MainForm.cs
└─ app.manifest
```

Logs and backups

The tool creates folders on the Desktop:

TS-Tool-cleanup-YYYYMMDD-HHMMSS
TS-Tool-fix-YYYYMMDD-HHMMSS

Each session may contain:

App.log
Backup/Registry/*.reg
Backup/UserData/*
ZIP archive

# 🇷🇺 Русский

## Что делает программа

Программа содержит 2 действия:

### 1. Удаление TS

Выполняет полную очистку:

ищет установленные версии TeamSpeak

запускает штатные деинсталляторы

защищает от зависших uninstall процессов

останавливает процессы TeamSpeak и MSI

экспортирует ключи реестра (.reg)

делает backup пользовательских данных

удаляет сломанные привязки протоколов

удаляет остаточные папки TeamSpeak

сохраняет лог

создаёт ZIP архив с результатами

### 2. Фикс протокола

Используется только если нужно после установки TeamSpeak 3.

Функция:

удаляет конфликтующие user-привязки ts3server://

регистрирует ts3server:// заново

перезапускает Explorer

В большинстве случаев после очистки + установки TS3 фикс не нужен.

Зачем нужна эта утилита

Новые версии TeamSpeak могут перезаписывать обработчик протокола:

ts3server://

После их удаления Windows может оставить неправильные привязки, из-за чего:

ссылки в браузере не открывают TS3

ts3server:// не работает

браузер предлагает открыть программу, но ничего не происходит

Эта утилита автоматически исправляет такие ситуации.

Рекомендуемый порядок использования

Запустить Удаление TS

Перезагрузить Windows

Установить TeamSpeak 3

Один раз запустить TS3

Проверить:
```
Win + R
ts3server://voice.teamspeak.com
```
Если ссылки всё ещё не работают → запустить Фикс протокола

Особенности

portable приложение

GUI на WinForms

системная тема Windows

запуск с правами администратора

окно логов

backup реестра

backup пользовательских данных

архивирование результатов

сначала используется штатный uninstall

затем удаляются остатки

Логи и backup

Результаты сохраняются на рабочий стол:
```
TS-Tool-cleanup-YYYYMMDD-HHMMSS
TS-Tool-fix-YYYYMMDD-HHMMSS
```
Содержимое:
```
App.log
Backup/Registry/*.reg
Backup/UserData/*
ZIP архив
```
Безопасность

Перед удалением программа пытается:

экспортировать ключи реестра

сохранить пользовательские папки TeamSpeak

Это позволяет восстановить данные при необходимости.
