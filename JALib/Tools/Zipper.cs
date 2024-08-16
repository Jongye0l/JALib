using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using JALib.Data;

namespace JALib.Tools;

public static class Zipper {

    public static RawFile[] Unzip(byte[] zipData) {
        using MemoryStream zipStream = new(zipData);
        return Unzip(zipStream);
    }

    public static RawFile[] Unzip(System.IO.Stream stream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
        List<RawFile> files = new();
        Dictionary<string, RawFile> folders = new();
        foreach(ZipArchiveEntry entry in archive.Entries) {
            byte[] buffer = new byte[entry.Length];
            entry.Open().Read(buffer);
            string[] path = entry.FullName.Split('/');
            if(path.Length > 1) {
                string folder = string.Join("/", path[..^1]);
                if(!folders.ContainsKey(folder)) {
                    RawFile parent = null;
                    for(int i = 0; i < path.Length - 1; i++) {
                        string folderPath = string.Join("/", path[..i]);
                        if(folders.TryGetValue(folderPath, out RawFile folder1)) {
                            parent = folder1;
                            continue;
                        }
                        RawFile rawFile = new(path[i], Array.Empty<RawFile>());
                        folders.Add(folderPath, rawFile);
                        files.Add(rawFile);
                        parent?.Files.Add(rawFile);
                        parent = rawFile;
                    }
                }
                folders[folder].Files.Add(new RawFile(path[^1], buffer));
            } else files.Add(new RawFile(entry.FullName, buffer));
        }
        return files.ToArray();
    }

    public static RawFile Unzip(string name, byte[] zipData) {
        return new RawFile(name, Unzip(zipData));
    }

    public static RawFile Unzip(string name, System.IO.Stream stream) {
        return new RawFile(name, Unzip(stream));
    }

    public static void Unzip(byte[] zipData, string path) {
        using MemoryStream zipStream = new(zipData);
        Unzip(zipStream, path);
    }

    public static void Unzip(System.IO.Stream stream, string path) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
        foreach(ZipArchiveEntry entry in archive.Entries) {
            string entryPath = Path.Combine(path, entry.FullName);
            if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
            else {
                using FileStream fileStream = new(entryPath, FileMode.Create);
                entry.Open().CopyTo(fileStream);
            }
        }
    }

    public static byte[] Zip(IEnumerable<RawFile> files) {
        using MemoryStream zipStream = new();
        Zip(files, zipStream);
        return zipStream.ToArray();
    }

    public static void Zip(IEnumerable<RawFile> files, System.IO.Stream stream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Create, false, Encoding.UTF8);
        foreach(RawFile file in files) WriteZip(file, archive, null);
    }

    public static byte[] Zip(RawFile file) {
        return file.IsFolder ? Zip(file.Files) : Zip(new[] { file });
    }

    private static void WriteZip(RawFile rawFile, ZipArchive archive, string folder) {
        string fileName = folder == null ? rawFile.Name : Path.Combine(folder, rawFile.Name);
        if(rawFile.IsFolder) {
            foreach(RawFile file in rawFile.Files) WriteZip(file, archive, fileName);
            return;
        }
        ZipArchiveEntry entry = archive.CreateEntry(fileName);
        using System.IO.Stream entryStream = entry.Open();
        entryStream.Write(rawFile.Data);
    }
}