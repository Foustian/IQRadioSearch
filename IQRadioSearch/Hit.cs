using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IQRadioSearch
{
    [DataContract]
    public class Hit
    {
        [DataMember(Name = "iqseqid")]
        public Int64 ID { get; set; }

        [DataMember(Name = "stationid")]
        public string StationID { get; set; }

        [DataMember(Name = "market")]
        public string Market { get; set; }

        [DataMember(Name = "datetime_dt")]
        public DateTime DateTime { get; set; }

        [DataMember(Name = "gmtdatetime_dt")]
        public DateTime GMTDateTime { get; set; }

        [DataMember(Name = "guid")]
        public Guid GUID { get; set; }

        [DataMember(Name = "timezone")]
        public string TimeZone { get; set; }

        [DataMember(Name = "iq_cc_key")]
        public string IQCCKey { get; set; }

        public int TotalNoOfOccurrence { get; set; }

        public List<TermOccurrence> TermOccurrences { get; set; }

        public List<TermOccurrence> ClosedCaptions { get; set; }

        [DataMember(Name = "cc_gen")]
        public string CCText { get; set; }

    }

    public class TermOccurrence
    {
        public int TimeOffset { get; set; }

        public string SurroundingText { get; set; }

        public string SearchTerm { get; set; }
    }
}
