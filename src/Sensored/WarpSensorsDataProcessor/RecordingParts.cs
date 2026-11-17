using System.Globalization;

using WarpToolkit.Sensors;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Lists finalized semicolon-CSV recording parts in the same order as
///  <see cref="SensorRecordingReader.ReadFolderAsync"/>.
/// </summary>
internal static class RecordingParts
{
    /// <summary>
    ///  Returns finalized <c>part-*.csv</c> paths in part-index order.
    /// </summary>
    /// <param name="recordingFolder">The recording folder.</param>
    internal static string[] ListSemicolonCsvFiles(string recordingFolder)
        =>
        [
            .. Directory
                .EnumerateFiles(recordingFolder, "part-*.csv", SearchOption.TopDirectoryOnly)
                .Where(static path => !path.Contains("--writing", StringComparison.Ordinal))
                .OrderBy(GetPartIndex)
                .ThenBy(static path => path, StringComparer.Ordinal)
        ];

    /// <summary>
    ///  Computes a SHA-256 hex digest of a file without loading it entirely into memory.
    /// </summary>
    /// <param name="path">The file to hash.</param>
    internal static string ComputeSha256(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            FileOptions.SequentialScan);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(stream);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    ///  Computes a deterministic combined hash from ordered part hashes.
    /// </summary>
    /// <param name="partHashes">Lines of <c>fileName:sha256</c> in part order.</param>
    internal static string ComputeCombinedSha256(IEnumerable<string> partHashes)
    {
        using System.Security.Cryptography.IncrementalHash hasher =
            System.Security.Cryptography.IncrementalHash.CreateHash(
                System.Security.Cryptography.HashAlgorithmName.SHA256);

        foreach (string line in partHashes)
        {
            hasher.AppendData(System.Text.Encoding.UTF8.GetBytes(line));
            hasher.AppendData([(byte)'\n']);
        }

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
    }

    private static long GetPartIndex(string path)
    {
        ReadOnlySpan<char> fileName = Path.GetFileName(path);
        const string Prefix = "part-";

        if (!fileName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return long.MaxValue;
        }

        ReadOnlySpan<char> indexText = fileName[Prefix.Length..];
        int separator = indexText.IndexOf('-');

        if (separator >= 0)
        {
            indexText = indexText[..separator];
        }

        return long.TryParse(
            indexText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long index)
                ? index
                : long.MaxValue;
    }
}
