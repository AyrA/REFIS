using System;
using System.Collections.Generic;
using System.Linq;

namespace REFIS
{
    /// <summary>
    /// Represents an entry in an <see cref="RefisIndex"/>
    /// </summary>
    public class RefisIndexEntry
    {
        /// <summary>
        /// Collection of found headers
        /// </summary>
        private readonly List<RefisOffset> _headers;

        /// <summary>
        /// Gets or sets all headers at once
        /// </summary>
        /// <remarks>
        /// Intended for serialization purposes only.
        /// Use <see cref="GetAllHeaders"/> to obtain a sorted list
        /// </remarks>
        public RefisOffset[] Headers
        {
            get
            {
                return _headers.ToArray();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _headers.Clear();
                _headers.AddRange(value);
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
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

        /// <summary>
        /// Gets the master header from the collection
        /// </summary>
        /// <returns>Master header, or null if header not present</returns>
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

        /// <summary>
        /// Checks if all headers have been found yet
        /// </summary>
        /// <returns>true, if all headers found</returns>
        /// <remarks>Files are only possible to recover if this is true</remarks>
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
