using System;

namespace REFIS
{
    /// <summary>
    /// Represents a header with file offset
    /// </summary>
    public class RefisOffset
    {
        /// <summary>
        /// REFIS header
        /// </summary>
        public readonly long Offset;
        /// <summary>
        /// Offset of the header in the dump file
        /// </summary>
        public readonly RefisHeader Header;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="offset">Offset of the header in the dump file</param>
        public RefisOffset(RefisHeader header, long offset)
        {
            Offset = offset;
            Header = header ?? throw new ArgumentNullException(nameof(header));
        }
    }
}
