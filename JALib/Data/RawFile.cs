using System;
using System.IO;

namespace JALib.Data;

public class RawFile : IDisposable {
    public string Name { get; private set; }
    public byte[] Data { get; private set; }
    public RawFile[] Files { get; private set; }
    public bool IsFolder => Data == null;
    
    public RawFile(string name, byte[] data) {
        Name = name;
        Data = data;
    }

    public RawFile(string filePath) {
        Name = Path.GetFileName(filePath);
        if(File.Exists(filePath)) {
            Data = File.ReadAllBytes(filePath);
            return;
        }
        if(!Directory.Exists(filePath)) throw new FileNotFoundException();
        string[] paths = Directory.GetFiles(filePath);
        Files = new RawFile[paths.Length];
        for(int i = 0; i < paths.Length; i++) Files[i] = new RawFile(paths[i]);
    }
    
    public RawFile(string name, RawFile[] files) {
        Name = name;
        Files = files;
    }
    
    public void Save(string path) {
        path = Path.Combine(path, Name);
        if(IsFolder) {;
            Directory.CreateDirectory(path);
            foreach(RawFile file in Files) file.Save(path);
            return;
        }
        File.Create(path);
        File.WriteAllBytes(path, Data);
    }

    public void Dispose() {
        Name = null;
        Data = null;
        if(Files == null) return;
        foreach(RawFile file in Files) file.Dispose();
        Files = null;
    }
}