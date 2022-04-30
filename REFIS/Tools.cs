using System.IO;

namespace REFIS
{
    public static class Tools
    {
        public static RefisIndex ReadIndexFile(string Filename)
        {
            return File.ReadAllText(Filename).FromJson<RefisIndex>();
        }
    }
}
