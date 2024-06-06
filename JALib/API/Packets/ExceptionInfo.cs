using System;
using JALib.Core;
using JALib.Stream;
using JALib.Tools;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

namespace JALib.API.Packets;

internal class ExceptionInfo : RequestPacket {

    private readonly Exception exception;
    private readonly JAMod mod;

    public ExceptionInfo(JAMod mod, Exception exception) {
        this.mod = mod;
        this.exception = exception;
    }
    
    public override void ReceiveData(byte[] data) {
        ErrorUtils.ErrorInfo(data);
    }

    public override byte[] GetBinary() {
        using ByteArrayDataOutput output = new(JALib.Instance);
        Exception exception = this.exception;
        output.WriteUTF(mod.Name);
        output.WriteUTF(mod.ModEntry.Info.Version);
        output.WriteInt(GCNS.releaseNumber);
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