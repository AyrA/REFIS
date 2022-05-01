using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REFIS
{
    /// <summary>
    /// REFIS header index from a dump
    /// </summary>
    public class RefisIndex
    {
        /// <summary>
        /// REFIS files
        /// </summary>
        public readonly Dictionary<Guid, RefisIndexEntry> Files;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public RefisIndex()
        {
            Files = new Dictionary<Guid, RefisIndexEntry>();
        }

        /// <summary>
        /// Gets all file ids
        /// </summary>
        /// <returns>File ids</returns>
        /// <remarks>This also returns ids of incomplete files</remarks>
        public Guid[] GetIds()
        {
            return Files.Keys.ToArray();
        }

        /// <summary>
        /// Adds a header to the index
        /// </summary>
        /// <param name="H">Header</param>
        /// <param name="Offset">Offset in dump file of the header</param>
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
