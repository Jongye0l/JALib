using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using JALib.Data;

namespace JALib.Tools;

public static class Zipper {
    public static readonly Encoding Encoding = Encoding.GetEncoding(949);

    public static RawFile[] Unzip(byte[] zipData) {
        using MemoryStream zipStream = new(zipData);
        return Unzip(zipStream);
    }

    public static RawFile[] Unzip(Stream stream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding);
        List<RawFile> files = [];
        Dictionary<string, RawFile> folders = new();
        foreach(ZipArchiveEntry entry in archive.Entries) {
            byte[] buffer = new byte[entry.Length];
            using(Stream st = entry.Open()) st.Read(buffer, 0, buffer.Length);
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

    public static RawFile[] Unzip(byte[] zipData, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using MemoryStream zipStream = new(zipData);
        return Unzip(zipStream, nameChanger, dataChanger);
    }

    public static RawFile[] Unzip(Stream stream, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding);
        List<RawFile> files = [];
        Dictionary<string, RawFile> folders = new();
        foreach(ZipArchiveEntry entry in archive.Entries) {
            byte[] buffer = new byte[entry.Length];
            using(Stream st = entry.Open()) st.Read(buffer, 0, buffer.Length);
            buffer = dataChanger(buffer);
            string[] path = nameChanger(entry.FullName).Split('/');
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
            } else files.Add(new RawFile(nameChanger(entry.FullName), buffer));
        }
        return files.ToArray();
    }

    public static RawFile Unzip(string name, byte[] zipData) => new(name, Unzip(zipData));

    public static RawFile Unzip(string name, Stream stream) => new(name, Unzip(stream));

    public static RawFile Unzip(string name, byte[] zipData, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) => new(name, Unzip(zipData, nameChanger, dataChanger));

    public static RawFile Unzip(string name, Stream stream, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) => new(name, Unzip(stream, nameChanger, dataChanger));

    public static void Unzip(byte[] zipData, string path) {
        using MemoryStream zipStream = new(zipData);
        Unzip(zipStream, path);
    }

    public static void Unzip(Stream stream, string path) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding);
        foreach(ZipArchiveEntry entry in archive.Entries) {
            string entryPath = Path.Combine(path, entry.FullName);
            if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
            else {
                string directory = Path.GetDirectoryName(entryPath);
                if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using FileStream fileStream = File.Exists(entryPath) ? new FileStream(entryPath, FileMode.Open, FileAccess.Write, FileShare.None) : new FileStream(entryPath, FileMode.Create);
                using Stream st = entry.Open();
                st.CopyTo(fileStream);
            }
        }
    }

    public static void Unzip(byte[] zipData, string path, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using MemoryStream zipStream = new(zipData);
        Unzip(zipStream, path, nameChanger, dataChanger);
    }

    public static void Unzip(Stream stream, string path, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding);
        foreach(ZipArchiveEntry entry in archive.Entries) {
            string entryPath = Path.Combine(path, nameChanger(entry.FullName));
            if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
            else {
                string directory = Path.GetDirectoryName(entryPath);
                if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using FileStream fileStream = File.Exists(entryPath) ? new FileStream(entryPath, FileMode.Open, FileAccess.Write, FileShare.None) : new FileStream(entryPath, FileMode.Create);
                byte[] buffer = new byte[entry.Length];
                using(Stream st = entry.Open()) st.Read(buffer, 0, buffer.Length);
                buffer = dataChanger(buffer);
                fileStream.Write(buffer, 0, buffer.Length);
            }
        }
    }

    public static void Unzip(byte[] zipData, string path, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        using MemoryStream zipStream = new(zipData);
        Unzip(zipStream, path, nameChanger, writeStream, readStream);
    }

    public static void Unzip(Stream stream, string path, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding);
        foreach(ZipArchiveEntry entry in archive.Entries) {
            string entryPath = Path.Combine(path, nameChanger(entry.FullName));
            if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
            else {
                string directory = Path.GetDirectoryName(entryPath);
                if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using FileStream fileStream = File.Exists(entryPath) ? new FileStream(entryPath, FileMode.Open, FileAccess.Write, FileShare.None) : new FileStream(entryPath, FileMode.Create);
                using Stream st = entry.Open();
                st.CopyTo(writeStream);
                readStream.CopyTo(fileStream);
            }
        }
    }

    public static void Unzip(string zipPath, string path) {
        using FileStream fileStream = new(zipPath, FileMode.Open);
        Unzip(fileStream, path);
    }

    public static void Unzip(string zipPath, string path, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using FileStream fileStream = new(zipPath, FileMode.Open);
        Unzip(fileStream, path, nameChanger, dataChanger);
    }

    public static void Unzip(string zipPath, string path, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        using FileStream fileStream = new(zipPath, FileMode.Open);
        Unzip(fileStream, path, nameChanger, writeStream, readStream);
    }

    public static byte[] Zip(IEnumerable<RawFile> files) {
        using MemoryStream zipStream = new();
        Zip(files, zipStream);
        return zipStream.ToArray();
    }

    public static void Zip(IEnumerable<RawFile> files, Stream stream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Create, false, Encoding);
        foreach(RawFile file in files) WriteZip(file, archive, null);
    }

    public static byte[] Zip(IEnumerable<RawFile> files, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using MemoryStream zipStream = new();
        Zip(files, zipStream, nameChanger, dataChanger);
        return zipStream.ToArray();
    }

    public static byte[] Zip(IEnumerable<RawFile> files, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        using MemoryStream zipStream = new();
        Zip(files, zipStream, nameChanger, writeStream, readStream);
        return zipStream.ToArray();
    }

    public static void Zip(IEnumerable<RawFile> files, Stream stream, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Create, false, Encoding);
        foreach(RawFile file in files) WriteZip(file, archive, null, nameChanger, dataChanger);
    }

    public static void Zip(IEnumerable<RawFile> files, Stream stream, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        using ZipArchive archive = new(stream, ZipArchiveMode.Create, false, Encoding);
        foreach(RawFile file in files) WriteZip(file, archive, null, nameChanger, writeStream, readStream);
    }

    public static byte[] Zip(RawFile file) {
        return file.IsFolder ? Zip(file.Files) : Zip([file]);
    }

    public static byte[] Zip(RawFile file, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        return file.IsFolder ? Zip(file.Files, nameChanger, dataChanger) : Zip([file], nameChanger, dataChanger);
    }

    public static byte[] Zip(RawFile file, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        return file.IsFolder ? Zip(file.Files, nameChanger, writeStream, readStream) : Zip([file], nameChanger, writeStream, readStream);
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

    private static void WriteZip(RawFile rawFile, ZipArchive archive, string folder, Func<string, string> nameChanger, Func<byte[], byte[]> dataChanger) {
        string fileName = folder == null ? rawFile.Name : Path.Combine(folder, rawFile.Name);
        if(rawFile.IsFolder) {
            foreach(RawFile file in rawFile.Files) WriteZip(file, archive, fileName);
            return;
        }
        ZipArchiveEntry entry = archive.CreateEntry(nameChanger(fileName));
        using Stream entryStream = entry.Open();
        entryStream.Write(dataChanger(rawFile.Data));
    }

    private static void WriteZip(RawFile rawFile, ZipArchive archive, string folder, Func<string, string> nameChanger, Stream writeStream, Stream readStream) {
        string fileName = folder == null ? rawFile.Name : Path.Combine(folder, rawFile.Name);
        if(rawFile.IsFolder) {
            foreach(RawFile file in rawFile.Files) WriteZip(file, archive, fileName);
            return;
        }
        ZipArchiveEntry entry = archive.CreateEntry(nameChanger(fileName));
        using Stream entryStream = entry.Open();
        writeStream.Write(rawFile.Data, 0, rawFile.Data.Length);
        readStream.CopyTo(entryStream);
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
        MemoryStream memoryStream = new();
        stream.UnDeflate(memoryStream);
        return memoryStream;
    }

    public static void UnDeflate(this Stream stream, Stream input) {
        using DeflateStream deflateStream = new(stream, CompressionMode.Decompress);
        deflateStream.CopyTo(input);
    }

    public static void UnDeflate(this Stream stream, byte[] deflateData) {
        using MemoryStream memoryStream = new(deflateData);
        UnDeflate(stream, memoryStream);
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
        stream.Deflate(memoryStream, compressionLevel);
        return memoryStream;
    }

    public static void Deflate(this Stream stream, Stream input, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using DeflateStream deflateStream = new(stream, compressionLevel, true);
        input.CopyTo(deflateStream);
    }

    public static void Deflate(this Stream stream, byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        using MemoryStream memoryStream = new(data);
        Deflate(stream, memoryStream, compressionLevel);
    }
}
