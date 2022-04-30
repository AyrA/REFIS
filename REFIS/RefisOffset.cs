using System;

namespace REFIS
{
    public class RefisOffset
    {
        public readonly long Offset;
        public readonly RefisHeader Header;

        public RefisOffset(RefisHeader header, long offset)
        {
            Offset = offset;
            Header = header ?? throw new ArgumentNullException(nameof(header));
        }
    }
}
