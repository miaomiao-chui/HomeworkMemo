using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace HomeworkMemo;

public class StorageService
{
    private readonly string _dataDir;
    private readonly string _dataFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public AppSettings Settings { get; private set; } = new();
    public ObservableCollection<HomeworkItem> Items { get; } = new();

    public StorageService()
    {
        _dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HomeworkMemo");
        Directory.CreateDirectory(_dataDir);
        _dataFile = Path.Combine(_dataDir, "data.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_dataFile))
            {
                var dto = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(_dataFile), JsonOptions);
                if (dto != null)
                {
                    if (dto.Settings != null) Settings = dto.Settings;
                    if (dto.Items != null)
                        foreach (var it in dto.Items) Items.Add(it);
                }
            }
        }
        catch
        {
            // 数据损坏时回退为默认，不中断启动
        }

        if (string.IsNullOrWhiteSpace(Settings.StorageRoot))
            Settings.StorageRoot = Path.Combine(_dataDir, "files");

        Directory.CreateDirectory(Settings.StorageRoot);
    }

    public void Save()
    {
        var dto = new DataFile { Settings = Settings, Items = Items.ToList() };
        File.WriteAllText(_dataFile, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public string ResolvePath(string relativePath)
        => Path.Combine(Settings.StorageRoot, relativePath);

    public Attachment AddAttachment(HomeworkItem item, string sourcePath)
    {
        var taskDir = Path.Combine(Settings.StorageRoot, "attachments", item.Id);
        Directory.CreateDirectory(taskDir);

        var dest = UniquePath(taskDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, dest, overwrite: false);

        var att = new Attachment
        {
            OriginalName = Path.GetFileName(dest),
            RelativePath = Path.GetRelativePath(Settings.StorageRoot, dest),
            Size = new FileInfo(dest).Length,
        };
        item.Attachments.Add(att);
        Save();
        return att;
    }

    public void OpenAttachment(Attachment att)
    {
        var full = ResolvePath(att.RelativePath);
        if (File.Exists(full))
            Process.Start(new ProcessStartInfo(full) { UseShellExecute = true });
        else
            MessageBox.Show($"文件不存在：\n{full}", "无法打开", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void RemoveAttachment(HomeworkItem item, Attachment att)
    {
        if (att.Kind != "text")
        {
            var full = ResolvePath(att.RelativePath);
            try { if (File.Exists(full)) File.Delete(full); } catch { }
        }
        item.Attachments.Remove(att);
        Save();
    }

    public void AddTextNote(HomeworkItem item, string text)
    {
        item.Attachments.Add(new Attachment { Kind = "text", Text = text });
        Save();
    }

    public void UpdateTextNote(Attachment att, string text)
    {
        att.Text = text;
        Save();
    }

    public void DeleteItem(HomeworkItem item)
    {
        foreach (var att in item.Attachments.ToList())
        {
            if (att.Kind != "text")
            {
                var full = ResolvePath(att.RelativePath);
                try { if (File.Exists(full)) File.Delete(full); } catch { }
            }
        }
        Items.Remove(item);
        Save();
    }

    public void ChangeStorageRoot(string newRoot, bool migrate)
    {
        newRoot = Path.GetFullPath(newRoot);
        Directory.CreateDirectory(newRoot);

        var oldAtt = Path.Combine(Settings.StorageRoot, "attachments");
        var newAtt = Path.Combine(newRoot, "attachments");

        if (migrate
            && Directory.Exists(oldAtt)
            && !string.Equals(oldAtt, newAtt, StringComparison.OrdinalIgnoreCase))
        {
            CopyDirectory(oldAtt, newAtt);
        }

        Settings.StorageRoot = newRoot;
        Save();
    }

    private static string UniquePath(string dir, string name)
    {
        var dest = Path.Combine(dir, name);
        if (!File.Exists(dest)) return dest;

        var baseName = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (int i = 1; ; i++)
        {
            dest = Path.Combine(dir, $"{baseName} ({i}){ext}");
            if (!File.Exists(dest)) return dest;
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = dir.Substring(source.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(source.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dest = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    public class DataFile
    {
        public AppSettings? Settings { get; set; }
        public List<HomeworkItem>? Items { get; set; }
    }
}
