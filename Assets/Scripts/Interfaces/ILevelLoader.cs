using UnityEngine;

public interface ILevelLoader
{
    /// <summary>
    /// Parses the level data from a source file.
    /// </summary>
    void ReadFile();

    /// <summary>
    /// Instantiates the level objects based on the parsed data.
    /// </summary>
    void LoadLevel();
}
