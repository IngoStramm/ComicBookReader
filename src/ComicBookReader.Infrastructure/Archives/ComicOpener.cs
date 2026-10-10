using System.Buffers.Binary;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;

namespace ComicBookReader.Infrastructure.Archives;

public sealed class ComicOpener(SkiaPageDecoder? decoder = null) : IComicOpener
{
    private readonly SkiaPageDecoder decoder = decoder ?? new SkiaPageDecoder();

    public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
        IProgress<ComicOpenProgress>? progress = null) => Task.Run<IComicSession>(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        FileStream? file = null;
        try
        {
            file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> signature = stackalloc byte[8];
            var count = file.Read(signature);
            file.Position = 0;
            if (count >= 7 && signature[..7].SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00 }) ||
                count >= 8 && signature.SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00 }))
                throw new ReaderException(ReaderErrorCode.UnsupportedArchive, "O suporte a arquivos RAR ainda não está disponível.");
            if (count < 4 || signature[0] != 0x50 || signature[1] != 0x4b ||
                !((signature[2] == 3 && signature[3] == 4) || (signature[2] == 5 && signature[3] == 6) || (signature[2] == 7 && signature[3] == 8)))
                throw new ReaderException(ReaderErrorCode.InvalidArchive, "O arquivo não é uma HQ ZIP válida.");
            CheckUnsupportedZip(file, cancellationToken);
            file.Position = 0;
            cancellationToken.ThrowIfCancellationRequested();
            var session = new ZipComicSession(file, decoder);
            file = null;
            if (cancellationToken.IsCancellationRequested)
            {
                session.DisposeAsync().AsTask().GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return session;
        }
        catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException)
        {
            throw new ReaderException(ReaderErrorCode.InvalidArchive, "O arquivo ZIP está danificado ou incompleto.", exception);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ReaderException(ReaderErrorCode.FileUnavailable, "Não foi possível acessar o arquivo da HQ.", exception);
        }
        finally
        {
            file?.Dispose();
        }
    }, cancellationToken);

    private static void CheckUnsupportedZip(FileStream file, CancellationToken cancellationToken)
    {
        // ZIP flags are absent from ZipArchiveEntry's public contract. Read only directory headers.
        var tail = new byte[(int)Math.Min(file.Length, 65_557)];
        file.Position = file.Length - tail.Length;
        file.ReadExactly(tail);
        Span<byte> header = stackalloc byte[46];
        for (var offset = tail.Length - 22; offset >= 0; offset--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var end = tail.AsSpan(offset);
            if (BinaryPrimitives.ReadUInt32LittleEndian(end) != 0x06054b50 ||
                offset + 22 + BinaryPrimitives.ReadUInt16LittleEndian(end[20..]) != tail.Length) continue;
            if (BinaryPrimitives.ReadUInt16LittleEndian(end[4..]) != 0 || BinaryPrimitives.ReadUInt16LittleEndian(end[6..]) != 0 ||
                BinaryPrimitives.ReadUInt16LittleEndian(end[8..]) != BinaryPrimitives.ReadUInt16LittleEndian(end[10..]))
                throw UnsupportedZip();
            var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(end[10..]);
            var directoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(end[16..]);
            // ZIP64 is handled by ZipArchive; sentinel offsets cannot be traversed as ZIP32.
            if (directoryOffset == uint.MaxValue || entryCount == ushort.MaxValue) return;
            file.Position = directoryOffset;
            for (var entry = 0; entry < entryCount; entry++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                file.ReadExactly(header);
                if (BinaryPrimitives.ReadUInt32LittleEndian(header) != 0x02014b50)
                    throw new InvalidDataException("Invalid ZIP directory header.");
                if ((BinaryPrimitives.ReadUInt16LittleEndian(header[8..]) & 0x0041) != 0 ||
                    BinaryPrimitives.ReadUInt16LittleEndian(header[34..]) != 0) throw UnsupportedZip();
                var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(header[28..]) +
                    BinaryPrimitives.ReadUInt16LittleEndian(header[30..]) + BinaryPrimitives.ReadUInt16LittleEndian(header[32..]);
                if (extraLength > file.Length - file.Position) throw new InvalidDataException("Invalid ZIP directory length.");
                file.Position += extraLength;
            }
            return;
        }
        throw new InvalidDataException("Missing ZIP directory.");
    }

    private static ReaderException UnsupportedZip() => new(ReaderErrorCode.UnsupportedArchive,
        "Arquivos ZIP com senha ou múltiplos volumes não são suportados.");
}
