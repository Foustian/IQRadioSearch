using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IQRadioSearch
{
    public class SearchResult
    {

        /// <summary>
        /// Raw XML response from the web service
        /// </summary>
        public string ResponseXml
        {
            get { return _responseXML; }
            set { _responseXML = value; }
        }
        string _responseXML = null;
        /// <summary>
        /// Number of documents that matched this search request.
        /// </summary>
        public int TotalHitCount
        {
            get { return _hitCount; }
            set { _hitCount = value; }
        }
        int _hitCount = 0;

        /// <summary>
        /// Original Search Request, including search terms and parameters
        /// </summary>
        public SearchRequest OriginalRequest
        {
            get { return _req; }
            set { _req = value; }
        }
        SearchRequest _req = null;

        /// <summary>
        /// List of matching documents (Clips).
        /// </summary>
        public List<Hit> Hits
        {
            get { return _hits; }
            set { _hits = value; }
        }
        List<Hit> _hits = new List<Hit>();

        public string RequestUrl
        {
            get { return _requestURL; }
            set { _requestURL = value; }
        }
        string _requestURL = string.Empty;

        public int Status { get; set; }

        public string Message { get; set; }

        public Int64 MaxSinceID { get; set; }
        
    }
}
