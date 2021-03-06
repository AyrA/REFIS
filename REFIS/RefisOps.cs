using System;
using System.IO;
using System.Linq;
using System.Text;

namespace REFIS
{
    /// <summary>
    /// Provides easy access to common REFIS operations
    /// </summary>
    public static class RefisOps
    {
        /// <summary>
        /// Encodes a file into REFIS format
        /// </summary>
        /// <param name="Source">File name</param>
        /// <param name="Dest">REFIS file name</param>
        /// <param name="Overwrite">Permit overwriting of <paramref name="Dest"/></param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdEncode(string Source, string Dest, bool Overwrite)
        {
            if (Source is null)
            {
                throw new ArgumentNullException(nameof(Source));
            }

            if (Dest is null)
            {
                throw new ArgumentNullException(nameof(Dest));
            }

            if (!Overwrite && File.Exists(Dest))
            {
                return RET.EXISTS;
            }
            using (var FSin = File.OpenRead(Source))
            {
                using (var FSout = Tools.CreateFile(Dest, Overwrite))
                {
                    var Header = new RefisHeader(Source);
                    byte[] Buffer = new byte[RefisHeader.DATA_SIZE];
                    Header.Serialize(FSout, 0);
                    if (Header.Filesize > 0)
                    {
                        int index = 0;
                        int Read;
                        do
                        {
                            Read = FSin.Read(Buffer, 0, Buffer.Length);
                            if (Read > 0)
                            {
                                Header.Serialize(FSout, ++index);
                                //Regardless of how much was actually read,
                                //always write multiples of the block size
                                FSout.Write(Buffer, 0, Buffer.Length);
                            }
                        } while (Read > 0);
                    }
                }
            }
            return RET.SUCCESS;
        }

        /// <summary>
        /// Lists all files from an index file
        /// </summary>
        /// <param name="IndexFile">Index file</param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdList(string IndexFile)
        {
            if (IndexFile is null)
            {
                throw new ArgumentNullException(nameof(IndexFile));
            }

            RefisIndex Index;
            try
            {
                Index = Tools.ReadIndexFile(IndexFile);
            }
            catch (FileNotFoundException)
            {
                return RET.NOTFOUND;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read index file. {0}", ex.Message);
                return RET.DATAERROR;
            }
            if (Index.Files.Count == 0)
            {
                Console.WriteLine("The given index file is empty");
                return RET.SUCCESS;
            }
            Console.WriteLine("{0,36} Size Name Recoverable", "Id");
            foreach (var Entry in Index.Files)
            {
                var Master = Entry.Value.GetMasterHeader();
                Console.WriteLine("{0,36} {1} {2} {3}",
                    Entry.Key,
                    Master.Filesize,
                    Master.Filename,
                    Entry.Value.IsComplete() ? 'Y' : 'N');
            }
            return RET.SUCCESS;
        }

        /// <summary>
        /// Decodes a REFIS file
        /// </summary>
        /// <param name="Source">REFIS file</param>
        /// <param name="Dest">Decoded file name (optional, can be null or directory)</param>
        /// <param name="Overwrite">Permit overwriting of <paramref name="Dest"/></param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdDecode(string Source, string Dest, bool Overwrite)
        {
            if (Source is null)
            {
                throw new ArgumentNullException(nameof(Source));
            }

            const int DataSize = RefisHeader.BLOCK_SIZE - RefisHeader.SLAVE_HEADER_SIZE;
            using (var FSin = File.OpenRead(Source))
            {
                byte[] Block = new byte[RefisHeader.BLOCK_SIZE];
                if (FSin.Read(Block, 0, Block.Length) != Block.Length || !RefisHeader.IsHeader(Block))
                {
                    return RET.NOHEADER;
                }
                var Master = new RefisHeader(Block);

                if (!Master.IsMaster)
                {
                    return RET.WRONGHEADER;
                }
                //Get name from header if not supplied
                if (string.IsNullOrEmpty(Dest))
                {
                    Dest = Path.Combine(Path.GetDirectoryName(Source), Master.Filename);
                }

                if (!Overwrite && File.Exists(Dest))
                {
                    return RET.EXISTS;
                }

                using (var FSout = Tools.CreateFile(Dest, Overwrite))
                {
                    long ByteCount = 0;
                    long ExpectedIndex = 1;
                    int Read;
                    do
                    {
                        Read = FSin.Read(Block, 0, Block.Length);
                        if (Read > 0)
                        {
                            if (!RefisHeader.IsHeader(Block))
                            {
                                return RET.DATAERROR;
                            }
                            var Header = new RefisHeader(Block);
                            if (Header.IsMaster)
                            {
                                return RET.DATAERROR;
                            }
                            if (Header.Id != Master.Id)
                            {
                                return RET.DATAERROR;
                            }
                            if (Header.Index != ExpectedIndex)
                            {
                                return RET.DATAERROR;
                            }
                            FSout.Write(
                                Block,
                                RefisHeader.SLAVE_HEADER_SIZE,
                                (int)Math.Min(Master.Filesize - ByteCount, DataSize));
                            ByteCount += DataSize;
                            ExpectedIndex++;
                        }
                    } while (Read > 0);
                }
                //Don't fail if we can't set attributes
                try
                {
                    File.SetCreationTimeUtc(Dest, Master.CreateTime);
                    File.SetLastWriteTimeUtc(Dest, Master.ChangeTime);
                }
                catch
                {
                    return RET.ATTRFAIL;
                }
            }
            return RET.SUCCESS;
        }

        /// <summary>
        /// Lists information of a single REFIS file
        /// </summary>
        /// <param name="Source">REFIS file</param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdInfo(string Source)
        {
            if (Source is null)
            {
                throw new ArgumentNullException(nameof(Source));
            }

            const string RECOVERY = "For data recovery, use /S instead to scan the entire file for all headers.";
            using (var FSin = File.OpenRead(Source))
            {
                byte[] Header = new byte[RefisHeader.BLOCK_SIZE];
                if (FSin.Read(Header, 0, Header.Length) != Header.Length)
                {
                    Console.WriteLine("File is too short to contain a REFIS header");
                    return RET.SUCCESS;
                }
                if (!RefisHeader.IsHeader(Header))
                {
                    Console.WriteLine("File does not begin with a REFIS header. {0}", RECOVERY);
                    return RET.SUCCESS;
                }
                var Master = new RefisHeader(Header);
                if (!Master.IsMaster)
                {
                    Console.WriteLine("File doesn't starts with a master header. {0}", RECOVERY);
                    return RET.SUCCESS;
                }
                Console.WriteLine(@"REFIS master header information
Id: {0}
File name: {1}
File size: {2} bytes
Created: {3}
Last change: {4}",
Master.Id,
Master.Filename,
Master.Filesize,
Master.CreateTime,
Master.ChangeTime);
                return RET.SUCCESS;
            }
        }

        /// <summary>
        /// Scans a dump file for REFIS chunks and writes metadata for recovery into an index file.
        /// </summary>
        /// <param name="Source">Dump file</param>
        /// <param name="IndexFile">Index file</param>
        /// <param name="Overwrite">Permit overwriting of <paramref name="IndexFile"/></param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdScan(string Source, string IndexFile, bool Overwrite)
        {
            if (Source is null)
            {
                throw new ArgumentNullException(nameof(Source));
            }

            if (IndexFile is null)
            {
                throw new ArgumentNullException(nameof(IndexFile));
            }

            using (var FSin = File.OpenRead(Source))
            {
                using (var FSout = Tools.CreateFile(IndexFile, Overwrite))
                {
                    var HeaderIndex = CreateIndex(FSin);
                    byte[] Data = Encoding.UTF8.GetBytes(HeaderIndex.ToJson());
                    FSout.Write(Data, 0, Data.Length);
                }
            }
            return RET.SUCCESS;
        }

        /// <summary>
        /// Restores a REFIS file from a data dump such as a raw disk image.
        /// </summary>
        /// <param name="Source">Dump file with REFIS headers</param>
        /// <param name="IndexFile">Index file created previously</param>
        /// <param name="Id">Id of file to recover</param>
        /// <param name="Dest">Decoded file name (optional, can be null or directory)</param>
        /// <param name="Overwrite">Permit overwriting of <paramref name="Dest"/></param>
        /// <returns>Code from <see cref="RET"/></returns>
        public static int CmdRestore(string Source, string IndexFile, Guid Id, string Dest, bool Overwrite)
        {
            if (Source is null)
            {
                throw new ArgumentNullException(nameof(Source));
            }

            if (IndexFile is null)
            {
                throw new ArgumentNullException(nameof(IndexFile));
            }

            if (Id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(Id));
            }

            RefisIndex Index;
            try
            {
                Index = Tools.ReadIndexFile(IndexFile);
            }
            catch (FileNotFoundException)
            {
                return RET.NOTFOUND;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read index file. {0}", ex.Message);
                return RET.DATAERROR;
            }

            if (!Index.Files.ContainsKey(Id))
            {
                return RET.INVALIDID;
            }
            var Chunks = Index.Files[Id];
            if (!Chunks.IsComplete())
            {
                return RET.INCOMPLETE;
            }

            var Master = Chunks.GetMasterHeader();

            if (Dest == null)
            {
                Dest = Master.Filename;
            }
            else if (Directory.Exists(Dest))
            {
                Dest = Path.Combine(Dest, Master.Filename);
            }

            using (var FSin = File.OpenRead(Source))
            {
                using (var FSout = Tools.CreateFile(Dest, Overwrite))
                {
                    long Bytes = 0;
                    byte[] Data = new byte[RefisHeader.DATA_SIZE];
                    //Skip master header
                    var SortedChunks = Chunks.GetAllHeaders().Skip(1).ToArray();
                    foreach (var H in SortedChunks)
                    {
                        FSin.Seek(H.Offset + RefisHeader.SLAVE_HEADER_SIZE, SeekOrigin.Begin);
                        FSin.Read(Data, 0, Data.Length);
                        FSout.Write(Data, 0, (int)Math.Min(Data.Length, Master.Filesize - Bytes));
                        Bytes += RefisHeader.DATA_SIZE;
                    }
                }
            }
            return RET.SUCCESS;
        }

        /// <summary>
        /// Creates an index of a dump
        /// </summary>
        /// <param name="Source">Stream with data dump</param>
        /// <returns>Index</returns>
        public static RefisIndex CreateIndex(Stream Source)
        {
            int Read;
            var HeaderIndex = new RefisIndex();
            byte[] Buffer = new byte[RefisHeader.BLOCK_SIZE];
            do
            {
                Read = Source.Read(Buffer, 0, Buffer.Length);
                if (Read == RefisHeader.BLOCK_SIZE)
                {
                    if (RefisHeader.IsHeader(Buffer))
                    {
                        HeaderIndex.AddHeader(new RefisHeader(Buffer), Source.Position - RefisHeader.BLOCK_SIZE);
                    }
                }
            } while (Read > 0);
            return HeaderIndex;
        }
    }
}
