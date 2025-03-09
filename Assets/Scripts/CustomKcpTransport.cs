using kcp2k;
using Mirror;
using UnityEngine;

public class CustomKcpTransport : KcpTransport
{
    [Header("Packet Size Settings")]
    [Tooltip("Maximum packet size in bytes")]
    public int MaxPacketSize = 4096; // Increase this value as needed

    // Override the GetMaxPacketSize method to use our custom value
    public override int GetMaxPacketSize(int channelId = Channels.DefaultReliable)
    {
        return MaxPacketSize;
    }
}
