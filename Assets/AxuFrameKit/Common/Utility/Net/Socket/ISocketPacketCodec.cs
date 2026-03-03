using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AxuFrameKit.Common.Utility.Net
{
    public interface ISocketPacketCodec
    {
        byte[] Encode(byte[] payload);
        Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken cancellationToken);
    }
}
