namespace RTWServer.ServerCore.Interface;

public interface IPacketSerializer
{
    int GetHeaderSize();
    int GetPayloadSize(byte[] header);
    byte[] Serialize(IPacket packet);
    IPacket Deserialize(byte[] data);
}