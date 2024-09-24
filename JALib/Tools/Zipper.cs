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

    public static RawFile[] Unzip(Stream stream) {
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

    public static RawFile Unzip(string name, Stream stream) {
        return new RawFile(name, Unzip(stream));
    }

    public static void Unzip(byte[] zipData, string path) {
        using MemoryStream zipStream = new(zipData);
        Unzip(zipStream, path);
    }

    public static void Unzip(Stream stream, string path) {
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

    public static void Zip(IEnumerable<RawFile> files, Stream stream) {
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
        using Stream entryStream = entry.Open();
        entryStream.Write(rawFile.Data);
    }

    public static byte[] Gunzip(byte[] gzipData) {
        using MemoryStream gzipStream = new(gzipData);
        return Gunzip(gzipStream);
    }

    public static byte[] Gunzip(Stream stream) {
        using GZipStream gzipStream = new(stream, CompressionMode.Decompress);
        using MemoryStream memoryStream = GunzipToMemoryStream(stream);
        return memoryStream.ToArray();
    }

    public static MemoryStream GunzipToMemoryStream(byte[] gzipData) {
        using MemoryStream gzipStream = new(gzipData);
        return GunzipToMemoryStream(gzipStream);
    }

    public static MemoryStream GunzipToMemoryStream(Stream stream) {
        MemoryStream memoryStream = new();
        stream.Gunzip(memoryStream);
        return memoryStream;
    }

    public static void Gunzip(this Stream stream, Stream input) {
        using GZipStream gzipStream = new(stream, CompressionMode.Decompress);
        gzipStream.CopyTo(input);
    }

    public static void Gunzip(this Stream stream, byte[] gzipData) {
        using MemoryStream memoryStream = new(gzipData);
        Gunzip(stream, memoryStream);
    }

    public static void Gunzip(byte[] gzipData, string path) {
        using MemoryStream gzipStream = new(gzipData);
        Gunzip(gzipStream, path);
    }

    public static void Gunzip(Stream stream, string path) {
        using GZipStream gzipStream = new(stream, CompressionMode.Decompress);
        using FileStream fileStream = new(path, FileMode.Create);
        gzipStream.CopyTo(fileStream);
    }

    public static byte[] Gzip(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using MemoryStream memoryStream = GzipToMemoryStream(data, compressionLevel);
        return memoryStream.ToArray();
    }

    public static byte[] Gzip(Stream stream, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using MemoryStream memoryStream = GzipToMemoryStream(stream, compressionLevel);
        return memoryStream.ToArray();
    }

    public static MemoryStream GzipToMemoryStream(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        MemoryStream memoryStream = new();
        memoryStream.Gzip(data, compressionLevel);
        return memoryStream;
    }

    public static MemoryStream GzipToMemoryStream(Stream stream, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        MemoryStream memoryStream = new();
        memoryStream.Gzip(stream, compressionLevel);
        return memoryStream;
    }

    public static void Gzip(this Stream stream, Stream input, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using GZipStream gzipStream = new(stream, compressionLevel, true);
        input.CopyTo(gzipStream);
    }

    public static void Gzip(this Stream stream, byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using GZipStream gzipStream = new(stream, compressionLevel, true);
        gzipStream.Write(data);
    }

    public static byte[] UnDeflate(byte[] deflateData) {
        using MemoryStream deflateStream = new(deflateData);
        return UnDeflate(deflateStream);
    }

    public static byte[] UnDeflate(Stream stream) {
        using DeflateStream deflateStream = new(stream, CompressionMode.Decompress);
        using MemoryStream memoryStream = UnDeflateToMemoryStream(stream);
        return memoryStream.ToArray();
    }

    public static MemoryStream UnDeflateToMemoryStream(byte[] deflateData) {
        using MemoryStream deflateStream = new(deflateData);
        return UnDeflateToMemoryStream(deflateStream);
    }

    public static MemoryStream UnDeflateToMemoryStream(Stream stream) {
        using DeflateStream deflateStream = new(stream, CompressionMode.Decompress);
        MemoryStream memoryStream = new();
        deflateStream.CopyTo(memoryStream);
        return memoryStream;
    }

    public static byte[] Deflate(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using MemoryStream memoryStream = DeflateToMemoryStream(data, compressionLevel);
        return memoryStream.ToArray();
    }

    public static byte[] Deflate(Stream stream, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using MemoryStream memoryStream = DeflateToMemoryStream(stream, compressionLevel);
        return memoryStream.ToArray();
    }

    public static MemoryStream DeflateToMemoryStream(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        MemoryStream memoryStream = new();
        using DeflateStream deflateStream = new(memoryStream, compressionLevel, true);
        deflateStream.Write(data);
        return memoryStream;
    }

    public static MemoryStream DeflateToMemoryStream(Stream stream, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        MemoryStream memoryStream = new();
        using DeflateStream deflateStream = new(memoryStream, compressionLevel, true);
        stream.CopyTo(deflateStream);
        return memoryStream;
    }
}