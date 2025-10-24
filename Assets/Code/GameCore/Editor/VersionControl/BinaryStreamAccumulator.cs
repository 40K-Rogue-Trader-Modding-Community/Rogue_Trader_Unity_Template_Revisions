using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace Code.Framework.Editor.VersionControl
{
    public class BinaryStreamAccumulator
    {
        private readonly byte[] buffer = new byte[4096];
        private readonly List<byte> content = new(1024 * 1024);
        private readonly Stream stream;

        public BinaryStreamAccumulator(Stream stream)
        {
            this.stream = stream;
            this.stream.BeginRead(buffer, 0, buffer.Length, AccumulateReceived, null);
        }

        private void AccumulateReceived(IAsyncResult ar)
        {
            int bytesRead = stream.EndRead(ar);
            if (bytesRead == 0)
            {
                return;
            }

            for (int i = 0; i < bytesRead; i++)
            {
                content.Add(buffer[i]);
            }

            stream.BeginRead(buffer, 0, buffer.Length, AccumulateReceived, null);
        }

        public string GetString(Encoding encoding)
        {
            return encoding.GetString(content.ToArray());
        }
    }
}