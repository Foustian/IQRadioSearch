using System;
using System.Configuration;
using System.IO;

namespace IQRadioSearch
{
    public class CommonFunction
    {
        // TODO: Add new web.config settings
        public static void LogInfo(string Message, bool IsLogging = false, string LogFileLocation = "", bool OverrideConfig = false)
        {
            LogMessage(Message, "[INFO]", IsLogging, LogFileLocation, OverrideConfig);
        }

        public static void LogError(string Message, bool IsLogging = false, string LogFileLocation = "", bool OverrideConfig = false)
        {
            LogMessage(Message, "[ERROR]", IsLogging, LogFileLocation, OverrideConfig);
        }

        private static void LogMessage(string LogMessage, string MessageType, bool IsLogging, string LogFileLocation, bool OverrideConfig)
        {
            try
            {
                if ((ConfigurationManager.AppSettings["IQRadioIsLogging"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["IQRadioIsLogging"], System.Globalization.CultureInfo.CurrentCulture) == true) || OverrideConfig)
                {
                    string path = ConfigurationManager.AppSettings["IQRadioLogFileLocation"] + "LOG_" + DateTime.Today.ToString("MMddyyyy", System.Globalization.CultureInfo.CurrentCulture) + ".csv";

                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();
                    }
                    using (StreamWriter w = File.AppendText(path))
                    {
                        w.WriteLine(DateTime.Now.ToString() + " , " + MessageType + " ,\"" + LogMessage + "\"");
                    }
                }
                else if (ConfigurationManager.AppSettings["IQRadioIsLogging"] == null && IsLogging == true && !string.IsNullOrEmpty(LogFileLocation))
                {
                    string path = LogFileLocation + "LOG_" + DateTime.Today.ToString("MMddyyyy", System.Globalization.CultureInfo.CurrentCulture) + ".csv";

                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();
                    }
                    using (StreamWriter w = File.AppendText(path))
                    {
                        w.WriteLine(DateTime.Now.ToString() + " , " + MessageType + " ,\"" + LogMessage + "\"");
                    }
                }
            }
            catch (Exception)
            {
            }
        }        
    }
}