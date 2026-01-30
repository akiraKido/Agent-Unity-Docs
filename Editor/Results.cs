namespace UnityDocsIndex.Editor
{
    /// <summary>
    /// Result of documentation download operation
    /// </summary>
    public enum DownloadResult
    {
        Success,
        ErrorInvalidVersion,
        ErrorNetworkFailure,
        ErrorExtractionFailed,
        ErrorCancelled
    }

    /// <summary>
    /// Result of local documentation copy operation
    /// </summary>
    public enum CopyResult
    {
        Success,
        ErrorSourcePathEmpty,
        ErrorSourceNotFound,
        ErrorCopyFailed
    }

    /// <summary>
    /// Result of index generation operation
    /// </summary>
    public enum GenerateResult
    {
        Success,
        ErrorManualPathNotFound,
        ErrorScriptRefPathNotFound,
        ErrorNoDocumentationFound,
        ErrorRootPathEmpty
    }

    /// <summary>
    /// Result of file injection operation
    /// </summary>
    public enum InjectResult
    {
        SuccessCreated,
        SuccessUpdated,
        SuccessAppended,
        ErrorFilePathEmpty,
        ErrorFileNotFound,
        ErrorWriteFailed
    }

    /// <summary>
    /// Result of gitignore update operation
    /// </summary>
    public enum GitignoreResult
    {
        SuccessCreated,
        SuccessUpdated,
        AlreadyPresent,
        ErrorPathEmpty,
        ErrorWriteFailed
    }
}
