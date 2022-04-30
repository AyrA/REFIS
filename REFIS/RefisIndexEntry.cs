using System;
using System.Collections.Generic;
using System.Linq;

namespace REFIS
{
    public class RefisIndexEntry
    {
        private readonly List<RefisOffset> _headers;

        public RefisOffset[] Headers
        {
            get
            {
                return _headers.ToArray();
            }
            set
            {
                _headers.Clear();
                _headers.AddRange(value);
            }
        }

        public RefisIndexEntry()
        {
            _headers = new List<RefisOffset>();
        }

        /// <summary>
        /// Adds a header to the collection
        /// </summary>
        /// <param name="H">Header</param>
        /// <param name="Offset">Offset in source of this header</param>
        public void AddEntry(RefisHeader H, long Offset)
        {
            if (H == null)
            {
                throw new ArgumentNullException(nameof(H));
            }

            if (_headers.Count == 0 || _headers.First().Header.Id == H.Id)
            {
                //Do not add duplicate indexes
                if (_headers.All(m => m.Header.Index != H.Index))
                {
                    _headers.Add(new RefisOffset(H, Offset));
                }
            }
            else
            {
                throw new Exception("invalid header id");
            }
        }

        public RefisHeader GetMasterHeader()
        {
            return _headers.FirstOrDefault(m => m.Header.IsMaster)?.Header;
        }

        /// <summary>
        /// Gets all existing headers in order of their index
        /// </summary>
        /// <returns>Header collection with offsets</returns>
        public RefisOffset[] GetAllHeaders()
        {
            return _headers
                .OrderBy(m => m.Header.Index)
                .ToArray();
        }

        public bool IsComplete()
        {
            var Master = GetMasterHeader();
            if (Master == null)
            {
                return false;
            }

            var ExpectedCount = (int)Math.Ceiling((double)Master.Filesize / RefisHeader.DATA_SIZE);
            return _headers.Count == ExpectedCount + 1; //Add one for master header itself
        }
    }
}
