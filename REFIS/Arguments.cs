using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REFIS
{
    public class Arguments
    {
        public OpMode Mode { get; }
        public string[] ModeArgs { get; }
        public bool Overwrite { get; }

        public Arguments(string[] Args)
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
                default:
                    throw new NotImplementedException($"Missing mode tests for {Mode}");
            }
            ModeArgs = Opts.ToArray();
        }

        public string ArgOrNull(int index)
        {
            return index >= 0 && index < ModeArgs.Length ? ModeArgs[index] : null;
        }

        private void CheckFile(string F)
        {
            if (!File.Exists(F))
            {
                throw new IOException($"The file {F} does not exist");
            }
            if (Directory.Exists(F))
            {
                throw new IOException($"The file {F} refers to a directory");
            }
        }

        private static void CheckLength(int RealLength, int ExpectedLength, bool AllowExceeding = true)
        {
            if (RealLength < ExpectedLength || (!AllowExceeding && RealLength > ExpectedLength))
            {
                throw new Exception($"This mode expects {RealLength} arguments. {ExpectedLength} arguments given");
            }
        }

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

    public enum OpMode
    {
        None,
        Encode,
        Decode,
        Scan,
        List,
        Restore,
        Help,
        Info
    }
}
