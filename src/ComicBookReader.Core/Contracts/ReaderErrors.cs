namespace ComicBookReader.Core.Contracts;

public enum ReaderErrorCode
{
    FileUnavailable, InvalidArchive, UnsupportedArchive, NoImages,
    PageDecodeFailed, ResourceLimit, StorageUnavailable
}

public sealed class ReaderException(ReaderErrorCode code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public ReaderErrorCode Code { get; } = code;
}
