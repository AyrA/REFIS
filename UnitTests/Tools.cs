using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UnitTests
{
    public static class Tools
    {
        [DllImport("msvcrt.dll")]
        private static extern int memcmp(byte[] b1, byte[] b2, int count);

        private static bool CompareFast(byte[] b1, byte[] b2, int count)
        {
            if (count > b1.Length || count > b2.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count too big");
            }
            return memcmp(b1, b2, count) == 0;
        }

        private static bool CompareManaged(byte[] b1, byte[] b2, int count)
        {
            if (count > b1.Length || count > b2.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count too big");
            }
            for (var i = 0; i < count; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CompareFiles(string F1, string F2)
        {
            var IsNT = Environment.OSVersion.Platform == PlatformID.Win32NT;
            bool result;
            int Read;
            byte[] Data1 = new byte[1024 * 1024];
            byte[] Data2 = new byte[1024 * 1024];

            using (var FS1 = File.OpenRead(F1))
            {
                using (var FS2 = File.OpenRead(F2))
                {
                    if (FS1.Length != FS2.Length)
                    {
                        return false;
                    }
                    do
                    {
                        Read = FS1.Read(Data1, 0, Data1.Length);
                        if (Read != FS2.Read(Data2, 0, Data2.Length))
                        {
                            return false;
                        }
                        if (IsNT)
                        {
                            result = CompareFast(Data1, Data2, Read);
                        }
                        else
                        {
                            result = CompareManaged(Data1, Data2, Read);
                        }
                        if (!result)
                        {
                            return false;
                        }
                    } while (Read > 0);
                }
            }
            return true;
        }
    }
}
