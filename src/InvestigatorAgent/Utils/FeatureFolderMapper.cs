namespace InvestigatorAgent.Utils;

/// <summary>
/// A utility class to map feature folder names to their absolute paths within the incoming_data directory.
/// </summary>
public sealed class FeatureFolderMapper : IFeatureFolderMapper
{
    private readonly string _dataDirectory;

    public FeatureFolderMapper(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Data directory must be provided", nameof(dataDirectory));
        }

        // Handle relative paths by making them absolute to the current working directory
        if (!Path.IsPathRooted(dataDirectory))
        {
            _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), dataDirectory);
        }
        else
        {
            _dataDirectory = dataDirectory;
        }
    }

    /// <summary>
    /// Returns a dictionary mapping the feature folder name (e.g., "feature1") 
    /// to its absolute path on disk.
    /// </summary>
    public Dictionary<string, string> GetFeatureFolders()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_dataDirectory))
        {
            return mapping;
        }

        foreach (string dir in Directory.GetDirectories(_dataDirectory))
        {
            string folderName = new DirectoryInfo(dir).Name;
            mapping[folderName] = dir;
        }

        return mapping;
    }
}
