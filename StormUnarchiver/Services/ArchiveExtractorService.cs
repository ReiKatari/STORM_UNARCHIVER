using SharpCompress.Archives;
using SharpCompress.Common;

namespace StormUnarchiver.Services;

public class ArchiveExtractorService
{
    // All supported archive extensions (comprehensive list)
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // === Core formats (SharpCompress native) ===
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".lz",

        // === TAR compound formats ===
        ".tgz", ".tbz2", ".txz", ".tlz",
        ".tar.gz", ".tar.bz2", ".tar.xz", ".tar.lz",
        ".tar.zst", ".tar.lz4", ".tar.lzma",

        // === Compressed single-file streams ===
        ".gzip", ".bzip2", ".lzma", ".lzip",
        ".zst", ".zstd",   // Zstandard
        ".lz4",            // LZ4
        ".z",              // Unix compress
        ".sz",             // Snappy

        // === ZIP-based containers ===
        ".zipx",           // Extended ZIP
        ".jar", ".war", ".ear",  // Java
        ".apk", ".aab",   // Android
        ".ipa",            // iOS
        ".xpi",            // Firefox/Thunderbird extensions
        ".crx",            // Chrome extensions
        ".epub",           // eBooks
        ".odt", ".ods", ".odp", ".odg",  // OpenDocument
        ".nupkg",          // NuGet
        ".vsix",           // VS extensions
        ".xap",            // Silverlight

        // === Comic book archives ===
        ".cbz",            // Comic ZIP
        ".cbr",            // Comic RAR
        ".cb7",            // Comic 7z
        ".cbt",            // Comic TAR

        // === System/Package formats ===
        ".cab",            // Windows Cabinet
        ".msi",            // Windows Installer
        ".iso",            // Disc image
        ".img",            // Disk image
        ".dmg",            // macOS disk image
        ".rpm",            // Red Hat package
        ".deb",            // Debian package
        ".pkg",            // macOS package
        ".cpio",           // CPIO archive
        ".wim", ".swm", ".esd",  // Windows Imaging
        ".vhd", ".vhdx",  // Virtual Hard Disk

        // === Legacy/Classic formats ===
        ".arj",            // ARJ
        ".lzh", ".lha",   // LHA/LZH
        ".arc",            // ARC
        ".ace",            // ACE
        ".zoo",            // Zoo
        ".sit", ".sitx",  // StuffIt
        ".sea",            // Self-Extracting Archive (Mac)
        ".pea",            // PEA
        ".a", ".ar",       // Unix archive
        ".shar",           // Shell archive
        ".sqx",            // SQX
        ".alz",            // ALZip
        ".egg",            // EGG (Korean)
        ".uue", ".uu",    // UUEncode

        // === Split/Multi-volume ===
        ".001", ".part1",  // Split volumes
        ".r00", ".r01",   // RAR volumes

        // === Specialized ===
        ".xar",            // eXtensible ARchive
        ".warc",           // Web ARChive
        ".zim",            // ZIM (Wikipedia offline)
        ".pak",            // Game PAK
        ".vpk",            // Valve PAK
        ".bsa",            // Bethesda archive
        ".hpk",            // HPK archive
        ".mpq",            // Blizzard MPQ
    };

    // Double/compound extensions to check
    private static readonly string[] DoubleExtensions =
    {
        ".tar.gz", ".tar.bz2", ".tar.xz", ".tar.lz",
        ".tar.zst", ".tar.lz4", ".tar.lzma",
        ".tar.sz",
    };

    public static bool IsArchive(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (SupportedExtensions.Contains(ext))
            return true;

        // Check for double extensions like .tar.gz
        var name = Path.GetFileName(filePath);
        foreach (var doubleExt in DoubleExtensions)
        {
            if (name.EndsWith(doubleExt, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Extracts archive, renames files to match archive name, moves to output folder.
    /// Returns (success, extractedFiles, errorMessage).
    /// </summary>
    public static (bool Success, List<string> Files, string? Error) ExtractAndMove(
        string archivePath, string outputFolder, bool deleteArchive = true,
        bool preserveStructure = false, string? password = null)
    {
        var extractedFiles = new List<string>();
        var archiveDir = Path.GetDirectoryName(archivePath)!;
        var archiveName = GetArchiveBaseName(archivePath);
        var tempExtractDir = Path.Combine(archiveDir, $"_storm_temp_{Guid.NewGuid():N}");

        try
        {
            // Ensure output folder exists
            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(tempExtractDir);

            // Wait for file to be fully written
            WaitForFile(archivePath, TimeSpan.FromSeconds(30));

            // Open with optional password
            var readerOptions = new SharpCompress.Readers.ReaderOptions();
            if (!string.IsNullOrEmpty(password))
                readerOptions.Password = password;

            // Extract archive to temp directory — explicit using block to release file handle
            using (var archive = ArchiveFactory.Open(archivePath, readerOptions))
            {
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    entry.WriteToDirectory(tempExtractDir, new ExtractionOptions
                    {
                        ExtractFullPath = preserveStructure,
                        Overwrite = true
                    });
                }
            }
            // Archive file handle is now released

            // Get extracted files (recursively if structure was preserved)
            var files = Directory.GetFiles(tempExtractDir,
                "*", preserveStructure ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                return (false, extractedFiles, "Архив пуст");
            }

            if (files.Length == 1 && !preserveStructure)
            {
                // Single file — rename to archive name + original extension
                var file = files[0];
                var ext = Path.GetExtension(file);
                var destName = archiveName + ext;
                var destPath = Path.Combine(outputFolder, destName);

                // Overwrite if file already exists
                if (File.Exists(destPath))
                    File.Delete(destPath);
                File.Move(file, destPath);
                extractedFiles.Add(Path.GetFileName(destPath));
            }
            else
            {
                // Multiple files or preserveStructure — create subfolder with archive name
                var subFolder = Path.Combine(outputFolder, archiveName);
                Directory.CreateDirectory(subFolder);

                foreach (var file in files)
                {
                    // Compute relative path from temp dir to preserve structure
                    var relativePath = preserveStructure
                        ? Path.GetRelativePath(tempExtractDir, file)
                        : Path.GetFileName(file);

                    var destPath = Path.Combine(subFolder, relativePath);

                    // Ensure subdirectory exists
                    var destDir = Path.GetDirectoryName(destPath);
                    if (destDir != null) Directory.CreateDirectory(destDir);

                    // Overwrite if file already exists
                    if (File.Exists(destPath))
                        File.Delete(destPath);
                    File.Move(file, destPath);
                    extractedFiles.Add(relativePath);
                }
            }

            // Delete the original archive (permanently, bypassing recycle bin)
            if (deleteArchive)
            {
                try { File.Delete(archivePath); } catch { /* ignore */ }
            }

            return (true, extractedFiles, null);
        }
        catch (Exception ex)
        {
            return (false, extractedFiles, ex.Message);
        }
        finally
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(tempExtractDir))
                    Directory.Delete(tempExtractDir, true);
            }
            catch { /* ignore cleanup errors */ }
        }
    }

    private static string GetArchiveBaseName(string path)
    {
        var name = Path.GetFileName(path);

        // Handle double extensions
        foreach (var ext in DoubleExtensions)
        {
            if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return name[..^ext.Length];
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{name} ({counter}){ext}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    private static string GetUniqueDirectoryPath(string path)
    {
        if (!Directory.Exists(path)) return path;

        var counter = 1;
        string newPath;
        do
        {
            newPath = $"{path} ({counter})";
            counter++;
        } while (Directory.Exists(newPath));

        return newPath;
    }

    private static void WaitForFile(string path, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return; // File is available
            }
            catch (IOException)
            {
                Thread.Sleep(500);
            }
        }
    }
}
