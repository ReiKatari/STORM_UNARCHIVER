using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    private StormCheckBox _desktopShortcutCheck = null!;
    private StormCheckBox _startMenuShortcutCheck = null!;
    private StormCheckBox _registryCheck = null!;
    private StormCheckBox _launchAfterCheck = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Button _installButton = null!;
    private Button _cancelButton = null!;
    private Panel _cardPanel = null!;
    private Image? _appIconImage;

    public InstallerForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.Text = $"{Program.AppName} v{Program.Version} — Установка";
        this.Size = new Size(620, 530);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(15, 23, 42); // #0F172A Dark Slate
        this.ForeColor = Color.FromArgb(248, 250, 252);
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Load embedded icons
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var iconStream = asm.GetManifestResourceStream("Installer.Assets.app.ico")
                                ?? asm.GetManifestResourceStream("Assets.app.ico");
            if (iconStream != null)
            {
                this.Icon = new Icon(iconStream);
            }

            using var imgStream = asm.GetManifestResourceStream("Installer.Assets.Square44x44Logo.png")
                               ?? asm.GetManifestResourceStream("Assets.Square44x44Logo.png")
                               ?? asm.GetManifestResourceStream("Installer.Assets.app.png")
                               ?? asm.GetManifestResourceStream("Assets.app.png");
            if (imgStream != null)
            {
                _appIconImage = Image.FromStream(imgStream);
            }
        }
        catch { }

        // 1. Stylized Header Panel (WinUI 3 Theme Matched)
        var headerPanel = new HeaderPanel(_appIconImage, Program.Version)
        {
            Dock = DockStyle.Top,
            Height = 92
        };
        this.Controls.Add(headerPanel);

        // 2. Main Options Card Panel
        _cardPanel = new CardPanel
        {
            Location = new Point(24, 108),
            Size = new Size(556, 280),
            BackColor = Color.FromArgb(24, 30, 48), // #181E30
            Padding = new Padding(18)
        };

        // Folder selection
        var folderLabel = new Label
        {
            Text = "Папка для установки:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 16),
            AutoSize = true
        };

        var defaultInstallPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            Program.AppName);

        _targetDirBox = new TextBox
        {
            Text = defaultInstallPath,
            Location = new Point(18, 42),
            Size = new Size(408, 28),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };

        _browseButton = new Button
        {
            Text = "Обзор...",
            Location = new Point(434, 40),
            Size = new Size(104, 30),
            BackColor = Color.FromArgb(46, 56, 84), // #2E3854
            ForeColor = Color.FromArgb(241, 245, 249),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _browseButton.FlatAppearance.BorderSize = 1;
        _browseButton.FlatAppearance.BorderColor = Color.FromArgb(64, 78, 115);
        _browseButton.Click += BrowseButton_Click;

        // Options Section
        var optionsLabel = new Label
        {
            Text = "Параметры установки:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 86),
            AutoSize = true
        };

        _desktopShortcutCheck = new StormCheckBox
        {
            Text = "Создать ярлык на Рабочем столе (с иконкой)",
            Checked = true,
            Location = new Point(18, 114),
            Size = new Size(520, 28)
        };

        _startMenuShortcutCheck = new StormCheckBox
        {
            Text = "Добавить ярлыки в меню «Пуск»",
            Checked = true,
            Location = new Point(18, 148),
            Size = new Size(520, 28)
        };

        _registryCheck = new StormCheckBox
        {
            Text = "Зарегистрировать в списке установленных программ (реестр)",
            Checked = true,
            Location = new Point(18, 182),
            Size = new Size(520, 28)
        };

        _launchAfterCheck = new StormCheckBox
        {
            Text = "Запустить STORM UNARCHIVER после завершения",
            Checked = true,
            Location = new Point(18, 216),
            Size = new Size(520, 28),
            ForeColor = Color.FromArgb(76, 201, 240)
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

        // 3. Progress Bar & Status
        _progressBar = new ProgressBar
        {
            Location = new Point(24, 400),
            Size = new Size(556, 14),
            Style = ProgressBarStyle.Continuous,
            Value = 0,
            Visible = false
        };
        this.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            Text = "Нажмите «Установить» для продолжения.",
            Location = new Point(24, 426),
            Size = new Size(270, 42),
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9f)
        };
        this.Controls.Add(_statusLabel);

        // 4. Action Buttons (Wide & Beautiful)
        _installButton = new Button
        {
            Text = "Установить",
            Location = new Point(305, 424),
            Size = new Size(150, 40),
            BackColor = Color.FromArgb(76, 201, 240), // #4CC9F0 Cyan
            ForeColor = Color.FromArgb(15, 23, 42),   // Dark Navy Text
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += InstallButton_Click;
        this.Controls.Add(_installButton);

        _cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(465, 424),
            Size = new Size(115, 40),
            BackColor = Color.FromArgb(42, 51, 77), // #2A334D
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _cancelButton.FlatAppearance.BorderSize = 1;
        _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(59, 71, 107);
        _cancelButton.Click += (_, _) => this.Close();
        this.Controls.Add(_cancelButton);
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

// ===== CUSTOM STYLED UI COMPONENTS =====

/// <summary>
/// Custom Header Panel with gradient, logo icon, STORM brand title, and version badge.
/// </summary>
public class HeaderPanel : Panel
{
    private readonly Image? _icon;
    private readonly string _version;

    public HeaderPanel(Image? icon, string version)
    {
        _icon = icon;
        _version = version;
        this.DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Gradient Background #1A1B2E -> #242640 -> #2E3052
        using (var brush = new LinearGradientBrush(this.ClientRectangle,
            Color.FromArgb(26, 27, 46),
            Color.FromArgb(46, 48, 82),
            LinearGradientMode.Horizontal))
        {
            g.FillRectangle(brush, this.ClientRectangle);
        }

        // Bottom border
        using (var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
        {
            g.DrawLine(borderPen, 0, this.Height - 1, this.Width, this.Height - 1);
        }

        // Draw App Icon
        int iconX = 24;
        int iconY = 22;
        int iconSize = 48;
        if (_icon != null)
        {
            g.DrawImage(_icon, new Rectangle(iconX, iconY, iconSize, iconSize));
        }

        // Draw Title: STORM (Cyan) + UNARCHIVER (White)
        int textX = iconX + iconSize + 14;
        int textY = 18;

        using var fontStorm = new Font("Segoe UI", 16f, FontStyle.Bold);
        using var fontUnarchiver = new Font("Segoe UI", 15f, FontStyle.Bold);
        using var fontSubtitle = new Font("Segoe UI", 9f, FontStyle.Regular);
        using var fontBadge = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        var stormSize = g.MeasureString("STORM", fontStorm);
        using (var brushStorm = new SolidBrush(Color.FromArgb(76, 201, 240))) // Cyan #4CC9F0
        {
            g.DrawString("STORM", fontStorm, brushStorm, textX, textY);
        }

        int unarchiverX = textX + (int)stormSize.Width - 4;
        var unarchiverSize = g.MeasureString(" UNARCHIVER", fontUnarchiver);
        using (var brushUnarchiver = new SolidBrush(Color.FromArgb(248, 250, 252)))
        {
            g.DrawString(" UNARCHIVER", fontUnarchiver, brushUnarchiver, unarchiverX, textY + 1);
        }

        // Draw Version Pill Badge
        int badgeX = unarchiverX + (int)unarchiverSize.Width + 4;
        int badgeY = textY + 6;
        var badgeText = $"v{_version}";
        var badgeTextSize = g.MeasureString(badgeText, fontBadge);
        var badgeRect = new Rectangle(badgeX, badgeY, (int)badgeTextSize.Width + 12, 18);

        using (var badgeBg = new SolidBrush(Color.FromArgb(40, 76, 201, 240)))
        using (var badgeBorder = new Pen(Color.FromArgb(120, 76, 201, 240), 1))
        using (var badgeBrush = new SolidBrush(Color.FromArgb(76, 201, 240)))
        {
            g.FillPath(badgeBg, GetRoundedPath(badgeRect, 4));
            g.DrawPath(badgeBorder, GetRoundedPath(badgeRect, 4));
            g.DrawString(badgeText, fontBadge, badgeBrush, badgeRect.X + 6, badgeRect.Y + 2);
        }

        // Draw Subtitle
        using (var subtitleBrush = new SolidBrush(Color.FromArgb(148, 163, 184))) // #94A3B8
        {
            g.DrawString("Мастер быстрой установки • Архивация нового поколения для Windows",
                fontSubtitle, subtitleBrush, textX, textY + 36);
        }
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// Custom Card Panel with subtle border.
/// </summary>
public class CardPanel : Panel
{
    public CardPanel()
    {
        this.DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using var bgBrush = new SolidBrush(Color.FromArgb(24, 30, 48));
        using var borderPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1);
        using var path = GetRoundedPath(rect, 8);

        g.FillPath(bgBrush, path);
        g.DrawPath(borderPen, path);
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// Custom CheckBox with filled square checkmark (Cyan filled when checked, dark box when unchecked).
/// </summary>
public class StormCheckBox : Control
{
    private bool _checked = true;
    private bool _hovered = false;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public StormCheckBox()
    {
        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                       ControlStyles.UserPaint |
                       ControlStyles.OptimizedDoubleBuffer |
                       ControlStyles.ResizeRedraw |
                       ControlStyles.SupportsTransparentBackColor, true);
        this.BackColor = Color.Transparent;
        this.ForeColor = Color.FromArgb(226, 232, 240);
        this.Font = new Font("Segoe UI", 9.5f);
        this.Cursor = Cursors.Hand;
        this.Height = 28;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int boxSize = 18;
        int boxY = (this.Height - boxSize) / 2;
        var boxRect = new Rectangle(2, boxY, boxSize, boxSize);

        if (_checked)
        {
            // Filled Cyan Square (#4CC9F0)
            using var fillBrush = new SolidBrush(Color.FromArgb(76, 201, 240));
            using var borderPen = new Pen(_hovered ? Color.White : Color.FromArgb(76, 201, 240), 1.2f);
            using var path = GetRoundedPath(boxRect, 4);

            g.FillPath(fillBrush, path);
            g.DrawPath(borderPen, path);

            // Crisp Dark Checkmark (#0F172A)
            using var checkPen = new Pen(Color.FromArgb(15, 23, 42), 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLines(checkPen, new Point[]
            {
                new Point(boxRect.Left + 4, boxRect.Top + 9),
                new Point(boxRect.Left + 7, boxRect.Top + 13),
                new Point(boxRect.Left + 14, boxRect.Top + 5)
            });
        }
        else
        {
            // Unchecked Dark Square (#0F172A) with Border
            using var bgBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var borderPen = new Pen(_hovered ? Color.FromArgb(76, 201, 240) : Color.FromArgb(71, 85, 105), 1.5f);
            using var path = GetRoundedPath(boxRect, 4);

            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        // Draw Text
        int textX = boxRect.Right + 10;
        var textRect = new Rectangle(textX, 0, this.Width - textX, this.Height);
        TextRenderer.DrawText(g, this.Text, this.Font, textRect, this.ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
