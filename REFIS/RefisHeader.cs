using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace REFIS
{
    /// <summary>
    /// Represents a REFIS header
    /// </summary>
    public class RefisHeader
    {
        /// <summary>
        /// The size of a block. 512 is a fairly universal value that works for all disks
        /// </summary>
        /// <remarks>
        /// Optimization: This can be set to the size of a single allocation unit of the filesystem.
        /// </remarks>
        public const int BLOCK_SIZE = 512;
        /// <summary>
        /// Size of small headers
        /// </summary>
        /// <remarks>
        /// Size: MAGIC + SLAVE_HEADER + GUID + INDEX
        /// </remarks>
        public const int SLAVE_HEADER_SIZE = 5 + 1 + 16 + 8;
        /// <summary>
        /// Magic constant value for header detection
        /// </summary>
        public const string MAGIC = "REFIS";
        /// <summary>
        /// Value for master header.
        /// </summary>
        public const byte MASTER_HEADER = 0;
        /// <summary>
        /// Value for slave header.
        /// </summary>
        public const byte SLAVE_HEADER = MASTER_HEADER + 1;
        /// <summary>
        /// The maximum length of the file name in bytes
        /// This is basically <see cref="BLOCK_SIZE"/> minus the size of other header fields.
        /// The string is written null terminated to the header
        /// </summary>
        /// <remarks>
        /// Header fields:
        /// "REFIS": 5 bytes
        /// const 0: 1 byte
        /// Id: 16 bytes
        /// ChangeTime: 8 bytes
        /// CreateTime: 8 bytes
        /// FileSize: 8 bytes
        /// const 1: The null terminator
        /// </remarks>
        public const int MAX_NAME_LENGTH = BLOCK_SIZE - 5 - 1 - 16 - 8 - 8 - 8 - 1;
        /// <summary>
        /// How many bytes from the source file fit into a data block
        /// </summary>
        public const int DATA_SIZE = BLOCK_SIZE - SLAVE_HEADER_SIZE;

        public Guid Id { get; set; }
        public DateTime ChangeTime { get; set; }
        public DateTime CreateTime { get; set; }
        public long Filesize { get; set; }
        public string Filename { get; set; }
        public long Index { get; set; }
        [JsonIgnore]
        public bool IsMaster { get => Index == 0; }
        [JsonIgnore]
        public int HeaderSize { get => IsMaster ? BLOCK_SIZE : SLAVE_HEADER_SIZE; }

        [JsonConstructor]
        private RefisHeader()
        {

        }

        /// <summary>
        /// Create a REFIS header without a physical file
        /// </summary>
        /// <param name="Id">Header Id</param>
        /// <param name="Filename">File name (just name or full path)</param>
        /// <param name="Filesize">File size</param>
        /// <param name="Index">Header index</param>
        /// <param name="ChangeTime">Last write time</param>
        /// <param name="CreateTime">Creation tile</param>
        public RefisHeader(Guid Id, string Filename, long Filesize, long Index, DateTime ChangeTime, DateTime CreateTime)
        {
            if (string.IsNullOrEmpty(Filename))
            {
                throw new ArgumentException($"'{nameof(Filename)}' cannot be null or empty.", nameof(Filename));
            }
            if (Id == Guid.Empty)
            {
                throw new ArgumentException("Cannot use a nullguid");
            }
            if (Filesize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Filesize), "Size cannot be negative");
            }
            if (Index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Index), "Index cannot be negative");
            }

            this.Id = Id;
            this.Index = Index;
            if (Index == 0)
            {
                this.Filename = Path.GetFileName(Filename);
                this.Filesize = Filesize;
                try
                {
                    this.ChangeTime = ChangeTime.ToUniversalTime();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("ChangeTime is outside of permitted UTC range", nameof(ChangeTime), ex);
                }
                try
                {
                    this.CreateTime = CreateTime.ToUniversalTime();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("CreateTime is outside of permitted UTC range", nameof(CreateTime), ex);
                }
            }
        }

        public RefisHeader(FileInfo FI)
        {
            Id = Guid.NewGuid();
            if (!FI.Exists)
            {
                throw new IOException($"{FI.Name} does not exist");
            }
            Filename = FI.Name;
            ChangeTime = FI.LastWriteTimeUtc;
            CreateTime = FI.CreationTimeUtc;
            Filesize = FI.Length;
        }

        public RefisHeader(string FileName) : this(new FileInfo(FileName))
        {
            if (string.IsNullOrEmpty(FileName))
            {
                throw new ArgumentException($"'{nameof(FileName)}' cannot be null or empty.", nameof(FileName));
            }
            if (Filename.Contains("\0"))
            {
                throw new ArgumentException("Filename cannot contain nullbytes");
            }
        }

        public RefisHeader(byte[] Data) : this(new MemoryStream(Data, false))
        {
            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }
            if (Data.Length < BLOCK_SIZE)
            {
                throw new ArgumentException($"Expect {BLOCK_SIZE} bytes in constructor");
            }
        }

        public RefisHeader(Stream S)
        {
            if (S == null)
            {
                throw new ArgumentNullException(nameof(S));
            }
            if (!S.CanRead)
            {
                throw new ArgumentException("Stream not marked as readable");
            }
            using (var BR = new BinaryReader(S, Encoding.UTF8, true))
            {
                var Magic = BR.ReadBytes(5);
                if (!Magic.SequenceEqual(Encoding.UTF8.GetBytes(MAGIC)))
                {
                    throw new InvalidDataException($"Header doesn't starts with '{MAGIC}'");
                }
                var Mode = BR.ReadByte();
                if (Mode != MASTER_HEADER && Mode != SLAVE_HEADER)
                {
                    throw new InvalidDataException("Header type is invalid");
                }
                Id = new Guid(BR.ReadBytes(16));
                if (Mode == MASTER_HEADER)
                {
                    CreateTime = new DateTime(BR.ReadInt64(), DateTimeKind.Utc);
                    ChangeTime = new DateTime(BR.ReadInt64(), DateTimeKind.Utc);
                    Filesize = BR.ReadInt64();
                    var Name = BR.ReadBytes(MAX_NAME_LENGTH + 1);
                    var Nullbyte = Array.IndexOf(Name, (byte)0);
                    if (Nullbyte <= 0)
                    {
                        throw new InvalidDataException("Invalid file name in header");
                    }
                    Filename = Encoding.UTF8.GetString(Name, 0, Nullbyte);
                }
                else
                {
                    Index = BR.ReadInt64();
                    if (Index < 1)
                    {
                        throw new InvalidDataException("Header index is invalid");
                    }
                }
            }
        }

        public static bool IsHeader(byte[] Data)
        {
            if (Data == null || Data.Length != BLOCK_SIZE)
            {
                return false;
            }
            var Header = Encoding.UTF8.GetBytes(MAGIC);
            if (!Data.Take(Header.Length).SequenceEqual(Header))
            {
                return false;
            }
            if (Data[Header.Length] != MASTER_HEADER && Data[Header.Length] != SLAVE_HEADER)
            {
                return false;
            }
            return true;
        }

        public void Serialize(Stream S, long Index)
        {
            //Hide "IsMaster" member
            var IsMaster = Index == 0;
            if (Index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Index), "Invalid index");
            }
            if (S == null)
            {
                throw new ArgumentNullException(nameof(S));
            }
            if (!S.CanWrite)
            {
                throw new ArgumentException("Supplied stream is not marked as writable");
            }
            var Namelength = IsMaster ? Encoding.UTF8.GetByteCount(Filename) : 0;
            if (IsMaster)
            {
                if (string.IsNullOrEmpty(Filename))
                {
                    throw new InvalidOperationException("Cannot serialize without a file name");
                }
                if (Filename.Contains("\0"))
                {
                    throw new InvalidOperationException("File name cannot contain nullbytes");
                }
                if (Namelength > MAX_NAME_LENGTH)
                {
                    throw new InvalidOperationException($"File name is too long. Can be at most {MAX_NAME_LENGTH} bytes.");
                }
            }
            using (var BW = new BinaryWriter(S, Encoding.UTF8, true))
            {
                BW.Write(Encoding.UTF8.GetBytes(MAGIC));
                BW.Write(IsMaster ? MASTER_HEADER : SLAVE_HEADER);
                BW.Write(Id.ToByteArray());
                if (IsMaster)
                {
                    BW.Write(CreateTime.Ticks);
                    BW.Write(ChangeTime.Ticks);
                    BW.Write(Filesize);
                    BW.Write(Encoding.UTF8.GetBytes(Filename));
                    BW.Write((byte)0);
                }
                else
                {
                    BW.Write(Index);
                }
                BW.Flush();
            }
            if (IsMaster)
            {
                //Write padding bytes
                var Remain = MAX_NAME_LENGTH - Namelength;
                System.Diagnostics.Debug.Assert(Remain >= 0, "Remainder must not be negative");
                if (Remain > 0)
                {
                    S.Write(new byte[Remain], 0, Remain);
                }
            }
        }

        public void Serialize(Stream S)
        {
            Serialize(S, Index);
        }

        public byte[] Serialize(long Index)
        {
            using (var MS = new MemoryStream())
            {
                Serialize(MS, Index);
                return MS.ToArray();
            }
        }

        public byte[] Serialize()
        {
            return Serialize(Index);
        }
    }
}
