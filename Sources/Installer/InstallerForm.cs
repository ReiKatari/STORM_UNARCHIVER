using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StormUnarchiver.Installer;

public class InstallerForm : Form
{
    private TextBox _targetDirBox = null!;
    private Button _browseButton = null!;
    private CheckBox _desktopShortcutCheck = null!;
    private CheckBox _startMenuShortcutCheck = null!;
    private CheckBox _registryCheck = null!;
    private CheckBox _launchAfterCheck = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Button _installButton = null!;
    private Button _cancelButton = null!;
    private Panel _cardPanel = null!;

    public InstallerForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.Text = $"{Program.AppName} v{Program.Version} — Установка";
        this.Size = new Size(540, 480);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(15, 23, 42); // Dark slate #0F172A
        this.ForeColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Try load embedded icon
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var iconStream = asm.GetManifestResourceStream("Installer.Assets.app.ico") 
                                ?? asm.GetManifestResourceStream("Assets.app.ico");
            if (iconStream != null)
            {
                this.Icon = new Icon(iconStream);
            }
        }
        catch { }

        // Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Color.FromArgb(30, 41, 59), // #1E293B
            Padding = new Padding(20, 12, 20, 12)
        };

        var titleLabel = new Label
        {
            Text = "STORM UNARCHIVER",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(76, 201, 240), // #4CC9F0 Accent
            AutoSize = true,
            Location = new Point(20, 14)
        };

        var subtitleLabel = new Label
        {
            Text = $"Мастер установки версии v{Program.Version} для Windows",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184), // #94A3B8
            AutoSize = true,
            Location = new Point(22, 46)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        this.Controls.Add(headerPanel);

        // Main Card Panel
        _cardPanel = new Panel
        {
            Location = new Point(20, 100),
            Size = new Size(484, 250),
            BackColor = Color.FromArgb(24, 34, 53),
            Padding = new Padding(16)
        };

        // Folder selection
        var folderLabel = new Label
        {
            Text = "Папка для установки:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 16),
            AutoSize = true
        };

        var defaultInstallPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            Program.AppName);

        _targetDirBox = new TextBox
        {
            Text = defaultInstallPath,
            Location = new Point(16, 40),
            Size = new Size(348, 26),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };

        _browseButton = new Button
        {
            Text = "Обзор...",
            Location = new Point(372, 38),
            Size = new Size(96, 28),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _browseButton.FlatAppearance.BorderSize = 0;
        _browseButton.Click += BrowseButton_Click;

        // Options
        var optionsLabel = new Label
        {
            Text = "Параметры установки:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 80),
            AutoSize = true
        };

        _desktopShortcutCheck = new CheckBox
        {
            Text = "Создать ярлык на Рабочем столе (с иконкой)",
            Checked = true,
            Location = new Point(20, 106),
            Size = new Size(440, 24),
            ForeColor = Color.FromArgb(226, 232, 240),
            Cursor = Cursors.Hand
        };

        _startMenuShortcutCheck = new CheckBox
        {
            Text = "Добавить ярлыки в меню «Пуск»",
            Checked = true,
            Location = new Point(20, 134),
            Size = new Size(440, 24),
            ForeColor = Color.FromArgb(226, 232, 240),
            Cursor = Cursors.Hand
        };

        _registryCheck = new CheckBox
        {
            Text = "Зарегистрировать в списке установленных программ (реестр)",
            Checked = true,
            Location = new Point(20, 162),
            Size = new Size(440, 24),
            ForeColor = Color.FromArgb(226, 232, 240),
            Cursor = Cursors.Hand
        };

        _launchAfterCheck = new CheckBox
        {
            Text = "Запустить STORM UNARCHIVER после завершения",
            Checked = true,
            Location = new Point(20, 190),
            Size = new Size(440, 24),
            ForeColor = Color.FromArgb(76, 201, 240),
            Cursor = Cursors.Hand
        };

        _cardPanel.Controls.Add(folderLabel);
        _cardPanel.Controls.Add(_targetDirBox);
        _cardPanel.Controls.Add(_browseButton);
        _cardPanel.Controls.Add(optionsLabel);
        _cardPanel.Controls.Add(_desktopShortcutCheck);
        _cardPanel.Controls.Add(_startMenuShortcutCheck);
        _cardPanel.Controls.Add(_registryCheck);
        _cardPanel.Controls.Add(_launchAfterCheck);
        this.Controls.Add(_cardPanel);

        // Progress Bar & Status
        _progressBar = new ProgressBar
        {
            Location = new Point(20, 362),
            Size = new Size(484, 18),
            Style = ProgressBarStyle.Continuous,
            Value = 0,
            Visible = false
        };
        this.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            Text = "Нажмите «Установить» для продолжения.",
            Location = new Point(22, 386),
            Size = new Size(320, 36),
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9f)
        };
        this.Controls.Add(_statusLabel);

        // Action Buttons
        _cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(414, 388),
            Size = new Size(90, 34),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _cancelButton.FlatAppearance.BorderSize = 0;
        _cancelButton.Click += (_, _) => this.Close();
        this.Controls.Add(_cancelButton);

        _installButton = new Button
        {
            Text = "Установить",
            Location = new Point(296, 388),
            Size = new Size(110, 34),
            BackColor = Color.FromArgb(76, 201, 240), // #4CC9F0
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += InstallButton_Click;
        this.Controls.Add(_installButton);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку для установки STORM UNARCHIVER",
            UseDescriptionForTitle = true,
            SelectedPath = _targetDirBox.Text
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _targetDirBox.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        var targetDir = _targetDirBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir))
        {
            MessageBox.Show("Пожалуйста, укажите папку для установки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Disable controls
        _installButton.Enabled = false;
        _cancelButton.Enabled = false;
        _browseButton.Enabled = false;
        _targetDirBox.ReadOnly = true;
        _desktopShortcutCheck.Enabled = false;
        _startMenuShortcutCheck.Enabled = false;
        _registryCheck.Enabled = false;
        _launchAfterCheck.Enabled = false;

        _progressBar.Visible = true;
        _progressBar.Value = 10;

        try
        {
            _statusLabel.Text = "Подготовка файлов...";
            await Task.Delay(200);

            // Terminate running instance if any
            foreach (var proc in Process.GetProcessesByName("StormUnarchiver"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }

            // Create target directory
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            _progressBar.Value = 30;
            _statusLabel.Text = "Распаковка компонентов программы...";

            // Extract payload
            await Task.Run(() => ExtractPayload(targetDir));

            _progressBar.Value = 70;
            _statusLabel.Text = "Создание ярлыков и регистрация...";

            var mainExe = Path.Combine(targetDir, "StormUnarchiver.exe");
            var iconFile = Path.Combine(targetDir, "Assets", "app.ico");

            // Copy self as Uninstall.exe
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            var uninstallExe = Path.Combine(targetDir, "Uninstall.exe");
            if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
            {
                try { File.Copy(currentExe, uninstallExe, true); } catch { }
            }

            // 1. Desktop Shortcut
            if (_desktopShortcutCheck.Checked)
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var shortcutFile = Path.Combine(desktopPath, $"{Program.AppName}.lnk");
                CreateShortcut(shortcutFile, mainExe, targetDir, iconFile, "STORM UNARCHIVER — Автоматическая распаковка архивов");
            }

            // 2. Start Menu Shortcut
            if (_startMenuShortcutCheck.Checked)
            {
                var startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    Program.AppName);
                Directory.CreateDirectory(startMenuDir);

                var appShortcut = Path.Combine(startMenuDir, $"{Program.AppName}.lnk");
                CreateShortcut(appShortcut, mainExe, targetDir, iconFile, "STORM UNARCHIVER");

                if (File.Exists(uninstallExe))
                {
                    var uninstShortcut = Path.Combine(startMenuDir, $"Удалить {Program.AppName}.lnk");
                    CreateShortcut(uninstShortcut, uninstallExe, targetDir, iconFile, "Удаление STORM UNARCHIVER", "/uninstall");
                }
            }

            // 3. Registry Entries
            if (_registryCheck.Checked)
            {
                RegisterInWindows(targetDir, mainExe, iconFile, uninstallExe);
            }

            _progressBar.Value = 100;
            _statusLabel.Text = "Установка успешно завершена!";
            _statusLabel.ForeColor = Color.FromArgb(74, 222, 128); // Green #4ADE80

            _installButton.Text = "Готово";
            _installButton.Enabled = true;
            _installButton.Click -= InstallButton_Click;
            _installButton.Click += (_, _) =>
            {
                if (_launchAfterCheck.Checked && File.Exists(mainExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = mainExe,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }
                this.Close();
            };
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Ошибка: {ex.Message}";
            _statusLabel.ForeColor = Color.FromArgb(248, 113, 113);
            _cancelButton.Enabled = true;
            MessageBox.Show($"Ошибка установки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExtractPayload(string targetDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Installer.payload.zip")
                        ?? asm.GetManifestResourceStream("payload.zip");

        if (stream != null)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            zip.ExtractToDirectory(targetDir, overwriteFiles: true);
        }
        else
        {
            // Fallback: copy from adjacent Assembling directory if running locally
            var adjacentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Assembling");
            if (Directory.Exists(adjacentDir))
            {
                CopyDirectory(adjacentDir, targetDir);
            }
            else
            {
                throw new FileNotFoundException("Архив дистрибутива payload.zip не найден в ресурсах установщика.");
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath, string description, string args = "")
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = description;
                if (!string.IsNullOrEmpty(args))
                    shortcut.Arguments = args;
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    shortcut.IconLocation = $"{iconPath},0";
                shortcut.Save();
            }
        }
        catch { }
    }

    private static void RegisterInWindows(string installDir, string mainExe, string iconFile, string uninstallExe)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\STORM UNARCHIVER");
            if (key != null)
            {
                key.SetValue("DisplayName", Program.AppName);
                key.SetValue("DisplayVersion", Program.Version);
                key.SetValue("Publisher", Program.Publisher);
                key.SetValue("DisplayIcon", File.Exists(iconFile) ? iconFile : mainExe);
                key.SetValue("UninstallString", $"\"{uninstallExe}\" /uninstall");
                key.SetValue("InstallLocation", installDir);
                key.SetValue("URLInfoAbout", Program.GitHubUrl);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                // Estimate size in KB
                long sizeBytes = 0;
                try
                {
                    foreach (var f in Directory.GetFiles(installDir, "*", SearchOption.AllDirectories))
                        sizeBytes += new FileInfo(f).Length;
                }
                catch { }
                key.SetValue("EstimatedSize", (int)(sizeBytes / 1024), RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
