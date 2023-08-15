namespace PeaceProcessor.Functions
{
    // Wraps another Stream instance to provide a continuous stream of data.
    public class ContinuousStream : Stream
    {
        private readonly Stream innerStream;

        public ContinuousStream(Stream innerStream)
        {
            this.innerStream = innerStream;
        }

        // Reads from the wrapped Stream instance and returns the number of bytes read.
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (count > 0)
            {
                int bytesRead = this.innerStream.Read(buffer, offset, count);
                if (bytesRead == 0)
                    break;

                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }

            return totalBytesRead;
        }

        public override bool CanRead => this.innerStream.CanRead;
        public override bool CanSeek => this.innerStream.CanSeek;
        public override bool CanWrite => this.innerStream.CanWrite;
        public override long Length => this.innerStream.Length;
        public override long Position { get => this.innerStream.Position; set => this.innerStream.Position = value; }
        public override void Flush() => this.innerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => this.innerStream.Seek(offset, origin);
        public override void SetLength(long value) => this.innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => this.innerStream.Write(buffer, offset, count);
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.innerStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
