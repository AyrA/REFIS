using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REFIS
{
    /// <summary>
    /// Parses command line arguments
    /// </summary>
    public class Arguments
    {
        /// <summary>
        /// Gets the operation mode
        /// </summary>
        public OpMode Mode { get; }

        /// <summary>
        /// Gets additional arguments provided with the mode
        /// </summary>
        public string[] ModeArgs { get; }

        /// <summary>
        /// Gets if /Y was specified
        /// </summary>
        public bool Overwrite { get; }

        /// <summary>
        /// Creates a new instance from the given arguments
        /// </summary>
        /// <param name="Args">Command line arguments</param>
        public Arguments(params string[] Args)
        {
            if (Args == null || Args.Length == 0 || Args.Contains("/?"))
            {
                Mode = OpMode.Help;
                return;
            }
            var Arguments = Args.ToList();
            var Opts = new List<string>();
            Mode = OpMode.None;
            for (var i = 0; i < Arguments.Count; i++)
            {
                var Arg = Arguments[i];
                if (Arg.StartsWith("/"))
                {
                    if (Arg.ToUpper() == "/Y")
                    {
                        if (Overwrite)
                        {
                            throw new ArgumentException("Duplicate /Y argument");
                        }
                        Overwrite = true;
                    }
                    else
                    {
                        if (Mode != OpMode.None)
                        {
                            throw new ArgumentException($"Mode already set but new mode provided: {Arg}");
                        }
                        switch (Arg.ToUpper())
                        {
                            case "/E":
                                Mode = OpMode.Encode;
                                break;
                            case "/D":
                                Mode = OpMode.Decode;
                                break;
                            case "/S":
                                Mode = OpMode.Scan;
                                break;
                            case "/I":
                                Mode = OpMode.Info;
                                break;
                            case "/L":
                                Mode = OpMode.List;
                                break;
                            case "/R":
                                Mode = OpMode.Restore;
                                break;
                            default:
                                throw new ArgumentException($"Unknown mode: {Arg}");
                        }
                    }
                }
                else
                {
                    Opts.Add(Arg);
                }
            }
            switch (Mode)
            {
                case OpMode.Encode:
                    CheckLength(Opts.Count, 2);
                    CheckFile(Opts[0]);
                    break;
                case OpMode.Decode:
                    CheckLength(Opts.Count, 1, true);
                    CheckFile(Opts[0]);
                    break;
                case OpMode.Scan:
                    CheckLength(Opts.Count, 2);
                    CheckFile(Opts[0]);
                    break;
                case OpMode.Info:
                    CheckLength(Opts.Count, 1);
                    CheckFile(Opts[0]);
                    break;
                case OpMode.List:
                    CheckLength(Opts.Count, 1);
                    CheckFile(Opts[0]);
                    break;
                case OpMode.Restore:
                    CheckLength(Opts.Count, 3);
                    CheckFile(Opts[0]);
                    CheckFile(Opts[1]);
                    break;
                case OpMode.None:
                    throw new ArgumentException("Mode not specified");
                default:
                    throw new NotImplementedException($"Missing mode tests for {Mode}");
            }
            ModeArgs = Opts.ToArray();
        }

        /// <summary>
        /// Returns the argument at the given index, or null if index out of bounds
        /// </summary>
        /// <param name="index">Argument index</param>
        /// <returns>Argument</returns>
        public string ArgOrNull(int index)
        {
            return index >= 0 && index < ModeArgs.Length ? ModeArgs[index] : null;
        }

        /// <summary>
        /// Checks if a given path refers to an existing file
        /// </summary>
        /// <param name="F">File name</param>
        private static void CheckFile(string F)
        {
            if (!File.Exists(F))
            {
                throw new FileNotFoundException($"The file {F} does not exist");
            }
            if (Directory.Exists(F))
            {
                throw new IOException($"The file {F} refers to a directory");
            }
        }

        /// <summary>
        /// Checks argument count against expected count
        /// </summary>
        /// <param name="RealLength">Argument count</param>
        /// <param name="ExpectedLength">Expected argument count</param>
        /// <param name="AllowExceeding">
        /// Allow <paramref name="RealLength"/> to exceed <paramref name="ExpectedLength"/>
        /// </param>
        private static void CheckLength(int RealLength, int ExpectedLength, bool AllowExceeding = true)
        {
            if (RealLength < ExpectedLength || (!AllowExceeding && RealLength > ExpectedLength))
            {
                throw new ArgumentException($"This mode expects {RealLength} arguments. {ExpectedLength} arguments given");
            }
        }

        /// <summary>
        /// Try to create an instance from the given arguments
        /// </summary>
        /// <param name="Args">Arguments</param>
        /// <param name="A">Instance variable</param>
        /// <returns>true, if instance created</returns>
        public static bool TryCreate(string[] Args, out Arguments A)
        {
            try
            {
                A = new Arguments(Args);
                return true;
            }
            catch
            {
                A = null;
            }
            return false;
        }
    }

    /// <summary>
    /// Possible modes of operation
    /// </summary>
    public enum OpMode
    {
        /// <summary>
        /// No mode selected
        /// </summary>
        None,
        /// <summary>
        /// Enocode a REFIS file
        /// </summary>
        Encode,
        /// <summary>
        /// Decode a REFIS file
        /// </summary>
        Decode,
        /// <summary>
        /// Show info of REFIS file
        /// </summary>
        Info,
        /// <summary>
        /// Scan dump for REFIS chunks
        /// </summary>
        Scan,
        /// <summary>
        /// List index file contents
        /// </summary>
        List,
        /// <summary>
        /// Restore REFIS file from dump
        /// </summary>
        Restore,
        /// <summary>
        /// Show help
        /// </summary>
        Help
    }
}
