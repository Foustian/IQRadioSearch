using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Configuration;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using Newtonsoft.Json;

namespace IQRadioSearch
{
    public class SearchEngine
    {
        /// <summary>
        /// URL of the RESTSearch web service to connect to
        /// </summary>
        public System.Uri Url { get; set; }

        public SearchEngine(System.Uri url)
        {
            this.Url = url;
        }

        public Dictionary<string, object> Search(SearchRequest request, Int32? timeOutPeriod = null, string CustomSolrFl = "")
        {
            SearchResult res = new SearchResult();
            Dictionary<string, object> returnObject = new Dictionary<string, object>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                CommonFunction.LogInfo("IQRadio Call Start", request.IsLogging, request.LogFileLocation);

                List<KeyValuePair<string, string>> vars = new List<KeyValuePair<string, string>>();

                // 'Query' , we will pass in q query parameter of solr and 
                // 'FQuery' we will pass in the fq query parameter of solr 
                StringBuilder Query = new StringBuilder();
                StringBuilder FQuery = new StringBuilder();

                var fl = "";
                var keyword = "";

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    keyword = request.SearchTerm.Trim();
                }

                // if our search term starts with char '#' then
                // we understand that user wants exact search without sysnonym.
                // e.g. 'Find' , will only find terms with 'Find' and skip terms like 'Finding', 'Found' , ect...
                // we added that if search term is fuzzy , then we do make search in only CCgen and not CC

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    if (keyword.EndsWith("#"))
                    {
                        keyword = keyword.EndsWith("#") ? keyword.Remove(keyword.Length - 1, 1) : keyword;
                        if (Regex.IsMatch(keyword, @"\w+~[\d.]*(?=[^""]*(?:""[^""]*""[^""]*)*[^""]*$)"))
                        {
                            keyword = keyword.EndsWith("#") ? keyword.Remove(keyword.Length - 1, 1) : keyword;
                            fl = "cc_gen";
                        }
                        else
                        {
                            fl = "cc";
                        }
                    }
                    else
                    {
                        fl = "cc_gen";
                    }

                    // now if term is enclosed in double quote and it is phrase , then we must have to put slop ~2
                    // e.g. user search term "Hello World"
                    // and our CC may like "Hello 190s: World"
                    // although it is practically contious text but logically it has a word in between '190s:' 
                    // which will not allow to come in search results ,
                    // by making slop ~2 we will allow to return all search terms 
                    // which have gap of max. 2 words between them.
                    // Note : solr consider 190s: as 2 words , one is '190' (numeric) and seond is 's:' (alphabetic)

                    // New Note : we'll change the search term to lower case , but doing this
                    // we have to preserve the boolean keywords "AND" , "OR" , "NOT" in upper case, otherwise they loose their boolean operation
                    // we'll apply ~2 only on CC and not in CCgen. as CCgen is exact search
                    List<string> reservedWords = new List<string>() { "AND", "OR", "NOT" };
                    keyword = Regex.Replace(keyword, @"([\""](.*?)[\w(.*?) ]+[\""](~\d+)?)|(\w+)",
                        //@"([\""][\w ]+[\""])|(\w+)",
                                m => reservedWords.Contains(m.Value) ? m.Value : m.Value);

                    Query = Query.AppendFormat("{0}:({1})", fl, keyword);
                }

                if (request.DMAList != null && request.DMAList.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND (");
                    }

                    bool isFirst = true;

                    foreach (string dmaName in request.DMAList)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            FQuery = FQuery.AppendFormat(" market:\"{0}\"", dmaName);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR market:\"{0}\"", dmaName);
                        }
                    }

                    FQuery = FQuery.Append(" )");
                }

                if (request.ExcludeMarket != null && request.ExcludeMarket.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" NOT (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND NOT (");
                    }

                    bool isFirst = true;

                    foreach (string market in request.ExcludeMarket)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            FQuery = FQuery.AppendFormat(" market:\"{0}\"", market);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR market:\"{0}\"", market);
                        }
                    }

                    FQuery = FQuery.Append(" )");
                }

                if (request.StationIDList != null && request.StationIDList.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND (");
                    }

                    bool isFirst = true;

                    foreach (string stationID in request.StationIDList)
                    {
                        if (isFirst)
                        {
                            FQuery = FQuery.AppendFormat(" stationid:{0}", stationID);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR stationid:{0}", stationID);
                        }
                    }

                    FQuery = FQuery.Append(" )");
                }

                if (request.StationAffiliate != null && request.StationAffiliate.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND (");
                    }

                    bool isFirst = true;

                    foreach (string stationAfill in request.StationAffiliate)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            FQuery = FQuery.AppendFormat(" affiliate:\"{0}\"", stationAfill);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR affiliate:\"{0}\"", stationAfill);
                        }
                    }
                }

                if (request.Country != null && request.Country.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND (");
                    }

                    bool isFirst = true;

                    foreach (int countryNum in request.Country)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            FQuery = FQuery.AppendFormat(" country_num:{0}", countryNum);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR country_num:{0}", countryNum);
                        }
                    }

                    FQuery = FQuery.Append(" )");
                }

                if (request.Region != null && request.Region.Count > 0)
                {
                    if (string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" (");
                    }
                    else
                    {
                        FQuery = FQuery.Append(" AND (");
                    }

                    bool isFirst = true;

                    foreach (int region in request.Region)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            FQuery = FQuery.AppendFormat(" region_num:{0}", region);
                        }
                        else
                        {
                            FQuery = FQuery.AppendFormat(" OR region_num:{0}", region);
                        }
                    }

                    FQuery = FQuery.Append(" )");
                }

                Query = Query.Length == 0 ? Query.Append("isdeleted:false") : Query.Append(" AND isdeleted:false");

                if (!string.IsNullOrEmpty(request.SearchTerm) && request.IsHighlighting)
                {
                    // if user has searched with CC , then only we need to pass below params to solr.
                    // as we need to give highlight on CC ,  for user searched term.
                    // all these feilds are for highlighting functionality
                    // hl.fl =  name of the feild on which need to provide highlighting
                    // hl = value can be on/off , if on then highlighting feature is enabled otherwise disabled.
                    // hl.maxAnalyzedChars =  default max char length for highlight is 51200 , but we need unlimited
                    vars.Add(new KeyValuePair<string, string>("hl.fl", fl));
                    vars.Add(new KeyValuePair<string, string>("hl.requireFieldMatch", "true"));
                    vars.Add(new KeyValuePair<string, string>("hl", "on"));

                    vars.Add(new KeyValuePair<string, string>("hl.maxAnalyzedChars", "2147483647"));

                    // as our CC text is very long , we will get exact closed-caption 
                    // only at time of showing it while we play video
                    // in all other cases we just need to display no. of hits and not the cc text.
                    // so we'll process it only at time of showing it 
                    // and in other cases we'll get count for hits from solr highlights
                    if (request.IsShowCC)
                    {
                        // hl.fragsize = char size for fragment for highlight , 
                        // by setting it to 0 ,it will not fragment and return whole CC in sigle highlight. 
                        vars.Add(new KeyValuePair<string, string>("hl.fragsize", "0"));
                    }
                    else
                    {
                        // by setting it to 145 ,it will return no. of highlights 
                        // fragment size for signle highlight is 145 (approx)
                        if (request.FragSize.HasValue)
                        {
                            vars.Add(new KeyValuePair<string, string>("hl.fragsize", request.FragSize.Value.ToString()));
                        }
                        else
                        {
                            vars.Add(new KeyValuePair<string, string>("hl.fragsize", ConfigurationManager.AppSettings["SolrIQRadioFragSize"]));
                        }

                        vars.Add(new KeyValuePair<string, string>("hl.snippets", "99"));
                    }
                }
                else
                {
                    vars.Add(new KeyValuePair<string, string>("hl", "off"));
                }

                if (request.FromDate != null && request.ToDate != null)
                {
                    if (!string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" AND");
                    }

                    FQuery = FQuery.Append(" datetime_dt:[");
                    FQuery = FQuery.Append(request.FromDate.Value.ToString("s", System.Globalization.CultureInfo.CurrentCulture));
                    FQuery = FQuery.Append("Z TO ");
                    FQuery = FQuery.Append(request.ToDate.Value.ToString("s", System.Globalization.CultureInfo.CurrentCulture));
                    FQuery = FQuery.Append("Z]");
                }

                if (request.FromDateGMT != null && request.ToDateGMT != null)
                {
                    if (!string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" AND");
                    }

                    FQuery = FQuery.Append(" gmtdatetime_dt:[");
                    FQuery = FQuery.Append(request.FromDateGMT.Value.ToString("s", System.Globalization.CultureInfo.CurrentCulture));
                    FQuery = FQuery.Append("Z TO ");
                    FQuery = FQuery.Append(request.ToDateGMT.Value.ToString("s", System.Globalization.CultureInfo.CurrentCulture));
                    FQuery = FQuery.Append("Z]");
                }

                if (request.DMAList != null && request.DMAList.Count > 0)
                {
                    FQuery = FQuery.Append((FQuery.Length == 0 ? "(market:" : " AND (market:") + string.Join(" OR market:", request.DMAList.Select(dma => "\"" + dma + "\"")) + ")");
                }

                if (request.StationIDList != null && request.StationIDList.Count > 0)
                {
                    FQuery = FQuery.Append((FQuery.Length == 0 ? "(stationid:" : " AND (stationid:") + string.Join(" OR stationid:", request.StationIDList.Select(st => "\"" + st + "\"")) + ")");
                }

                if (request.SinceID.HasValue && request.SinceID.Value > 0)
                {
                    if (!string.IsNullOrEmpty(FQuery.ToString()))
                    {
                        FQuery = FQuery.Append(" AND");
                    }

                    FQuery = FQuery.AppendFormat(" iqseqid:[* TO {0}]", request.SinceID.Value);
                }

                if (request.GUIDList != null && request.GUIDList.Count > 0)
                {
                    FQuery = FQuery.Append((FQuery.Length == 0 ? "(guid:" : " AND (guid:") + string.Join(" OR guid:", request.GUIDList.Select(guid => "\"" + guid + "\"")) + ")");
                }

                string SortFields = "";

                // Sort Fields
                switch (request.SortBy)
                {
                    case SortBy.Date:
                        SortFields = "datetime_dt " + (request.IsSortAsc ? "asc" : "desc");
                        SortFields = SortFields + ",stationid asc";
                        break;
                }

                vars.Add(new KeyValuePair<string, string>("sort", SortFields));

                vars.Add(new KeyValuePair<string, string>("q", Query.ToString()));

                if (!string.IsNullOrWhiteSpace(FQuery.ToString()))
                {
                    vars.Add(new KeyValuePair<string, string>("fq", FQuery.ToString()));
                }

                if (string.IsNullOrEmpty(CustomSolrFl))
                {
                    fl = System.Configuration.ConfigurationManager.AppSettings["SolrIQRadioFL"];
                }
                else
                {
                    fl = CustomSolrFl;
                }

                if (string.IsNullOrEmpty(request.SearchTerm) && request.IsShowCC == true)
                {
                    fl = fl + ",cc_gen";
                }

                vars.Add(new KeyValuePair<string, string>("fl", fl));

                if (request.FromRecordID.HasValue)
                {
                    vars.Add(new KeyValuePair<string, string>("start", Convert.ToString(request.FromRecordID, System.Globalization.CultureInfo.CurrentCulture)));
                }
                else
                {
                    vars.Add(new KeyValuePair<string, string>("start", Convert.ToString((request.PageNumber) * request.PageSize, System.Globalization.CultureInfo.CurrentCulture)));
                }

                vars.Add(new KeyValuePair<string, string>("rows", Convert.ToString(request.PageSize, System.Globalization.CultureInfo.CurrentCulture)));

                if (request.ResponseType != null)
                {
                    vars.Add(new KeyValuePair<string, string>("wt", request.ResponseType.ToString()));
                }
                else
                {
                    vars.Add(new KeyValuePair<string, string>("wt", "json"));
                }

                if (request.IsInitialSearch)
                {
                    vars.Add(new KeyValuePair<string, string>("stats", "true"));
                    vars.Add(new KeyValuePair<string, string>("stats.field", "iqseqid"));
                }

                if (request.isFaceting)
                {
                    Dictionary<string, Dictionary<string, string>> facets = GetFacets(vars, request, sw);
                    returnObject.Add("Facet", facets);
                }

                var reqURL = "";
                res.ResponseXml = RestClient.getXML(Url.AbsoluteUri, vars, request.IsLogging, request.LogFileLocation, out reqURL);

                CommonFunction.LogInfo("IQRadioSolr Response - TimeTaken - " + string.Format("Minutes :{0} Seconds :{1} Mili seconds :{2} \n URL: {3}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.TotalMilliseconds, reqURL), request.IsLogging, request.LogFileLocation);

                res.OriginalRequest = request;

                CommonFunction.LogInfo("Parse Response", request.IsLogging, request.LogFileLocation);

                ParseResponse(res, fl);

            }
            catch (Exception ex)
            {
                if (res.Status >= 0)
                {
                    res.Status = -1;
                }
                res.Message = ex.ToString();

                if (ex.Data.Contains("RequestUrl"))
                {
                    CommonFunction.LogError("IQRadioSolr Error: " + ex.ToString() + ex.Data["RequestUrl"].ToString(), OverrideConfig: true);
                }
            }

            returnObject.Add("SearchResult", res);

            sw.Stop();

            CommonFunction.LogInfo("IQRadioSolr Parse Response - TimeTaken - " + string.Format("Minutes :{0} Seconds :{1} Mili seconds :{2}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.TotalMilliseconds), request.IsLogging, request.LogFileLocation);

            CommonFunction.LogInfo(string.Format("Total Hti Count :{0}", res.TotalHitCount), request.IsLogging, request.LogFileLocation);

            CommonFunction.LogInfo("IQRadio Call End", request.IsLogging, request.LogFileLocation);

            return returnObject;
        }

        private Dictionary<string, Dictionary<string, string>> GetFacets(List<KeyValuePair<string, string>> vars, SearchRequest request, Stopwatch sw)
        {
            Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>();

            var lstTasks = new List<Task>();
            var dictResults = new Dictionary<string, Dictionary<string, string>>();
            var enc = new UTF8Encoding();

            // Perform facet queries
            vars.Add(new KeyValuePair<string, string>("facet", "on"));
            vars.Add(new KeyValuePair<string, string>("facet.limit", "-1"));
            vars.Add(new KeyValuePair<string, string>("facet.mincount", "1"));
            vars.Add(new KeyValuePair<string, string>("rows", "0"));

            // Simultaneously run each facet as it's own query to improve performance
            List<KeyValuePair<string, string>> marketVars = new List<KeyValuePair<string, string>>(vars);
            marketVars.Add(new KeyValuePair<string, string>("facet.field", "iq_dma_num"));
            lstTasks.Add(Task.Factory.StartNew((object obj) => ExecuteFacetSearch(marketVars, "Market Facet Call", false, null), "RadioMarketFacet", TaskCreationOptions.AttachedToParent));

            List<KeyValuePair<string, string>> stationVars = new List<KeyValuePair<string, string>>(vars);
            stationVars.Add(new KeyValuePair<string, string>("facet.field", "stationid"));
            lstTasks.Add(Task.Factory.StartNew((object obj) => ExecuteFacetSearch(stationVars, "Market Facet Call", false, null), "RadioStationFacet", TaskCreationOptions.AttachedToParent));

            try
            {
                Task.WaitAll(lstTasks.ToArray(), 90000);
            }
            catch (AggregateException _Exception)
            {
                CommonFunction.LogInfo("Exception:" + _Exception.Message + " :: " + _Exception.StackTrace, request.IsLogging, request.LogFileLocation);
            }
            catch (Exception _Exception)
            {
                CommonFunction.LogInfo("Exception:" + _Exception.Message + " :: " + _Exception.StackTrace, request.IsLogging, request.LogFileLocation);
            }

            var dictResponses = new Dictionary<string, SearchResult>();
            foreach (var tsk in lstTasks)
            {
                SearchResult taskRes = ((Task<SearchResult>)tsk).Result;
                string taskType = (string)tsk.AsyncState;

                dictResponses.Add(taskType, taskRes);
            }

            CommonFunction.LogInfo("Solr Response - TimeTaken - for get response" + string.Format("with thread : Minutes :{0}  Seconds :{1}  Milliseconds :{2}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.TotalMilliseconds), request.IsLogging, request.LogFileLocation);

            // Create facet result objects
            Newtonsoft.Json.Linq.JObject jsonData = new Newtonsoft.Json.Linq.JObject();
            foreach (KeyValuePair<string, SearchResult> kvResponse in dictResponses)
            {
                SearchResult resFacet = kvResponse.Value;
                jsonData = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(resFacet.ResponseXml);
                Newtonsoft.Json.Linq.JArray jsonArray = new JArray();
                    switch(kvResponse.Key)
                    {
                        case "RadioMarketFacet":
                            jsonArray = (Newtonsoft.Json.Linq.JArray)jsonData["facet_counts"]["facet_fields"]["iq_dma_num"];
                            break;
                        case "RadioStationFacet":
                            jsonArray = (Newtonsoft.Json.Linq.JArray)jsonData["facet_counts"]["facet_fields"]["stationid"];
                            break;
                        default:
                            break;
                    }    

                var pairedResult = new Dictionary<string,string>();
                int pairTracker = 0;
                foreach (var elem in jsonArray) 
                {
                    if (pairTracker % 2 == 0)
                    {
                        pairedResult.Add(elem.ToString(), elem.Next.ToString());
                    }
                    pairTracker += 1;
                }
                result.Add(kvResponse.Key, pairedResult);
            }

            sw.Stop();

            CommonFunction.LogInfo("Solr Response - TimeTaken - for parse response" + string.Format("with thread : Minutes :{0}  Seconds :{1}  Milliseconds :{2}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.TotalMilliseconds), request.IsLogging, request.LogFileLocation);


            return result;
        }

        private SearchResult ExecuteFacetSearch(List<KeyValuePair<string, string>> vars, string logMessage, bool isLogging, string logFileLocation)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                string requestUrl;
                string xml = RestClient.getXML(Url.AbsoluteUri, vars, isLogging, logFileLocation, out requestUrl);
                CommonFunction.LogInfo("\"" + logMessage + " (" + sw.ElapsedMilliseconds + "ms),\"" + requestUrl, isLogging, logFileLocation);
                sw.Stop();

                SearchResult res = new SearchResult();
                res.ResponseXml = xml;
                return res;
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("RequestUrl"))
                {
                    CommonFunction.LogInfo("\"" + logMessage + " - ERROR,\"Error occurred for request url " + ex.Data["RequestUrl"], isLogging, logFileLocation);
                }
                throw;
            }
        }

        private void ParseResponse(SearchResult p_SR, string p_HighlightFieldName)
        {
            try
            {
                JObject jObj = JObject.Parse(p_SR.ResponseXml);

                p_SR.TotalHitCount = Convert.ToInt32(jObj["response"]["numFound"]);

                if (p_SR.OriginalRequest.IsInitialSearch)
                {
                    p_SR.MaxSinceID = Convert.ToInt64(jObj["stats"]["stats_fields"]["iqseqid"]["max"]);
                }

                if (p_SR.TotalHitCount > 0)
                {
                    p_SR.Hits = new List<Hit>();

                    p_SR.Hits = (List<Hit>)Newtonsoft.Json.JsonConvert.DeserializeObject(jObj["response"]["docs"].ToString(), p_SR.Hits.GetType());

                    if (!string.IsNullOrWhiteSpace(p_SR.OriginalRequest.SearchTerm) && p_SR.OriginalRequest.IsHighlighting)
                    {
                        Dictionary<string, Dictionary<string, string[]>> highlights = new Dictionary<string, Dictionary<string, string[]>>();

                        highlights = (Dictionary<string, Dictionary<string, string[]>>)Newtonsoft.Json.JsonConvert.DeserializeObject(jObj["highlighting"].ToString(), highlights.GetType());

                        foreach (Hit hit in p_SR.Hits)
                        {
                            if (highlights[hit.IQCCKey].Values.Count > 0)
                            {
                                var hilitArr = highlights[hit.IQCCKey].Values.ElementAt(0);

                                hit.TotalNoOfOccurrence = hilitArr.Length;

                                string text = "";

                                if (!p_SR.OriginalRequest.IsShowCC)
                                {
                                    foreach (var hilit in hilitArr)
                                    {
                                        if (!string.IsNullOrWhiteSpace(hilit.Trim()) && Regex.IsMatch(hilit.Trim(), "^(([0-9]*[.])?[0-9]+s:\\s*\\w*)"))
                                        {
                                            text += hilit;
                                        }
                                        else
                                        {
                                            text = text + " 9999.9999s: " + hilit;
                                        }
                                    }
                                }
                                else
                                {
                                    text = hilitArr[0];
                                }

                                int totalNoOfOccurrence = 0;
                                List<TermOccurrence> closedCaptions = new List<TermOccurrence>();
                                hit.TermOccurrences = parseCC(text, out totalNoOfOccurrence, p_SR.OriginalRequest.FragOffset, out closedCaptions);

                                hit.TotalNoOfOccurrence = totalNoOfOccurrence;
                                hit.ClosedCaptions = closedCaptions;
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(p_SR.OriginalRequest.SearchTerm) && p_SR.OriginalRequest.IsShowCC)
                    {
                        foreach (Hit hit in p_SR.Hits)
                        {
                            var pattern = @"\b(?=\s*\d+\.\d+s:)";

                            // we will split all the lines and get their index and item string
                            // it will return text in ItemName and , its arry index no. in Postion
                            // ItemName : 0s: hello world  , Position : 0
                            // ItemName : 1s: this is new day  , Position : 1
                            // ItemName : 9999s: this is new day  , Position : 2
                            // ItemName : 649s: this is new day  , Position : 3
                            List<TermOccurrence> tcList = new Regex(pattern).Split(hit.CCText).Where(
                                s =>
                                string.IsNullOrEmpty(s.Trim()) == false).Select((item, index) => new TermOccurrence
                                {
                                    TimeOffset = string.IsNullOrEmpty(Regex.Match(item, "([0-9]*[.])?[0-9]+(s:)").ToString().Replace("s:", string.Empty)) ? 0 : Convert.ToInt32(Convert.ToDouble(Regex.Match(item, "([0-9]*[.])?[0-9]+(s:)").ToString().Replace("s:", string.Empty))),
                                    SurroundingText = Regex.Replace(item, "(([0-9]*[.])?[0-9]+)*(s:)", string.Empty)
                                }).ToList();
                            hit.ClosedCaptions = tcList.Where(tmp => !string.IsNullOrWhiteSpace(tmp.SurroundingText) && string.Compare(tmp.SurroundingText.Trim(), "NULL", true) != 0).ToList();


                        }
                    }
                }
            }
            catch (Exception)
            {
                p_SR.Status = -2;
                throw;
            }
        }

        private static List<TermOccurrence> parseCC(string p_HighlightingText, out int p_TotalOccurences, int p_FragOffset, out List<TermOccurrence> p_ClosedCaptions)
        {
            p_TotalOccurences = 0;
            string txtHighlight = p_HighlightingText;

            List<TermOccurrence> occurences = new List<TermOccurrence>();

            List<int> _ListOfProccesedElement = new List<int>();
            List<int> _ListOfProccesedOffset = new List<int>();

            // if our search is phrase search , 
            // then it is possibility that our highlight string is like
            // e.g. 4s: and in this <em>new</em>, 5s: <em>year</em> everybody try to make our planet clean and polution free. 
            // and we need to replace it with 
            //  4s: and in this <em>new</em>, <em>year</em> everybody try to make our planet clean and polution free. 
            // below is regex for that.
            txtHighlight = Regex.Replace(txtHighlight, "</span>(.)(\\s*)((([0-9]*[.])?[0-9]+)*)(s:)(\\s*)(.)<span class=\"highlight\"", "</span>$1$2<span class=\"highlight\"");

            // now get all the lines in CC,
            var pattern = @"\b(?=\s*\d+\.\d+s:)";


            // we will split all the lines and get their index and item string
            // it will return text in ItemName and , its arry index no. in Postion
            // ItemName : 0s: hello world  , Position : 0
            // ItemName : 1s: this is new day  , Position : 1
            // ItemName : 9999s: this is new day  , Position : 2
            // ItemName : 649s: this is new day  , Position : 3
            var templines = new Regex(pattern).Split(txtHighlight).Where(
                s =>
                string.IsNullOrEmpty(s.Trim()) == false).Select((item, index) => new
                {
                    Offset = Regex.Match(item, "([0-9]*[.])?[0-9]+(s:)").ToString().Replace("s:", string.Empty),
                    SText = Regex.Replace(item, "(([0-9]*[.])?[0-9]+)*(s:)", string.Empty),
                    MatchIndex = index
                }).ToList();

            // Three are many enteries like 123.456s: NULL 234.456s: NULL 235.67s: Hello world !! 240.00s: NULL
            // Remove such "NULL" enteries
            templines = templines.Where(tmp => string.Compare(tmp.SText.Trim(), "NULL", true) != 0).Select((item, index) => new
            {
                Offset = item.Offset,
                SText = item.SText,
                MatchIndex = index
            }).ToList();

            var lines = templines.Select((item) => new
            {
                Offset = (item.Offset == "9999.9999" && item.MatchIndex < (templines.Count() - 1) ? (Convert.ToInt32(Convert.ToDouble(templines.ElementAt(item.MatchIndex + 1).Offset)) - 1).ToString() : Convert.ToInt32(Convert.ToDouble(item.Offset)).ToString()),
                SText = item.SText,
                MatchIndex = item.MatchIndex
            }).OrderBy(o => Convert.ToInt32(Convert.ToDouble(o.Offset))).ToList();

            Func<string, string> EncodeString = (p_CCText) =>
            {
                p_CCText = Regex.Replace(p_CCText, "<span class=\"highlight\">", "@@@");
                p_CCText = Regex.Replace(p_CCText, "</span>", "@@@");
                p_CCText = System.Web.HttpUtility.HtmlEncode(p_CCText);
                p_CCText = Regex.Replace(p_CCText, "(@@@)(.*?)(@@@)", "<span class=\"highlight\">$2</span>");

                return p_CCText;
            };

            // ItemName : 0s: hello world  , Position : 0
            // ItemName : 1s: this is new day  , Position : 1
            // ItemName : 649s: this is new day  , Position : 2
            // ItemName : 9999s: this is new day  , Position : 3
            lines = lines.Select((item, index) => new
            {
                Offset = item.Offset,
                SText = EncodeString(item.SText),
                MatchIndex = index
            }).ToList();

            p_ClosedCaptions = lines.Select(item => new TermOccurrence
            {
                SurroundingText = item.SText,
                TimeOffset = string.IsNullOrEmpty(item.Offset) ? 0 : Convert.ToInt32(item.Offset)
            }).ToList();

            if (lines != null && lines.Count() > 0)
            {
                // again check in for lines , if line do have hightlight term in that. 
                // if yest , then we'll get its offset (second liek 4s: , then 4 ) , 
                // and text spoken on that offset.
                var kvps = (from m in lines
                            where m.ToString().Contains("<span")
                            select m).ToList();


                // now we have all the list highlights in out 'kvps' linq object
                // lets add them in ours hit's TermOccurence list.
                foreach (var item in kvps)
                {
                    try
                    {
                        // for CC , we'll disply's its perior and its later index text.
                        // so below is processing for that.
                        if (!_ListOfProccesedElement.Contains(item.MatchIndex))
                        {

                            TermOccurrence oc = new TermOccurrence();
                            oc.SurroundingText = string.Empty;

                            for (int i = p_FragOffset; i > 0; i--)
                            {
                                if (item.MatchIndex - i >= 0 && !_ListOfProccesedElement.Contains(item.MatchIndex - i))
                                {
                                    oc.SurroundingText += lines.ElementAt(item.MatchIndex - i).SText;
                                    _ListOfProccesedElement.Add(item.MatchIndex - i);
                                }
                            }

                            oc.SurroundingText += item.SText;
                            _ListOfProccesedElement.Add(item.MatchIndex);

                            for (int i = 1; i <= p_FragOffset; i++)
                            {
                                if (item.MatchIndex + i < lines.Count())
                                {
                                    oc.SurroundingText += lines.ElementAt(item.MatchIndex + i).SText;
                                    _ListOfProccesedElement.Add(item.MatchIndex + i);
                                }
                            }

                            oc.TimeOffset = Convert.ToInt32(string.IsNullOrEmpty(item.Offset) ? "0" : item.Offset, System.Globalization.CultureInfo.CurrentCulture);
                            occurences.Add(oc);

                            _ListOfProccesedOffset.Add(item.MatchIndex);

                        }
                        //else
                        //    _ListOfProccesedOffset.Add(item.MatchIndex);
                    }
                    catch (Exception)
                    {

                    }
                }

                p_TotalOccurences = _ListOfProccesedOffset.Count();
            }

            return occurences;
        }
    }
}