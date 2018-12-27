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
        /// <summary>
        /// Initializes a new instance of the <see cref="BgzfStream"/> class using the specified stream and
        /// <see cref="CompressionMode"/> value, and a value that specifies whether to leave the stream open.
        /// </summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the <see cref="CompressionMode"/> values that indicates the action to take.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the stream open; otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        public BgzfStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Mode = mode;
            LeaveOpen = leaveOpen;
        }

        /// <inheritdoc cref="Stream.CanRead"/>
        public override bool CanRead => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.CanSeek"/>
        public override bool CanSeek => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.CanWrite"/>
        public override bool CanWrite => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.Length"/>
        public override long Length
        {
            [StringResourceUsage("NotSup.Stream")]
            get => throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        /// <inheritdoc cref="Stream.Position"/>
        public override long Position
        {
            [StringResourceUsage("NotSup.Stream")]
            get => throw new NotSupportedException(Res.GetString("NotSup.Stream"));

            [StringResourceUsage("NotSup.Stream")]
            set => throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        private Stream Stream { get; }

        private CompressionMode Mode { get; }

        private bool LeaveOpen { get; }

        /// <inheritdoc cref="Stream.Read"/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.Write"/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush() => throw new NotImplementedException();

        /// <inheritdoc cref="Stream.Seek"/>
        [StringResourceUsage("NotSup.Stream")]
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Res.GetString("NotSup.Stream"));
        }

        /// <inheritdoc cref="Stream.SetLength"/>
        [StringResourceUsage("NotSup.Stream")]
        public override void SetLength(long value) => throw new NotSupportedException(Res.GetString("NotSup.Stream"));

        /// <summary>
        /// Determines whether the specified file is in the BGZF format.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns>
        /// <see langword="true"/> if the specified file has the regular BGZF header; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsBgzf(string path)
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
                }
                return true;
            }
            catch (ArgumentException) { }
            catch (IOException) { }
            catch (NotSupportedException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }
    }
}
