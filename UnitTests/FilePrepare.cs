using System;
using System.IO;
using System.Linq;
using REFIS;

namespace UnitTests
{
    public static class FilePrepare
    {
        private static readonly object sync = new object();
        private static readonly Random R = new Random();
        public struct FileNames
        {
            public const string EMPTY = "empty.bin";
            public const string SMALL = "small.bin";
            public const string EXACT = "exact.bin";
            public const string BIG = "big.bin";

            public static string[] GetAll()
            {
                return new string[]
                {
                    EMPTY, SMALL, EXACT, BIG
                };
            }

            public static string GetCombined()
            {
                return "combined.raw";
            }

            public static string GetRandom()
            {
                return "random.raw";
            }
        }

        private static string TempDir = null;

        public static string GetTempDir()
        {
            lock (sync)
            {
                if (TempDir == null)
                {
                    string CompleteDir;
                    int i = 0;
                    var Temp = Path.GetTempPath();
                    do
                    {
                        CompleteDir = Path.Combine(Temp, $"UnitTests_{++i}");
                    } while (Directory.Exists(CompleteDir));
                    //Consider printing a warning if i>1 because this indicates a failed test run
                    TempDir = Directory.CreateDirectory(CompleteDir).FullName;
                }
                return TempDir;
            }
        }

        public static string Prepare()
        {
            var Root = GetTempDir();
            //Random content for files
            var Buffer = new byte[RefisHeader.BLOCK_SIZE * 100];
            R.NextBytes(Buffer);
            //Ensures that there is never a chance to accidentally generate a valid header at the start
            Buffer[0] = 0xFF;
            CreateFileAndEncode(Path.Combine(Root, FileNames.EMPTY), new byte[0]);
            CreateFileAndEncode(Path.Combine(Root, FileNames.SMALL), Buffer.Take(20).ToArray());
            CreateFileAndEncode(Path.Combine(Root, FileNames.BIG), Buffer);
            CreateFileAndEncode(Path.Combine(Root, FileNames.EXACT), Buffer.Take(RefisHeader.DATA_SIZE * 2).ToArray());
            //Create combined file for search command
            using (var Combined = File.Create(Path.Combine(Root, FileNames.GetCombined())))
            {
                foreach (var f in Directory.GetFiles(Root, "*.refis"))
                {
                    var Data = File.ReadAllBytes(f);
                    Combined.Write(Data, 0, Data.Length);
                    //Add a block of random data
                    Combined.Write(Buffer, 0, RefisHeader.BLOCK_SIZE);
                }
            }
            //Mess up the combined file by reordering 
            using (var Combined = File.OpenRead(Path.Combine(Root, FileNames.GetCombined())))
            {
                var Count = Combined.Length / RefisHeader.BLOCK_SIZE;
                var Indexes = Enumerable.Range(0, (int)Count)
                    .Select(m => m * RefisHeader.BLOCK_SIZE)
                    .ToList();
                using (var Temp = new MemoryStream())
                {
                    var Data = new byte[RefisHeader.BLOCK_SIZE];
                    while (Indexes.Count > 0)
                    {
                        var Index = R.Next(Indexes.Count);
                        Combined.Seek(Indexes[Index], SeekOrigin.Begin);
                        Indexes.RemoveAt(Index);
                        Combined.Read(Data, 0, Data.Length);
                        Temp.Write(Data, 0, Data.Length);
                    }
                    var Buffered = Temp.ToArray();
                    //Write the data twice to test the handling of duplicates.
                    using (var Random = File.Create(Path.Combine(Root, FileNames.GetRandom())))
                    {
                        Random.Write(Buffered, 0, Buffered.Length);
                        Random.Write(Buffered, 0, Buffered.Length);
                    }
                }
            }
            return Root;
        }

        private static void CreateFileAndEncode(string RawFile, byte[] Data)
        {
            File.WriteAllBytes(RawFile, Data);
            RefisOps.CmdEncode(RawFile, RawFile + ".refis", false);
        }

        public static void Cleanup()
        {
            lock (sync)
            {
                if (!string.IsNullOrEmpty(TempDir))
                {
                    Directory.Delete(TempDir, true);
                    TempDir = null;
                }
            }
        }
    }
}
