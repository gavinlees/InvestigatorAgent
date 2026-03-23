using System.Collections.Generic;

namespace InvestigatorAgent.Utils;

/// <summary>
/// Interface for mapping feature folder names to their paths.
/// </summary>
public interface IFeatureFolderMapper
{
    /// <summary>
    /// Returns a dictionary mapping the feature folder name (e.g., "feature1") 
    /// to its absolute path on disk.
    /// </summary>
    Dictionary<string, string> GetFeatureFolders();
}
