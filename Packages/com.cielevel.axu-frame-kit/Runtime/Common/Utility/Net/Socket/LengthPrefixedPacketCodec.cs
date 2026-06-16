using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AxuFrameKit.Common.Utility.Net
{
    public sealed class LengthPrefixedPacketCodec : ISocketPacketCodec
    {
        private readonly int _maxPacketSize;

        public LengthPrefixedPacketCodec(int maxPacketSize = 2 * 1024 * 1024)
        {
            if (maxPacketSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPacketSize));

            _maxPacketSize = maxPacketSize;
        }

        public byte[] Encode(byte[] payload)
        {
            payload ??= Array.Empty<byte>();

            if (payload.Length > _maxPacketSize)
                throw new InvalidOperationException($"Payload too large: {payload.Length} bytes");

            var frame = new byte[payload.Length + 4];
            int length = payload.Length;

            frame[0] = (byte)((length >> 24) & 0xFF);
            frame[1] = (byte)((length >> 16) & 0xFF);
            frame[2] = (byte)((length >> 8) & 0xFF);
            frame[3] = (byte)(length & 0xFF);

            if (payload.Length > 0)
                Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);

            return frame;
        }

        public async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            byte[] header = ArrayPool<byte>.Shared.Rent(4);

            try
            {
                bool headerRead = await ReadExactlyAsync(stream, header, 4, cancellationToken).ConfigureAwait(false);
                if (!headerRead)
                    return null;

                int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                if (length < 0 || length > _maxPacketSize)
                    throw new InvalidDataException($"Invalid packet length: {length}");

                if (length == 0)
                    return Array.Empty<byte>();

                byte[] body = new byte[length];
                bool bodyRead = await ReadExactlyAsync(stream, body, length, cancellationToken).ConfigureAwait(false);
                return bodyRead ? body : null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(header);
            }
        }

        private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, int size, CancellationToken cancellationToken)
        {
            int offset = 0;

            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset, cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                    return false;

                offset += read;
            }

            return true;
        }
    }
}
