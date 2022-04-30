using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REFIS
{
    public class RefisIndex
    {
        public readonly Dictionary<Guid, RefisIndexEntry> Files;

        public RefisIndex()
        {
            Files = new Dictionary<Guid, RefisIndexEntry>();
        }

        public Guid[] GetIds()
        {
            return Files.Keys.ToArray();
        }

        public void AddHeader(RefisHeader H, long Offset)
        {
            if (H == null)
            {
                throw new ArgumentNullException(nameof(H));
            }
            if (!Files.ContainsKey(H.Id))
            {
                Files.Add(H.Id, new RefisIndexEntry());
            }
            Files[H.Id].AddEntry(H, Offset);
        }
    }
}
