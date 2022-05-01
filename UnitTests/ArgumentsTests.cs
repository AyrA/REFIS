using Microsoft.VisualStudio.TestTools.UnitTesting;
using REFIS;
using System;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class ArgumentsTests
    {
        private static readonly string[] Modes = "/E /D /R /S /I /L".Split(' ');

        [TestMethod]
        public void DuplicateArgumentsTest()
        {
            //Test duplicate mode args (with case insensitivity)
            foreach (var M in Modes)
            {
                Assert.ThrowsException<ArgumentException>(delegate { new Arguments(M.ToUpper(), M.ToLower()); }, "Duplicate test");
            }
            //Test duplicate /Y (with case insensitivity)
            Assert.ThrowsException<ArgumentException>(delegate { new Arguments("/Y", "/y"); }, "Duplicate test");
        }

        [TestMethod]
        public void NoModeTest()
        {
            Assert.ThrowsException<ArgumentException>(delegate { new Arguments("X", "X"); }, "Missing mode test");
        }

        [TestMethod]
        public void HelpTest()
        {
            Assert.IsTrue(new Arguments().Mode == OpMode.Help, "Help test");
            Assert.IsTrue(new Arguments(new string[0]).Mode == OpMode.Help, "Help test");
            Assert.IsTrue(new Arguments("/?").Mode == OpMode.Help, "Help test");
        }

        [TestMethod]
        public void NotEnoughArgumentTest()
        {
            //All modes need at least one argument
            foreach (var M in Modes)
            {
                Assert.ThrowsException<ArgumentException>(delegate { new Arguments(M); }, "Missing arguments test");
            }
        }

        [TestMethod]
        public void OptionalArgumentTest()
        {
            var Valid = Environment.ExpandEnvironmentVariables("%COMSPEC%");
            //Modes D and R have optional output argument
            Assert.IsTrue(new Arguments("/D", Valid).Mode == OpMode.Decode, "Optional outfile argument");
            Assert.IsTrue(new Arguments("/R", Valid, Valid, "X").Mode == OpMode.Restore, "Optional outfile argument");
        }

        [TestMethod]
        public void FileArgTest()
        {
            var Invalid = Path.Combine(Environment.CurrentDirectory, "NUL", "non-existent.bin");
            var Valid = Environment.ExpandEnvironmentVariables("%COMSPEC%");
            //E,D,I,S,L: First argument must exist
            //R        : First and second argument must exist
            foreach (var M in Modes)
            {
                Assert.ThrowsException<FileNotFoundException>(delegate { new Arguments(M.ToUpper(), Invalid, "X", "X", "X"); }, "Non-existing file test");
            }
            //Special handling for R: To test the second argument, the first must be valid
            Assert.ThrowsException<FileNotFoundException>(delegate { new Arguments("/R", Valid, Invalid, "X", "X"); }, "Non-existing file test");
        }
    }
}