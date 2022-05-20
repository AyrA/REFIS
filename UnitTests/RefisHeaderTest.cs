using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class RefisHeaderTest
    {
        [TestCleanup]
        public void Cleanup()
        {
            FilePrepare.Cleanup();
        }

        [TestMethod("Header scanning actually finds headers in valid files")]
        public void TestHeaderDetection()
        {
            var Root = FilePrepare.Prepare();
            foreach (var Name in FilePrepare.FileNames.GetAll())
            {
                var FullName = Path.Combine(Root, Name);
                REFIS.RefisHeader Header;
                var DataRaw = File.ReadAllBytes(FullName);
                var DataRefis = File.ReadAllBytes(FullName + ".refis");
                Assert.IsFalse(REFIS.RefisHeader.IsHeader(DataRaw), "Header of {0}", FullName);
                Assert.IsTrue(REFIS.RefisHeader.IsHeader(DataRefis.Take(REFIS.RefisHeader.BLOCK_SIZE).ToArray()), "Header of {0}.refis", FullName);
                try
                {
                    Header = new REFIS.RefisHeader(DataRefis);
                }
                catch
                {
                    Assert.Fail("Failed to decode {0}.refis", FullName);
                    throw;
                }
                Assert.IsTrue(Header.IsMaster, "Expected master header in {0}.refis", FullName);
                Assert.IsTrue(Path.GetFileName(FullName) == Header.Filename, "Invalid file name in header of {0}.refis", FullName);
            }
        }

        [TestMethod("Verify that decoded data is identical to source data")]
        public void TestHeaderDecoder()
        {
            var Root = FilePrepare.Prepare();
            foreach (var Name in FilePrepare.FileNames.GetAll())
            {
                var Fullname = Path.Combine(Root, Name);
                Assert.IsTrue(REFIS.RET.SUCCESS == REFIS.RefisOps.CmdDecode(Fullname + ".refis", Fullname + ".tmp", true));
                Assert.IsTrue(Tools.CompareFiles(Fullname, Fullname + ".tmp"), "{0} failed to decode. Data not identical", Name);
            }
        }
    }
}
