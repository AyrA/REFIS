namespace REFIS
{
    public struct RET
    {
        /// <summary>
        /// No errors
        /// </summary>
        public const int SUCCESS = 0;
        /// <summary>
        /// Destination file or directory already exists
        /// </summary>
        public const int EXISTS = 1;
        /// <summary>
        /// Expected a header but none was found
        /// </summary>
        public const int NOHEADER = 2;
        /// <summary>
        /// Wrong type of header was found
        /// </summary>
        public const int WRONGHEADER = 3;
        /// <summary>
        /// General data error in REFIS file
        /// </summary>
        public const int DATAERROR = 4;
        /// <summary>
        /// Failed to set file attributes
        /// </summary>
        public const int ATTRFAIL = 5;
        /// <summary>
        /// A file does not exist
        /// </summary>
        public const int NOTFOUND = 6;
        /// <summary>
        /// A given id is invalid
        /// </summary>
        public const int INVALIDID = 7;
        /// <summary>
        /// Not all headers are present for recovery
        /// </summary>
        public const int INCOMPLETE = 8;
        /// <summary>
        /// Problems parsing command line arguments
        /// </summary>
        public const int PARAM_FAIL = 255;

        public static string GetMessage(int Value)
        {
            switch (Value)
            {
                case SUCCESS:
                    return "The operation completed sucessfully";
                case EXISTS:
                    return "Destination already exists. Change the name or use /Y to force overwriting it";
                case NOHEADER:
                    return "The given file lacks a REFIS header";
                case WRONGHEADER:
                    return "The given file doesn't starts with a master header";
                case DATAERROR:
                    return "Data error in REFIS file";
                case ATTRFAIL:
                    return "Failed to set file attributes";
                case NOTFOUND:
                    return "File not found";
                case INVALIDID:
                    return "Id is invalid";
                case INCOMPLETE:
                    return "The file is incomplete and cannot be restored";
                case PARAM_FAIL:
                    return "Invalid arguments. Use /? for help";
                default:
                    return $"Unknown error code: {Value}";
            }
        }
    }
}
