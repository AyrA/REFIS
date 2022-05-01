using System.IO;

namespace REFIS
{
    public static class Tools
    {
        public static RefisIndex ReadIndexFile(string Filename)
        {
            return File.ReadAllText(Filename).FromJson<RefisIndex>();
        }

        public static Stream CreateFile(string Name, bool Overwrite)
        {
            return File.Open(Name, Overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
    }
}
