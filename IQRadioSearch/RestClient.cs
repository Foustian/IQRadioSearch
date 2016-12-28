using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace IQRadioSearch
{
    class RestClient
    {
        public static String getXML(String URL, List<KeyValuePair<string, string>> vars, bool IsLogging, string LogFileLocation, Int32? timeOutPeriod = null, string RawParam = null)
        {
            try
            {

                Uri address = new Uri(URL);
                String ret = string.Empty;

                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = timeOutPeriod == null ? 210000 : (Int32)timeOutPeriod;

                StringBuilder data = new StringBuilder();
                int c = 0;
                foreach (KeyValuePair<String, String> kvp in vars)
                {
                    if (c > 0) data.Append("&");
                    data.Append(kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value));
                    c++;
                }

                data = data.Append(!string.IsNullOrWhiteSpace(RawParam) ? RawParam : string.Empty);

                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());

                string _URL = URL + data.ToString();

                CommonFunction.LogInfo(_URL, IsLogging, LogFileLocation);

                request.ContentLength = byteData.Length;

                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    ret = reader.ReadToEnd();

                    //CommonFunction.LogInfo(ret,IsLogging,LogFileLocation);

                    return ret;
                }
            }
            catch (TimeoutException ex)
            {
                throw;

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public static String getXML(String URL, List<KeyValuePair<string, string>> vars, bool IsLogging, string LogFileLocation, out string RequestURL, string RawParam = null)
        {
            RequestURL = String.Empty;
            try
            {

                Uri address = new Uri(URL);
                String ret = string.Empty;

                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 210000;

                StringBuilder data = new StringBuilder();
                int c = 0;
                foreach (KeyValuePair<String, String> kvp in vars)
                {
                    if (c > 0) data.Append("&");
                    data.Append(kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value));
                    c++;
                }

                data = data.Append(!string.IsNullOrWhiteSpace(RawParam) ? RawParam : string.Empty);

                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());

                string _URL = URL + data.ToString();

                RequestURL = _URL.Remove(URL.LastIndexOf("/")) + "?" + data.ToString();

                CommonFunction.LogInfo(RequestURL, IsLogging, LogFileLocation);

                request.ContentLength = byteData.Length;

                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    ret = reader.ReadToEnd();

                    //CommonFunction.LogInfo(ret,IsLogging,LogFileLocation);

                    return ret;
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("RequestUrl", RequestURL);
                throw ex;
            }

        }

        public static String getFacet(String URL, List<KeyValuePair<string, string>> vars, bool IsLogging, string LogFileLocation)
        {
            try
            {

                Uri address = new Uri(URL);
                String ret = string.Empty;

                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 210000;

                StringBuilder data = new StringBuilder();
                int c = 0;
                foreach (KeyValuePair<String, String> kvp in vars)
                {
                    if (c > 0) data.Append("&");
                    data.Append(kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value));
                    c++;
                }

                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());

                string _URL = URL + data.ToString();

                CommonFunction.LogInfo(_URL, IsLogging, LogFileLocation);

                request.ContentLength = byteData.Length;

                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    ret = reader.ReadToEnd();

                    //CommonFunction.LogInfo(ret,IsLogging,LogFileLocation);

                    return ret;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
