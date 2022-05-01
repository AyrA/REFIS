using System.IO;

namespace REFIS
{
    /// <summary>
    /// Generic tools
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Reads an index file and deserializes it
        /// </summary>
        /// <param name="Filename">Index file</param>
        /// <returns>Deserialized contents</returns>
        public static RefisIndex ReadIndexFile(string Filename)
        {
            return File.ReadAllText(Filename).FromJson<RefisIndex>();
        }

        /// <summary>
        /// Creates a file
        /// </summary>
        /// <param name="Name">File name</param>
        /// <param name="Overwrite">Permit overwriting of existing files</param>
        /// <returns>File stream</returns>
        /// <remarks>This is free of race conditions</remarks>
        public static Stream CreateFile(string Name, bool Overwrite)
        {
            return File.Open(Name, Overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
    }
}
