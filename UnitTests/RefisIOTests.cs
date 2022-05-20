using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class RefisIOTests
    {
        private const string JsonOrder = @"C:\Temp\refisOrder.json";
        private const string JsonRandom = @"C:\Temp\refisRandom.json";


        /// <summary>
        /// Files in this list are deleted on <see cref="Cleanup"/>
        /// </summary>
        private readonly List<string> TempFiles = new List<string>();

        [TestCleanup]
        public void Cleanup()
        {
            //return;
            FilePrepare.Cleanup();
            foreach (var F in TempFiles)
            {
                try
                {
                    File.Delete(F);
                }
                catch
                {
                    //Don't care if the file doesn't exist.
                    if (File.Exists(F))
                    {
                        throw;
                    }
                }
            }
            TempFiles.Clear();
        }

        [TestMethod("Boolean 'Overwrite' argument is respected properly")]
        public void TestOverwrite()
        {
            var Root = FilePrepare.Prepare();
            var Name = Path.Combine(Root, FilePrepare.FileNames.SMALL);
            Assert.IsTrue(REFIS.RefisOps.CmdEncode(Name, Name + ".refis", false) == REFIS.RET.EXISTS, "Overwrite test: Block overwrite");
            Assert.IsTrue(REFIS.RefisOps.CmdEncode(Name, Name + ".refis", true) == REFIS.RET.SUCCESS, "Overwrite test: Allow overwrite");
        }

        [TestMethod("'Info' command returns success")]
        public void TestInfo()
        {
            var Name = Path.Combine(FilePrepare.Prepare(), FilePrepare.FileNames.BIG);
            Assert.IsTrue(REFIS.RefisOps.CmdInfo(Name) == REFIS.RET.SUCCESS, "Returns {0}", REFIS.RET.SUCCESS);
        }

        [TestMethod("'Scan' command works and returns success")]
        public void TestScan()
        {
            var Root = FilePrepare.Prepare();
            var CombinedFile = Path.Combine(Root, FilePrepare.FileNames.GetCombined());
            var RandomFile = Path.Combine(Root, FilePrepare.FileNames.GetRandom());

            //Check completeness of the combined files before creating index
            using (var FS = File.OpenRead(CombinedFile))
            {
                var Index = REFIS.RefisOps.CreateIndex(FS);
                Assert.IsTrue(Index.Files.Count == FilePrepare.FileNames.GetAll().Length, "Number of encoded files");
                Assert.IsTrue(Index.Files.All(m => m.Value.IsComplete()), "{0} is completely decoded", CombinedFile);
            }
            using (var FS = File.OpenRead(RandomFile))
            {
                var Index = REFIS.RefisOps.CreateIndex(FS);
                Assert.IsTrue(Index.Files.Count == FilePrepare.FileNames.GetAll().Length, "Number of encoded files");
                Assert.IsTrue(Index.Files.All(m => m.Value.IsComplete()), "{0} is completely decoded", RandomFile);
            }
            //Check index creation
            CreateIndex(Root);
        }

        [TestMethod("'Restore' command works and returns success")]
        public void TestRestore()
        {
            var Root = FilePrepare.Prepare();
            var RandomFile = Path.Combine(Root, FilePrepare.FileNames.GetRandom());
            CreateIndex(Root);
            var Index = REFIS.Tools.ReadIndexFile(JsonRandom);
            foreach (var Entry in Index.Files)
            {
                var Master = Entry.Value.GetMasterHeader();
                var Original = Path.Combine(Root, Master.Filename);
                var Dest = Path.Combine(Root, Master.Filename + ".tmp");
                TempFiles.Add(Dest);
                Assert.IsTrue(REFIS.RefisOps.CmdRestore(RandomFile, JsonRandom, Entry.Key, Dest, false) == REFIS.RET.SUCCESS, "Restore file");
                Assert.IsTrue(Tools.CompareFiles(Original, Dest), $"Compare {Dest} with {Original}");
            }
        }

        [TestMethod("'Index' command works and returns success")]
        public void TestIndex()
        {
            CreateIndex(FilePrepare.Prepare());
            Assert.IsTrue(REFIS.RefisOps.CmdList(JsonOrder) == REFIS.RET.SUCCESS, "Reading ordered index");
            Assert.IsTrue(REFIS.RefisOps.CmdList(JsonRandom) == REFIS.RET.SUCCESS, "Reading random index");
        }

        [TestMethod("GetAllHeaders() returns entries in ascending order")]
        public void TestIndexOrder()
        {
            CreateIndex(FilePrepare.Prepare());
            var Index = REFIS.Tools.ReadIndexFile(JsonRandom);
            foreach(var FileEntry in Index.Files)
            {
                var Headers = FileEntry.Value.GetAllHeaders();
                for(var i = 0; i < Headers.Length; i++)
                {
                    Assert.IsTrue(Headers[i].Header.Index == i, "Index sorting");
                }
            }
        }

        private void CreateIndex(string Root)
        {
            var CombinedFile = Path.Combine(Root, FilePrepare.FileNames.GetCombined());
            var RandomFile = Path.Combine(Root, FilePrepare.FileNames.GetRandom());
            TempFiles.Add(JsonOrder);
            TempFiles.Add(JsonRandom);
            Assert.IsTrue(REFIS.RefisOps.CmdScan(CombinedFile, JsonOrder, true) == REFIS.RET.SUCCESS, "Returns {0}", REFIS.RET.SUCCESS);
            Assert.IsTrue(REFIS.RefisOps.CmdScan(RandomFile, JsonRandom, true) == REFIS.RET.SUCCESS, "Returns {0}", REFIS.RET.SUCCESS);
        }
    }
}
