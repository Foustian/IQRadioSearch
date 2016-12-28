using System;
using System.Collections.Generic;

namespace IQRadioSearch
{
    public class SearchRequest
    {
        public List<string> DMAList { get; set; }
      
        public DateTime? FromDate { get; set; }
        public DateTime? FromDateGMT { get; set; }

        public int FragOffset { get { return _fragOffset; } set { _fragOffset = value; } }
        private int _fragOffset = 0;

        public int? FragSize { get; set; }

        /// <summary>
        /// Used for paging. The number of records currently displayed.
        /// </summary>
        public Int64? FromRecordID { get; set; }

        public List<Guid> GUIDList { get; set; }

        /// <summary>
        /// Determines if deleted records should be returned.
        /// </summary>
        public bool IncludeDeleted { get; set; }

        public bool IsHighlighting { get; set; }

        /// <summary>
        /// Flag indicating if this is first search then SinceID option enalbe in query.
        /// </summary>
        public bool IsInitialSearch { get; set; }

        public bool IsLogging { get; set; }

        public bool IsShowCC { get; set; }

        public bool IsSortAsc { get; set; }

        public string LogFileLocation { get; set; }

        public int PageSize { get; set; }

        public ResponseType? ResponseType { get; set; }

        /// <summary>
        /// Text to search for highlighting text.
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        ///  Used to limit the results to records with IDs at or below this. Ensures that the total record set to search upon is locked at each search.
        /// </summary>
        public Int64? SinceID { get; set; }

        public SortBy SortBy { get; set; }

        public List<string> StationIDList { get; set; }        
        
        public DateTime? ToDate { get; set; }
        public DateTime? ToDateGMT { get; set; }

        public List<string> StationAffiliate { get; set; }

        public List<int> Country { get; set; }

        public List<int> Region { get; set; }

        public List<string> ExcludeMarket { get; set; }

        public int PageNumber
        {
            get { return _pageNum; }
            set { _pageNum = value; }
        } int _pageNum = 0;

        public bool isFaceting
        {
            get { return _isFaceting; }
            set { _isFaceting = value; }
        } bool _isFaceting = true;  // Initially set to true as Radio was faceting by default, want to keep original behavior where possible
    }

    public enum SortBy
    { 
        Date
    }

    public enum ResponseType
    {
        XML,
        json
    }
}