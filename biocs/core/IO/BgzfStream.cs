using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Security;

namespace Biocs.IO;

/// <summary>
/// Provides access to streams in the BGZF compression format.
/// </summary>
public class BgzfStream : Stream
{
    private Stream? stream;
    private readonly CompressionMode mode;
    private readonly CompressionLevel level;
    private readonly bool leaveOpen;
    private byte[]? blockData;
    private DeflateStream? deflateStream;
    private int inputLength;
    private uint crc;
    private uint originalCrc;
    private bool eofMarker;

    /// <summary>
    /// Initializes a new instance of the <see cref="BgzfStream"/> class with the specified stream and compression mode,
    /// and a value that specifies whether to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream to compress or decompress.</param>
    /// <param name="mode">One of the <see cref="CompressionMode"/> values that indicates the action to take.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="mode"/> is not a valid <see cref="CompressionMode"/> enumeration value.
    /// </exception>
    /// <remarks>
    /// The compression level is set to <see cref="CompressionLevel.Optimal"/> when the compression mode is
    /// <see cref="CompressionMode.Compress"/>.
    /// </remarks>
    [StringResourceUsage("Arg.InvalidEnumValue", 1)]
    public BgzfStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (mode != CompressionMode.Decompress && mode != CompressionMode.Compress)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidEnumValue", nameof(CompressionMode)), nameof(mode));

        this.stream = stream;
        this.mode = mode;
        this.leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BgzfStream"/> class with the specified stream and compression level,
    /// and a value that specifies whether to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream to compress.</param>
    /// <param name="level">
    /// One of the <see cref="CompressionLevel"/> values that indicates whether to emphasize speed or compression size.
    /// </param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="level"/> is not a valid <see cref="CompressionLevel"/> enumeration value.
    /// </exception>
    [StringResourceUsage("Arg.InvalidEnumValue", 1)]
    public BgzfStream(Stream stream, CompressionLevel level, bool leaveOpen = false)
        : this(stream, CompressionMode.Compress, leaveOpen)
    {
        if (level != CompressionLevel.Optimal && level != CompressionLevel.Fastest && level != CompressionLevel.NoCompression)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidEnumValue", nameof(CompressionLevel)), nameof(level));

        this.level = level;
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

    private byte[] CompressedData
    {
        get
        {
            blockData ??= new byte[0xffff - 25];
            return blockData;
        }
    }

    /// <summary>
    /// Reads a number of decompressed bytes from the underlying stream into the specified byte array.
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
    /// <para><paramref name="offset"/> or <paramref name="count"/> is negative.</para> -or-
    /// <para>The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the length of
    /// <paramref name="buffer"/>.</para>
    /// </exception>
    /// <exception cref="InvalidDataException">The stream data is in an invalid BGZF format.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
    /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        return ReadCore(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Reads a number of decompressed bytes from the underlying stream into the specified byte span.
    /// </summary>
    /// <param name="buffer">A region of memory.</param>
    /// <returns>
    /// The total number of decompressed bytes read into the buffer. This can be less than the length of
    /// <paramref name="buffer"/> or zero if the end of the stream has been reached.
    /// </returns>
    /// <exception cref="InvalidDataException">The stream data is in an invalid BGZF format.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
    /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
    public override int Read(Span<byte> buffer)
    {
        // The required minimum implementation in the derived class is Read(byte[], int, int).
        return GetType() != typeof(BgzfStream) ? base.Read(buffer) : ReadCore(buffer);
    }

    /// <summary>
    /// Writes a sequence of compressed bytes to the underlying stream.
    /// </summary>
    /// <param name="buffer">An array of bytes to compress.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin compressing.</param>
    /// <param name="count">The number of bytes to be compress.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="offset"/> or <paramref name="count"/> is negative.</para> -or-
    /// <para>The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the length of
    /// <paramref name="buffer"/>.</para>
    /// </exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">
    /// <para>The stream does not support writing.</para> -or-
    /// <para>The size of compressed bytes for a BGZF block exceeds about 64 KB.</para>
    /// </exception>
    /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        WriteCore(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Writes a sequence of compressed bytes to the underlying stream.
    /// </summary>
    /// <param name="buffer">A region of memory.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">
    /// <para>The stream does not support writing.</para> -or-
    /// <para>The size of compressed bytes for a BGZF block exceeds about 64 KB.</para>
    /// </exception>
    /// <exception cref="ObjectDisposedException">The method were called after the stream was closed.</exception>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        // The required minimum implementation in the derived class is Write(byte[], int, int).
        if (GetType() != typeof(BgzfStream))
            base.Write(buffer);
        else
            WriteCore(buffer);
    }

    /// <summary>
    /// Writes any buffered data to the underlying stream.
    /// </summary>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The size of compressed bytes for a BGZF block exceeds about 64 KB.</exception>
    [StringResourceUsage("NotSup.BlockSizeExceeded")]
    public override void Flush()
    {
        if (CanWrite && deflateStream != null)
        {
            var baseStream = deflateStream.BaseStream;
            try
            {
                // Purges buffers in DeflateStream.
                deflateStream.Dispose();
            }
            catch (NotSupportedException nse)
            {
                // The compressed size of a block exceeded the length of internal buffer.
                throw new NotSupportedException(Res.GetString("NotSup.BlockSizeExceeded"), nse);
            }
            WriteBgzfBlock((int)baseStream.Position);

            deflateStream = null;
            inputLength = 0;
            crc = 0;
        }
    }

    /// <summary>
    /// This method is not supported and always throws a <see cref="NotSupportedException"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This method is not supported on this stream.</exception>
    [StringResourceUsage("NotSup.Stream")]
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException(Res.GetString("NotSup.Stream"));

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
    public static bool IsBgzfFile(string? path)
    {
        const int HeaderSize = 16;

        if (path == null)
            return false;

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, HeaderSize);
            var header = (stackalloc byte[HeaderSize]);
            int length = TryReadExactBytes(stream, header);

            if (length != header.Length)
                return false;

            return IsBgzfHeader(header);
        }
        catch (ArgumentException) { }
        catch (IOException) { }
        catch (NotSupportedException) { }
        catch (SecurityException) { }
        catch (UnauthorizedAccessException) { }

        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (CanWrite)
            {
                Flush();

                if (!eofMarker)
                {
                    CompressedData[0] = 3;
                    CompressedData[1] = 0;
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
                blockData = null;
                deflateStream = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }

    // @exception InvalidDataException
    // @exception IOException
    // @exception NotSupportedException
    // @exception ObjectDisposedException
    [StringResourceUsage("InvalData.InvalidBlockSize")]
    [StringResourceUsage("InvalData.InvalidCrc")]
    [StringResourceUsage("NotSup.Stream")]
    private int ReadCore(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(stream == null, GetType().Name);

        if (!CanRead)
            throw new NotSupportedException(Res.GetString("NotSup.Stream"));

        int totalRead = 0;

        while (buffer.Length > 0)
        {
            // Reads a single BGZF block and creates a DeflateStream object to decompress.
            if (deflateStream == null)
            {
                deflateStream = ReadBgzfBlock();

                if (deflateStream == null)
                    break;
            }

            int bytes = deflateStream.Read(buffer);

            if (bytes == 0 && inputLength > 0)
            {
                // Actual size is smaller than expected size.
                throw new InvalidDataException(Res.GetString("InvalData.InvalidBlockSize"));
            }

            crc = Crc32.UpdateCrc(crc, buffer[..bytes]);
            totalRead += bytes;
            inputLength -= bytes;
            buffer = buffer[bytes..];

            // Check whether the BGZF block reaches the end.
            if (inputLength <= 0)
            {
                if (inputLength < 0 || deflateStream.ReadByte() != -1)
                {
                    // Actual size is larger than expected size.
                    throw new InvalidDataException(Res.GetString("InvalData.InvalidBlockSize"));
                }

                if (crc != originalCrc)
                    throw new InvalidDataException(Res.GetString("InvalData.InvalidCrc"));

                deflateStream.Dispose();
                deflateStream = null;
            }
        }
        return totalRead;
    }

    // Reads the header of next BGZF block and prepares a DeflateStream object that contains the compressed data.
    // @return true if next block was read successfully; false if there is no more block.
    // @exception InvalidDataException
    // @exception IOException
    [StringResourceUsage("InvalData.BadHeader")]
    [StringResourceUsage("InvalData.EndOfStream")]
    private DeflateStream? ReadBgzfBlock()
    {
        var buffer = (stackalloc byte[18]);
        int bytes = TryReadExactBytes(stream, buffer);

        if (bytes == 0)
            return null;

        if (bytes != 18)
            throw new InvalidDataException(Res.GetString("InvalData.EndOfStream"));

        if (!IsBgzfHeader(buffer))
            throw new InvalidDataException(Res.GetString("InvalData.BadHeader"));

        int flag = buffer[3];

        // FNAME
        if ((flag & 0b1000) != 0)
            SkipZeroTerminatedField();

        // FCOMMENT
        if ((flag & 0b1_0000) != 0)
            SkipZeroTerminatedField();

        // FHCRC
        if ((flag & 0b10) != 0)
            ReadExactBytes(buffer[..2]);

        // BSIZE in FEXTRA (total block size minus 1)
        int dataLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[16..]) - 25;

        if (dataLength < 0)
            throw new InvalidDataException(Res.GetString("InvalData.BadHeader"));

        // CDATA
        ReadExactBytes(CompressedData.AsSpan(..dataLength));

        // Footer (CRC32 and ISIZE)
        ReadExactBytes(buffer[..8]);

        if (buffer[6] > 1 || buffer[7] != 0)
            throw new InvalidDataException(Res.GetString("InvalData.BadHeader"));

        originalCrc = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        crc = 0;
        inputLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]);

        // EOF marker
        if (inputLength == 0 && dataLength == 2 && CompressedData[0] == 3 && CompressedData[1] == 0)
        {
            dataLength = 0;
            eofMarker = true;
        }
        else
            eofMarker = false;

        return new DeflateStream(new MemoryStream(CompressedData, 0, dataLength), CompressionMode.Decompress);
    }

    // Reads the current stream by the requested length exactly.
    // @param buffer An array of bytes used to store bytes.
    // @param count The number of bytes to read.
    // @exception InvalidDataException EOF has reached while reading.
    // @exception IOException
    [StringResourceUsage("InvalData.EndOfStream")]
    private void ReadExactBytes(Span<byte> buffer)
    {
        int bytes = TryReadExactBytes(stream, buffer);

        if (bytes != buffer.Length)
            throw new InvalidDataException(Res.GetString("InvalData.EndOfStream"));
    }

    // Reads the current stream until reaching a null character.
    // @exception InvalidDataException The end of the stream has reached while reading.
    // @exception IOException
    private void SkipZeroTerminatedField()
    {
        var buffer = (stackalloc byte[1]);
        do
        {
            ReadExactBytes(buffer);

            if (buffer[0] == 0)
                break;
        }
        while (true);
    }

    // @exception IOException
    // @exception NotSupportedException
    // @exception ObjectDisposedException
    [StringResourceUsage("NotSup.Stream")]
    [StringResourceUsage("NotSup.BlockSizeExceeded")]
    private void WriteCore(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(stream == null, GetType().Name);

        if (!CanWrite)
            throw new NotSupportedException(Res.GetString("NotSup.Stream"));

        const int MaxInputLength = 0xff00;

        while (buffer.Length > 0)
        {
            // Creates a stream to compress data.
            deflateStream ??= new DeflateStream(new MemoryStream(CompressedData), level, true);

            // Calculates # of bytes to be written to the DeflateStream.
            int maxBytesToWrite = MaxInputLength - inputLength;
            int length = Math.Min(buffer.Length, maxBytesToWrite);

            try
            {
                deflateStream.Write(buffer[..length]);
            }
            catch (NotSupportedException nse)
            {
                // The compressed size of a block exceeded the length of internal buffer (CompressedData).
                throw new NotSupportedException(Res.GetString("NotSup.BlockSizeExceeded"), nse);
            }

            crc = Crc32.UpdateCrc(crc, buffer[..length]);
            inputLength += length;
            maxBytesToWrite -= length;
            buffer = buffer[length..];

            if (maxBytesToWrite == 0)
                Flush();
        }
    }

    // Writes a header, compressed data with specified length, and a footer.
    // @param compressedLength The length of compressed data.
    // @exception IOException
    private void WriteBgzfBlock(int compressedLength)
    {
        if (stream == null)
            return;

        var buffer = (stackalloc byte[18]);
        // ID
        buffer[0] = 0x1f;
        buffer[1] = 0x8b;
        // CM
        buffer[2] = 8;
        // FLG
        buffer[3] = 4;
        // OS
        buffer[9] = 0xff;
        // XLEN
        buffer[10] = 6;
        // SI
        buffer[12] = 66;
        buffer[13] = 67;
        // SLEN
        buffer[14] = 2;

        // BSIZE (total block size minus 1)
        int bsize = compressedLength + 25;
        Debug.Assert(bsize < 0x10000);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[16..], (ushort)bsize);

        stream.Write(buffer);
        stream.Write(CompressedData, 0, compressedLength);

        if (inputLength > 0)
        {
            // CRC32
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, crc);

            // ISIZE
            Debug.Assert(inputLength < 0x10000);
            BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], inputLength);
        }
        else
            buffer[..4].Clear();

        stream.Write(buffer[..8]);
    }

    // Reads a stream by the requested length exactly.
    // @param stream The stream to read.
    // @param buffer An array of bytes used to store bytes.
    // @return The total number of bytes read into the buffer. If it is less than count, the end of the stream has reached.
    // @exception IOException
    private static int TryReadExactBytes(Stream? stream, Span<byte> buffer)
    {
        if (stream == null)
            return 0;

        int totalBytes = 0;

        while (buffer.Length > 0)
        {
            // An implementation of Stream.Read method is free to return fewer bytes than requested
            // even if the end of the stream has not reached.
            int bytes = stream.Read(buffer);

            if (bytes == 0)
                break;

            buffer = buffer[bytes..];
            totalBytes += bytes;
        }
        return totalBytes;
    }

    private static bool IsBgzfHeader(ReadOnlySpan<byte> header)
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
