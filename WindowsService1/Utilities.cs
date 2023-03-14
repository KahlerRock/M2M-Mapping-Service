using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WindowsService1

{
    internal class Utilities
    {

        private static List<String> logQueue = new List<String>();
        public Configuration Master_Configs = new Configuration();
        public EventLog Master_EventLog = new EventLog();
        public bool Load_Configurations(string configPath)
        {
            try
            {
                Write_To_Log(Source.Utility, new Log("Loading Config File"));
                if (!Master_Configs.loadConfiguration(configPath))
                {
                    return false;
                }
                Write_To_Log(Source.Utility, new Log("Configurations Applied"));
                generateConfigDirectories();
                return true;
            }
            catch (Exception ex)
            {
                Write_To_Log(Source.Utility, new Log("Failed to Load Configurations: " + ex.Message));
                return false;
            }
        }

        public bool Save_Configurations(string configPath)
        {
            try
            {                
                Master_Configs.saveConfiguration(configPath, Master_Configs);
                return true;
            }
            catch (Exception ex)
            {
                Write_To_Log(Source.Utility, new Log("Failed to Save Configurations: " + ex.Message + '\t' + ex.StackTrace));
                return false;
            }
        }

        public bool Load_EventLog(EventLog eventLog)
        {
            try
            {
                this.Master_EventLog = eventLog;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void Write_To_Log(Source source, Log log)
        {
            if (log.severity > Master_Configs.logLevel) return;
            try
            {
                DateTime date = new DateTime();
                date = DateTime.Now;
                string logName = date.ToString("yyyy_MMM_dd") + ".txt";
                string logTime = date.ToString("tt hh:mm:ss.fff");
                string logDir = @".\Logs\";
                Directory.CreateDirectory(logDir);
                using (StreamWriter sw = new StreamWriter(logDir + logName, true))
                {
                    string completeLog = logTime + "\t[" + source.ToString() + "] " + log.message;
                    sw.WriteLine(completeLog);

                    //Master_EventLog.WriteEntry(completeLog);
                }
            }
            catch (Exception ex)
            {
                logQueue.Add(log + ": " + ex.Message);

            }
        }

        /*public void Handle_Log_Queue(EventLog eventLog)
        {
            List<String> copyQ = new List<String>();
            copyQ.AddRange(logQueue);
            logQueue.Clear();
            for (int i = 0; i < copyQ.Count; i++)
            {
                String log = copyQ[i];
                Write_To_Log(Source.Utility, log);
            }
        }*/

        public void generateConfigDirectories()
        {
            foreach(var dirs in Master_Configs.pathList)
            {
                //if (dirs.Item3 == "d") Directory.CreateDirectory(dirs.Item2);
                //if (dirs.Item3 == "f") File.Create(dirs.Item2);
                Directory.CreateDirectory(Path.GetDirectoryName(dirs.Item2));
                if(dirs.Item3 == "f")
                {
                    if (!File.Exists(dirs.Item2))
                    {
                        File.Create(dirs.Item2).Dispose();
                    }
                }
            }
        }

        public enum Source
        {
            Installer,
            Service_Main,
            Configuration,
            Utility,
            Service,
            MappingToolSet
        }
    }

    internal class Log
    {
        public int severity;
        public string message;
        public Log(string message, int severity = 4)
        {
            this.severity = severity;
            this.message = message;
        }
    }
}
