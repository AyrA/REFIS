using System;
using System.IO;

namespace REFIS
{
    /// <summary>
    /// Main entry point
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main function
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code</returns>
        static int Main(string[] args)
        {
            Arguments A;
            try
            {
                A = new Arguments(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing arguments.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Use the \"/?\" argument for help");
                return RET.PARAM_FAIL;
            }
            int Ret;
            switch (A.Mode)
            {
                case OpMode.Encode:
                    Ret = RefisOps.CmdEncode(A.ModeArgs[0], A.ModeArgs[1], A.Overwrite);
                    break;
                case OpMode.Decode:
                    Ret = RefisOps.CmdDecode(A.ModeArgs[0], A.ArgOrNull(1), A.Overwrite);
                    break;
                case OpMode.Info:
                    Ret = RefisOps.CmdInfo(A.ModeArgs[0]);
                    break;
                case OpMode.List:
                    Ret = RefisOps.CmdList(A.ModeArgs[0]);
                    break;
                case OpMode.Scan:
                    Ret = RefisOps.CmdScan(A.ModeArgs[0], A.ModeArgs[1], A.Overwrite);
                    break;
                case OpMode.Restore:
                    Ret = Restore(A);
                    break;
                case OpMode.Help:
                    Ret = Help();
                    break;
                default:
                    throw new NotImplementedException($"Mode not implemented: {A.Mode}");
            }
            if (Ret != RET.SUCCESS)
            {
                Console.WriteLine(RET.GetMessage(Ret));
            }
#if DEBUG
            Console.WriteLine("#END Press [ESC] to exit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
#endif
            return Ret;
        }

        /// <summary>
        /// Handles restore operation
        /// </summary>
        /// <param name="Args">Processed arguments</param>
        /// <returns>Exit code</returns>
        private static int Restore(Arguments Args)
        {
            if (Args == null)
            {
                throw new ArgumentNullException(nameof(Args));
            }
            if (Args.Mode != OpMode.Restore)
            {
                throw new InvalidOperationException("Invalid mode");
            }
            if (!Guid.TryParse(Args.ModeArgs[2], out Guid Id) || Id == Guid.Empty)
            {
                return RET.PARAM_FAIL;
            }
            return RefisOps.CmdRestore(
                Args.ModeArgs[0],
                Args.ModeArgs[1],
                Id,
                Args.ArgOrNull(3),
                Args.Overwrite);
        }

        /// <summary>
        /// Displays command line help
        /// </summary>
        /// <returns><see cref="RET.SUCCESS"/></returns>
        private static int Help()
        {
            Console.WriteLine(@"REFIS - Resillient File Storage
refis.exe /E [/Y] <infile> <outfile>
refis.exe /D [/Y] <infile> [outfile]
refis.exe /S [/Y] <infile> <indexfile>
refis.exe /I <infile>
refis.exe /L <indexfile>
refis.exe /R [/Y] <infile> <indexfile> <id> [outfile]

Modes:
/E  - Encode a file into REFIS format
/D  - Decode a file from REFIS format
/S  - Scan a file for REFIS data and write index to file
/I  - Read header information from the given file
/L  - List contents of index
/R  - Restore a file from index

'outfile', if optional and not supplied, will be taken from the header
and restored to the current working directory. If 'outfile' is a directory,
the file name will be appended to it.

Switches:
/Y  - Overwrite destination

See https://github.com/AyrA/REFIS for details");
            return RET.SUCCESS;
        }
    }
}
