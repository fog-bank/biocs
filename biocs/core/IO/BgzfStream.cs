using System;
using System.IO;
using System.IO.Compression;
using System.Security;

namespace Biocs.IO
{
    /// <summary>
    /// Provides access to streams in the BGZF compression format.
    /// </summary>
    public class BgzfStream : Stream
    {
        private Stream stream;
        private readonly CompressionMode mode;
        private readonly bool leaveOpen;
        private byte[] blockData;
        private DeflateStream deflateStream;
        private int inputLength;
        private bool eofMarker;

        /// <summary>
        /// Initializes a new instance of the <see cref="BgzfStream"/> class using the specified stream and
        /// <see cref="CompressionMode"/> value, and a value that specifies whether to leave the stream open.
        /// </summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the <see cref="CompressionMode"/> values that indicates the action to take.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the stream open; otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="mode"/> is not a valid <see cref="CompressionMode"/> enumeration value.
        /// </exception>
        [StringResourceUsage("Arg.InvalidEnumValue", 1)]
        public BgzfStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (mode != CompressionMode.Decompress && mode != CompressionMode.Compress)
                throw new ArgumentException(Res.GetString("Arg.InvalidEnumValue", nameof(CompressionMode)), nameof(mode));

            this.mode = mode;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => mode == CompressionMode.Decompress && stream != null && stream.CanRead;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => mode == CompressionMode.Compress && stream != null && stream.CanWrite;

        /// <summary>
        /// This property is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length
        {
            [StringResourceUsage("NotSup.Stream")]
            get => throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        /// <summary>
        /// This property is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position
        {
            [StringResourceUsage("NotSup.Stream")]
            get => throw new NotSupportedException(Res.GetString("NotSup.Stream"));

            [StringResourceUsage("NotSup.Stream")]
            set => throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        private byte[] Buffer
        {
            get
            {
                if (blockData == null)
                    blockData = new byte[0xffff - 25];

                return blockData;
            }
        }

        /// <summary>
        /// Reads a sequence of decompressed bytes from the underlying stream.
        /// </summary>
        /// <param name="buffer">An array of bytes used to store decompressed bytes.</param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer"/> at which to begin storing decompressed bytes.
        /// </param>
        /// <param name="count">The maximum number of decompressed bytes to be read.</param>
        /// <returns>
        /// The total number of decompressed bytes read into the buffer. This can be less than <paramref name="count"/> or zero
        /// if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.
        /// </exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
        [StringResourceUsage("Arg.InvalidBufferRange", 3)]
        [StringResourceUsage("NotSup.Stream")]
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset + count > buffer.Length)
                throw new ArgumentException(Res.GetString("Arg.InvalidBufferRange", offset, count, buffer.Length));

            if (stream == null)
                throw new ObjectDisposedException(GetType().Name);

            if (!CanRead)
                throw new NotSupportedException(Res.GetString("NotSup.Stream"));

            int totalRead = 0;

            while (count > 0)
            {
                if (deflateStream == null)
                {
                    if (!ReadBgzfBlock())
                        break;
                }

                int bytes = deflateStream.Read(buffer, offset, count);
                totalRead += bytes;
                inputLength -= bytes;
                offset += bytes;
                count -= bytes;

                if (inputLength <= 0)
                {
                    if (inputLength < 0 || deflateStream.ReadByte() != -1)
                    {
                        // Actual size is larger than expected size.
                        throw new InvalidDataException();
                    }

                    deflateStream.Dispose();
                    deflateStream = null;
                }
                else if (bytes == 0)
                {
                    // Actual size is smaller than expected size.
                    throw new InvalidDataException();
                }
            }
            return totalRead;
        }

        /// <summary>
        /// Writes a sequence of compressed bytes to the underlying stream.
        /// </summary>
        /// <param name="buffer">An array of bytes to compress.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin compressing.</param>
        /// <param name="count">The number of bytes to be compress.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
        [StringResourceUsage("Arg.InvalidBufferRange", 3)]
        [StringResourceUsage("NotSup.Stream")]
        public override void Write(byte[] buffer, int offset, int count)
        {
            const int MaxInputLength = 0xff00;

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset + count > buffer.Length)
                throw new ArgumentException(Res.GetString("Arg.InvalidBufferRange", offset, count, buffer.Length));

            if (stream == null)
                throw new ObjectDisposedException(GetType().Name);

            if (!CanWrite)
                throw new NotSupportedException(Res.GetString("NotSup.Stream"));

            while (count > 0)
            {
                if (deflateStream == null)
                    deflateStream = new DeflateStream(new MemoryStream(Buffer), CompressionMode.Compress, true);

                int capacity = MaxInputLength - inputLength;
                int length = Math.Min(count, capacity);

                deflateStream.Write(buffer, offset, length);
                // TODO: Catch NotSupportedException in capacity over.

                inputLength += length;
                capacity -= length;
                offset += length;
                count -= length;

                if (capacity == 0)
                    Flush();
            }
        }

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush()
        {
            if (CanWrite && inputLength > 0)
            {
                var baseStream = deflateStream.BaseStream;
                deflateStream.Dispose();
                WriteBgzfBlock((int)baseStream.Position);

                deflateStream = null;
                inputLength = 0;
            }
        }

        /// <summary>
        /// This method is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This method is not supported on this stream.</exception>
        [StringResourceUsage("NotSup.Stream")]
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        /// <summary>
        /// This method is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This method is not supported on this stream.</exception>
        [StringResourceUsage("NotSup.Stream")]
        public override void SetLength(long value) => throw new NotSupportedException(Res.GetString("NotSup.Stream"));

        /// <summary>
        /// Determines whether the specified file is in the BGZF format.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns>
        /// <see langword="true"/> if the specified file has the regular BGZF header; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsBgzfFile(string path)
        {
            const int HeaderSize = 16;

            if (path == null)
                return false;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, HeaderSize))
                {
                    var header = new byte[HeaderSize];
                    int length = stream.Read(header, 0, header.Length);

                    if (length != header.Length)
                        return false;

                    return IsBgzfHeader(header);
                }
            }
            catch (ArgumentException) { }
            catch (IOException) { }
            catch (NotSupportedException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        /// <inheritdoc cref="Stream.Dispose(bool)"/>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (CanWrite)
                {
                    Flush();

                    if (!eofMarker)
                    {
                        Buffer[0] = 3;
                        Buffer[1] = 0;
                        WriteBgzfBlock(2);
                        eofMarker = true;
                    }
                }
            }
            finally
            {
                try
                {
                    if (!leaveOpen)
                        stream?.Dispose();

                    deflateStream?.Dispose();
                    stream = null;
                    deflateStream = null;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        // Reads the header of next BGZF block and prepares a DeflateStream object that contains the compressed data.
        // @return true if next block was read successfully; false if there is no more block.
        // @exception InvalidDataException
        private bool ReadBgzfBlock()
        {
            var array = new byte[18];
            int bytes = stream.Read(array, 0, 18);

            if (bytes == 0)
                return false;

            if (bytes != 18 || !IsBgzfHeader(array))
                throw new InvalidDataException();

            int flag = array[3];

            // FNAME
            if ((flag & 0b1000) != 0)
            {
                do
                {
                    int value = stream.ReadByte();

                    if (value == 0)
                        break;

                    if (value == -1)
                        throw new InvalidDataException();
                }
                while (true);
            }

            // FCOMMENT
            if ((flag & 0b1_0000) != 0)
            {
                do
                {
                    int value = stream.ReadByte();

                    if (value == 0)
                        break;

                    if (value == -1)
                        throw new InvalidDataException();
                }
                while (true);
            }

            // FHCRC
            if ((flag & 0b10) != 0)
            {
                bytes = stream.Read(array, 0, 2);

                if (bytes != 2)
                    throw new InvalidDataException();
            }

            int dataLength = BitConverter.ToUInt16(array, 16) - 25;

            if (dataLength < 0)
                throw new InvalidDataException();

            bytes = stream.Read(Buffer, 0, dataLength);

            if (bytes != dataLength)
                throw new InvalidDataException();

            // Footer (CRC32 and ISIZE)
            bytes = stream.Read(array, 0, 8);

            if (bytes != 8)
                throw new InvalidDataException();

            inputLength = BitConverter.ToInt32(array, 4);

            if (inputLength == 0 && dataLength == 2 && Buffer[0] == 3 && Buffer[1] == 0)
            {
                dataLength = 0;
                eofMarker = true;
            }
            else
                eofMarker = false;

            deflateStream = new DeflateStream(new MemoryStream(Buffer, 0, dataLength), CompressionMode.Decompress);
            return true;
        }

        private void WriteBgzfBlock(int compressedLength)
        {
            var array = new byte[18];
            // ID
            array[0] = 0x1f;
            array[1] = 0x8b;
            // CM
            array[2] = 8;
            // FLG
            array[3] = 4;
            // OS
            array[9] = 0xff;
            // XLEN
            array[10] = 6;
            // SI
            array[12] = 66;
            array[13] = 67;
            // SLEN
            array[14] = 2;

            // BSIZE
            int bsize = compressedLength + 25;
            array[16] = (byte)(bsize & 0xff);
            array[17] = (byte)((bsize & 0xff00) >> 8);

            stream.Write(array, 0, array.Length);
            stream.Write(Buffer, 0, compressedLength);

            // TODO: CRC32
            if (inputLength > 0)
            {
                array[0] = 0;
                array[1] = 0;
                array[2] = 0;
                array[3] = 0;
            }

            // ISIZE
            array[4] = (byte)(inputLength & 0xff);
            array[5] = (byte)((inputLength & 0xff00) >> 8);
            array[6] = (byte)((inputLength & 0xff0000) >> 16);
            array[7] = (byte)((inputLength & 0xff000000) >> 24);

            stream.Write(array, 0, 8);
        }

        private static bool IsBgzfHeader(byte[] header)
        {
            if (header.Length < 16)
                return false;

            // Identification in gzip format
            if (header[0] != 0x1f || header[1] != 0x8b)
                return false;

            // Compression method is deflate.
            if (header[2] != 8)
                return false;

            // FEXTRA (bit 2) is set and any reserved bit is not set to flags.
            if ((header[3] & 4) == 0 || (header[3] & 0b1110_0000) != 0)
                return false;

            // The length of the optional extra field is 6.
            if (header[10] != 6 || header[11] != 0)
                return false;

            // Identification in BGZF format
            if (header[12] != 'B' || header[13] != 'C')
                return false;

            // The length of the BGZF extra field is 2.
            if (header[14] != 2 || header[15] != 0)
                return false;

            return true;
        }
    }
}
