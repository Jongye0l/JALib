using System;
using JALib.Core;
using JALib.Stream;
using JALib.Tools;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

namespace JALib.API.Packets;

internal class ExceptionReport : RequestPacket {

    private readonly Exception exception;
    private readonly JAMod mod;
    
    public ExceptionReport(JAMod mod, Exception exception) {
        this.mod = mod;
        this.exception = exception;
    }
    
    public override void ReceiveData(byte[] data) {
        using ByteArrayDataInput input = new(data);
        if(input.ReadBoolean()) ErrorUtils.ReportComplete(input.ReadInt(), input.ReadUTF());
        else ErrorUtils.ReportFail(input.ReadInt());
    }

    public override byte[] GetBinary() {
        using ByteArrayDataOutput output = new(JALib.Instance);
        output.WriteUTF(mod.Name);
        output.WriteUTF(mod.Version.ToString());
        output.WriteUTF(mod.ModEntry.Info.Version);
        output.WriteUTF(SystemInfo.deviceModel);
        output.WriteUTF(SystemInfo.operatingSystem);
        output.WriteUTF(ADOBase.sceneName);
        Exception exception = this.exception;
        output.WriteInt(exception.GetHashCode());
        while(exception != null) {
            output.WriteUTF(exception.GetType().FullName);
            output.WriteUTF(exception.Message);
            output.WriteUTF(exception.Source);
            output.WriteUTF(exception.StackTrace);
            output.WriteBoolean(exception.InnerException == null);
            exception = exception.InnerException;
        }
        return output.ToByteArray();
    }
}