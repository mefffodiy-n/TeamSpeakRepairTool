using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Win32;

public sealed class MainForm : Form
{
    private readonly Button btnCleanup = new() { Text = "1. Удаление TS", Width = 180, Height = 40, UseVisualStyleBackColor = true };
    private readonly Button btnFix = new() { Text = "2. Фикс протокола", Width = 180, Height = 40, UseVisualStyleBackColor = true };
    private readonly Button btnOpenFolder = new() { Text = "Открыть папку логов", Width = 170, Height = 40, UseVisualStyleBackColor = true };
    private readonly Button btnClear = new() { Text = "Очистить лог", Width = 130, Height = 40, UseVisualStyleBackColor = true };

    private readonly TextBox txtLog = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        ReadOnly = true,
        WordWrap = false,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10f)
    };

    private readonly Label lblStatus = new() { AutoSize = true, Text = "Готово." };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Top, Height = 8, Style = ProgressBarStyle.Continuous };

    private string? workRoot;
    private string? backupRoot;
    private string? registryBackupRoot;
    private string? userBackupRoot;
    private string? logFile;

    public MainForm()
    {
        Text = "TeamSpeak Repair Tool";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1020;
        Height = 760;
        MinimumSize = new Size(940, 680);

        var top = new Panel { Dock = DockStyle.Top, Height = 250 };
        Controls.Add(top);
        Controls.Add(progress);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        body.Controls.Add(txtLog);
        Controls.Add(body);

        var title = new Label
        {
            Text = "TeamSpeak Repair Tool",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top = 16
        };
        top.Controls.Add(title);

        var sub = new Label
        {
            Text = "Сначала используй удаление TS. Фикс нужен только если после установки TS3 ссылки ts3server:// не работают.",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            AutoSize = true,
            Left = 22,
            Top = 68
        };
        top.Controls.Add(sub);

        var infoGroup = new GroupBox
        {
            Text = "Что будет сделано",
            Left = 22,
            Top = 104,
            Width = 950,
            Height = 92
        };
        top.Controls.Add(infoGroup);

        var infoLabel = new Label
        {
            AutoSize = false,
            Left = 12,
            Top = 24,
            Width = 920,
            Height = 56,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Text =
                "1. Удаление TS: запускает штатные деинсталляторы TeamSpeak, делает backup пользовательских данных и ключей реестра, " +
                "сохраняет лог, очищает остаточные привязки и папки, затем упаковывает результаты в архив." +
                Environment.NewLine +
                "2. Фикс протокола: заново регистрирует ts3server:// для TeamSpeak 3. Обычно не нужен, если после удаления и установки TS3 всё уже работает."
        };
        infoGroup.Controls.Add(infoLabel);

        btnCleanup.Left = 22; btnCleanup.Top = 204;
        btnFix.Left = 214; btnFix.Top = 204;
        btnOpenFolder.Left = 406; btnOpenFolder.Top = 204;
        btnClear.Left = 588; btnClear.Top = 204;
        lblStatus.Left = 770; lblStatus.Top = 214;

        top.Controls.AddRange(new Control[] { btnCleanup, btnFix, btnOpenFolder, btnClear, lblStatus });

        btnCleanup.Click += async (_, _) => await HandleCleanupAsync();
        btnFix.Click += async (_, _) => await HandleFixAsync();
        btnOpenFolder.Click += (_, _) => OpenWorkFolder();
        btnClear.Click += (_, _) =>
        {
            txtLog.Clear();
            lblStatus.Text = "Лог очищен.";
        };

        Log("INFO", "Приложение запущено.");
        Log("INFO", "Сначала используй кнопку 1. Кнопка 2 нужна только если после установки TS3 ссылки не работают.");
    }

    private async Task HandleCleanupAsync()
    {
        if (MessageBox.Show(
                "Будет выполнено удаление TeamSpeak штатными деинсталляторами, backup пользовательских данных, export ключей реестра и дочистка хвостов. Продолжить?",
                "Удаление TS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            SetBusy(true, "Выполняется удаление TS...");
            await Task.Run(RunCleanup);
            lblStatus.Text = "Удаление завершено.";
            MessageBox.Show(
                "Операция завершена. Логи, backup и архив сохранены на рабочий стол.",
                "Готово",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log("ERROR", ex.Message);
            lblStatus.Text = "Ошибка.";
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, "Готово.");
        }
    }

    private async Task HandleFixAsync()
    {
        if (MessageBox.Show(
                "Используй фикс только если после установки TS3 ссылки ts3server:// не работают. Продолжить?",
                "Фикс протокола",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            SetBusy(true, "Выполняется фикс протокола...");
            await Task.Run(RunFix);
            lblStatus.Text = "Фикс завершен.";
            MessageBox.Show(
                "Фикс завершен. Проверь ts3server:// через Win+R и затем в браузере.",
                "Готово",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log("ERROR", ex.Message);
            lblStatus.Text = "Ошибка.";
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, "Готово.");
        }
    }

    private void SetBusy(bool busy, string status)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetBusy(busy, status));
            return;
        }

        btnCleanup.Enabled = !busy;
        btnFix.Enabled = !busy;
        btnOpenFolder.Enabled = !busy;
        btnClear.Enabled = !busy;
        progress.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        lblStatus.Text = status;
        Application.DoEvents();
    }

    private void InitSession(string actionName)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        workRoot = Path.Combine(desktop, $"TS-Tool-{actionName}-{stamp}");
        backupRoot = Path.Combine(workRoot, "Backup");
        registryBackupRoot = Path.Combine(backupRoot, "Registry");
        userBackupRoot = Path.Combine(backupRoot, "UserData");

        Directory.CreateDirectory(workRoot);
        Directory.CreateDirectory(backupRoot);
        Directory.CreateDirectory(registryBackupRoot);
        Directory.CreateDirectory(userBackupRoot);

        logFile = Path.Combine(workRoot, "App.log");
        File.WriteAllText(logFile, string.Empty);
    }

    private void Log(string level, string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(level, message));
            return;
        }

        var line = $"[{DateTime.Now:HH:mm:ss}] [{level.ToUpper()}] {message}";
        txtLog.AppendText(line + Environment.NewLine);
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
        Application.DoEvents();

        if (!string.IsNullOrWhiteSpace(logFile))
            File.AppendAllText(logFile!, line + Environment.NewLine);
    }

    private void OpenWorkFolder()
    {
        if (!string.IsNullOrWhiteSpace(workRoot) && Directory.Exists(workRoot))
            Process.Start("explorer.exe", workRoot);
        else
            MessageBox.Show("Папка логов еще не создана. Сначала запусти действие.", "Информация",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void DeleteRegistryTreeSafe(RegistryKey baseKey, string path)
    {
        try { baseKey.DeleteSubKeyTree(path, false); } catch { }
    }

    private static bool RegistryKeyExists(RegistryKey baseKey, string path)
    {
        using var key = baseKey.OpenSubKey(path);
        return key != null;
    }

    private void BackupRegistryKey(string displayKey)
    {
        try
        {
            var parts = displayKey.Split('\\', 2);
            if (parts.Length != 2)
            {
                Log("WARN", $"Неподдерживаемый путь реестра: {displayKey}");
                return;
            }

            RegistryKey? baseKey = parts[0] switch
            {
                "HKCU" => Registry.CurrentUser,
                "HKLM" => Registry.LocalMachine,
                "HKCR" => Registry.ClassesRoot,
                _ => null
            };

            if (baseKey == null || !RegistryKeyExists(baseKey, parts[1]))
            {
                Log("WARN", $"Ключ не экспортирован (возможно отсутствует): {displayKey}");
                return;
            }

            var safeName = string.Concat(displayKey.Select(ch => "\\/:*?\"<>| ".Contains(ch) ? '_' : ch)) + ".reg";
            var outFile = Path.Combine(registryBackupRoot!, safeName);

            var regExe = Path.Combine(Environment.SystemDirectory, "reg.exe");
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = regExe,
                Arguments = $"export \"{displayKey}\" \"{outFile}\" /y",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            p!.WaitForExit();

            if (p.ExitCode == 0)
                Log("OK", $"Экспортирован ключ реестра: {displayKey}");
            else
                Log("WARN", $"Ошибка экспорта ключа: {displayKey} | ExitCode: {p.ExitCode}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Ошибка экспорта ключа: {displayKey} | {ex.Message}");
        }
    }

    private void BackupFolder(string source)
    {
        try
        {
            if (!Directory.Exists(source))
            {
                Log("INFO", $"Папка не найдена, пропуск: {source}");
                return;
            }

            var name = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar));
            var destination = Path.Combine(userBackupRoot!, name);
            CopyDirectory(source, destination);
            Log("OK", $"Сделан backup: {source}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Ошибка backup: {source} | {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
    }

    private void RemoveRegistryPath(string registryPath)
    {
        try
        {
            if (!registryPath.StartsWith("Registry::", StringComparison.OrdinalIgnoreCase))
            {
                Log("WARN", $"Неподдерживаемый путь реестра: {registryPath}");
                return;
            }

            var tail = registryPath["Registry::".Length..];
            var parts = tail.Split('\\', 2);
            if (parts.Length != 2)
            {
                Log("WARN", $"Неподдерживаемый путь реестра: {registryPath}");
                return;
            }

            RegistryKey? baseKey = parts[0] switch
            {
                "HKEY_CURRENT_USER" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                _ => null
            };

            if (baseKey == null)
            {
                Log("WARN", $"Неподдерживаемый путь реестра: {registryPath}");
                return;
            }

            if (!RegistryKeyExists(baseKey, parts[1]))
            {
                Log("INFO", $"Ключ не найден, пропуск: {registryPath}");
                return;
            }

            DeleteRegistryTreeSafe(baseKey, parts[1]);

            if (!RegistryKeyExists(baseKey, parts[1]))
                Log("OK", $"Удален ключ реестра: {registryPath}");
            else
                Log("WARN", $"Ключ остался после удаления: {registryPath}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Ошибка удаления ключа: {registryPath} | {ex.Message}");
        }
    }

    private void RemovePath(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Log("OK", $"Удалено: {path}");
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                Log("OK", $"Удален файл: {path}");
            }
            else
            {
                Log("INFO", $"Путь не найден, пропуск: {path}");
            }
        }
        catch (Exception ex)
        {
            Log("WARN", $"Не удалось удалить: {path} | {ex.Message}");
        }
    }

    private static (string exe, string args) ParseUninstallCommand(string command)
    {
        command = command.Trim();
        if (string.IsNullOrWhiteSpace(command)) return (string.Empty, string.Empty);

        if (command.StartsWith('"'))
        {
            var second = command.IndexOf('"', 1);
            if (second > 1)
            {
                var exe = command[1..second];
                var args = command[(second + 1)..].Trim();
                return (exe, args);
            }
        }

        var firstSpace = command.IndexOf(' ');
        if (firstSpace < 0) return (command, string.Empty);
        return (command[..firstSpace], command[(firstSpace + 1)..].Trim());
    }

    private void RunUninstallerWithTimeout(string name, string command, int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            Log("INFO", $"Нет команды деинсталляции для {name}");
            return;
        }

        Log("INFO", $"Запуск штатного деинсталлятора: {name}");
        Log("INFO", $"Команда: {command}");

        var (exe, args) = ParseUninstallCommand(command);
        if (string.IsNullOrWhiteSpace(exe))
        {
            Log("WARN", $"Не удалось разобрать команду деинсталляции для {name}");
            return;
        }

        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });

            if (p == null)
            {
                Log("WARN", $"Не удалось запустить деинсталлятор для {name}");
                return;
            }

            if (p.WaitForExit(timeoutSeconds * 1000))
            {
                Log("OK", $"Деинсталлятор завершился. Код: {p.ExitCode}");
            }
            else
            {
                Log("WARN", $"Таймаут деинсталлятора ({timeoutSeconds} сек). Завершаю процесс...");
                try { p.Kill(true); } catch { }

                foreach (var msiexec in Process.GetProcessesByName("msiexec"))
                {
                    try { msiexec.Kill(true); } catch { }
                }

                Log("WARN", $"Зависший деинсталлятор остановлен: {name}");
            }
        }
        catch (Exception ex)
        {
            Log("WARN", $"Ошибка запуска деинсталлятора для {name}: {ex.Message}");
        }
    }

    private sealed record ProductInfo(string Name, string? Version, string? Publisher, string? UninstallString, string? QuietUninstallString);

    private List<ProductInfo> FindInstalledProducts()
    {
        var list = new List<ProductInfo>();

        foreach (var rootPath in new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        })
        {
            foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                using var root = hive.OpenSubKey(rootPath);
                if (root == null) continue;

                foreach (var subName in root.GetSubKeyNames())
                {
                    using var sub = root.OpenSubKey(subName);
                    if (sub == null) continue;

                    var displayName = sub.GetValue("DisplayName")?.ToString();
                    if (string.IsNullOrWhiteSpace(displayName) ||
                        !displayName.Contains("TeamSpeak", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var info = new ProductInfo(
                        displayName,
                        sub.GetValue("DisplayVersion")?.ToString(),
                        sub.GetValue("Publisher")?.ToString(),
                        sub.GetValue("UninstallString")?.ToString(),
                        sub.GetValue("QuietUninstallString")?.ToString());

                    list.Add(info);
                    Log("INFO", $"Найдено: {info.Name} | Version: {info.Version} | Publisher: {info.Publisher}");
                }
            }
        }

        if (list.Count == 0)
            Log("INFO", "Установленные продукты TeamSpeak не найдены.");

        return list;
    }

    private bool TestRegistryPathExists(string registryPath)
    {
        if (!registryPath.StartsWith("Registry::", StringComparison.OrdinalIgnoreCase))
            return false;

        var tail = registryPath["Registry::".Length..];
        var parts = tail.Split('\\', 2);
        if (parts.Length != 2) return false;

        using var key = parts[0] switch
        {
            "HKEY_CURRENT_USER" => Registry.CurrentUser.OpenSubKey(parts[1]),
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine.OpenSubKey(parts[1]),
            "HKEY_CLASSES_ROOT" => Registry.ClassesRoot.OpenSubKey(parts[1]),
            _ => null
        };

        return key != null;
    }

    private void CompressResults()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var zipPath = Path.Combine(desktop, Path.GetFileName(workRoot!) + ".zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(workRoot!, zipPath);
            Log("OK", $"Создан архив: {zipPath}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Не удалось создать архив: {ex.Message}");
        }
    }

    private void RunCleanup()
    {
        InitSession("cleanup");

        string[] registryKeysToBackup =
        {
            @"HKCU\Software\TeamSpeak",
            @"HKLM\SOFTWARE\TeamSpeak",
            @"HKLM\SOFTWARE\WOW6432Node\TeamSpeak",
            @"HKCR\teamspeak",
            @"HKCR\ts3server",
            @"HKCR\ts3file",
            @"HKCR\ts3image",
            @"HKCU\Software\Classes\teamspeak",
            @"HKCU\Software\Classes\ts3server",
            @"HKCU\Software\Classes\ts3file",
            @"HKCU\Software\Classes\ts3image",
            @"HKLM\SOFTWARE\Classes\teamspeak",
            @"HKLM\SOFTWARE\Classes\ts3server",
            @"HKLM\SOFTWARE\Classes\ts3file",
            @"HKLM\SOFTWARE\Classes\ts3image",
            @"HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\teamspeak",
            @"HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\ts3server",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win64.exe",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win32.exe",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win64.exe",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win32.exe"
        };

        string[] registryPathsToRemove =
        {
            @"Registry::HKEY_CURRENT_USER\Software\TeamSpeak",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\TeamSpeak",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\TeamSpeak",
            @"Registry::HKEY_CLASSES_ROOT\teamspeak",
            @"Registry::HKEY_CLASSES_ROOT\ts3server",
            @"Registry::HKEY_CLASSES_ROOT\ts3file",
            @"Registry::HKEY_CLASSES_ROOT\ts3image",
            @"Registry::HKEY_CURRENT_USER\Software\Classes\teamspeak",
            @"Registry::HKEY_CURRENT_USER\Software\Classes\ts3server",
            @"Registry::HKEY_CURRENT_USER\Software\Classes\ts3file",
            @"Registry::HKEY_CURRENT_USER\Software\Classes\ts3image",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\teamspeak",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ts3server",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ts3file",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ts3image",
            @"Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\teamspeak",
            @"Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\ts3server",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win64.exe",
            @"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win32.exe",
            @"Registry::HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win64.exe",
            @"Registry::HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ts3client_win32.exe"
        };

        string[] userDataPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TS3Client"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TeamSpeak"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeamSpeak"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeamSpeak Client")
        };

        string[] installPathsToRemove =
        {
            @"C:\Program Files\TeamSpeak",
            @"C:\Program Files\TeamSpeak 3 Client",
            @"C:\Program Files (x86)\TeamSpeak",
            @"C:\Program Files (x86)\TeamSpeak 3 Client"
        };

        Log("INFO", "Старт очистки.");
        Log("INFO", $"Компьютер: {Environment.MachineName}");
        Log("INFO", $"Пользователь: {Environment.UserName}");
        Log("INFO", $"Рабочая папка: {workRoot}");

        Log("INFO", "Шаг 1/8. Поиск установленного TeamSpeak");
        var products = FindInstalledProducts();

        Log("INFO", "Шаг 2/8. Backup реестра и данных пользователя");
        foreach (var key in registryKeysToBackup) BackupRegistryKey(key);
        foreach (var path in userDataPaths) BackupFolder(path);

        Log("INFO", "Шаг 3/8. Запуск штатных деинсталляторов");
        foreach (var product in products)
        {
            var command = !string.IsNullOrWhiteSpace(product.QuietUninstallString)
                ? product.QuietUninstallString
                : product.UninstallString;

            if (!string.IsNullOrWhiteSpace(command))
                RunUninstallerWithTimeout(product.Name, command!, 30);
            else
                Log("INFO", $"Нет деинсталлятора для {product.Name}");
        }

        Log("INFO", "Шаг 4/8. Остановка оставшихся процессов");
        foreach (var processName in new[] { "ts3client_win64", "ts3client_win32", "teamspeak" })
        {
            foreach (var p in Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill(true);
                    Log("OK", $"Остановлен процесс: {p.ProcessName} (PID {p.Id})");
                }
                catch
                {
                    Log("WARN", $"Не удалось остановить процесс: {p.ProcessName} (PID {p.Id})");
                }
            }
        }

        foreach (var p in Process.GetProcessesByName("msiexec"))
        {
            try { p.Kill(true); } catch { }
        }

        Log("INFO", "Шаг 5/8. Очистка реестра и привязок");
        foreach (var path in registryPathsToRemove) RemoveRegistryPath(path);

        Log("INFO", "Шаг 6/8. Удаление оставшихся папок");
        foreach (var path in userDataPaths.Concat(installPathsToRemove)) RemovePath(path);

        Log("INFO", "Шаг 7/8. Проверка остатков");
        var leftovers = registryPathsToRemove.Where(TestRegistryPathExists)
            .Concat(userDataPaths.Where(Directory.Exists))
            .Concat(installPathsToRemove.Where(Directory.Exists))
            .ToList();

        if (leftovers.Count == 0)
        {
            Log("OK", "Остатков TeamSpeak не найдено.");
            Log("INFO", "Система готова к чистой установке TS3.");
        }
        else
        {
            foreach (var item in leftovers)
                Log("WARN", $"Остаток найден: {item}");
        }

        Log("INFO", "Шаг 8/8. Упаковка результатов");
        CompressResults();

        Log("OK", "Очистка завершена.");
        Log("INFO", $"Лог: {logFile}");
        Log("INFO", $"Backup: {backupRoot}");
    }

    private void RunFix()
    {
        InitSession("fix");
        Log("INFO", "Старт фикса протокола.");

        var tsExeCandidates = new[]
        {
            @"C:\Program Files\TeamSpeak 3 Client\ts3client_win64.exe",
            @"C:\Program Files (x86)\TeamSpeak 3 Client\ts3client_win32.exe"
        };

        var tsExe = tsExeCandidates.FirstOrDefault(File.Exists);
        if (tsExe == null)
            throw new Exception("TS3 не найден. Сначала установи TeamSpeak 3.");

        Log("OK", $"Найден TS3: {tsExe}");

        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\ts3server", false); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\teamspeak", false); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ts3server", false); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\teamspeak", false); } catch { }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ts3server"))
        {
            key!.SetValue(null, "URL:TeamSpeak 3 Protocol");
            key.SetValue("URL Protocol", "");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ts3server\DefaultIcon"))
        {
            key!.SetValue(null, tsExe);
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ts3server\shell\open\command"))
        {
            key!.SetValue(null, $"\"{tsExe}\" \"%1\"");
        }

        Log("OK", $"Зарегистрирован ts3server:// -> {tsExe}");

        foreach (var p in Process.GetProcessesByName("explorer"))
        {
            try { p.Kill(true); } catch { }
        }

        Process.Start("explorer.exe");
        Log("OK", "Explorer перезапущен");

        CompressResults();

        Log("OK", "Фикс завершен.");
        Log("INFO", $"Лог: {logFile}");
    }
}