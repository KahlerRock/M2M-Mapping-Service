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
    internal class Configuration
    {
        public string mitUpDirectoryPath;             //Mitsui Upload Directory Path
        public string mitDownDirectoryPath;         //Mitsui Download Directory Path

        public string manUpDirectoryPath;             //Manhattan Upload Directory Path
        public string manDownDirectoryPath;           //Manhattan Download Directory Path

        public string manhattanProcessedDir;    //Manhattan Processed File Directory
        public string mitsuiProcessedDir;       //Mitsui Processed File Directory

        public string mit2manMapPath;        //Mitsui to Manhattan Map Path
        public string man2mitMapPath;        //Manhattan to Mitsui Map Path

        public string configDirectoryPath;  //Configuration Directory

        public string logDirectoryPath;      //Log Directory Path
        public string templateDirectoryPath; //Template Directory Path
        public string manhattanTemplate;    //Name of Manhattan File Template
        public string mitsuiTemplate;       //Name of Mitsui File Template

        public string mitUpExt;
        public string mitDownExt;
        public string manUpExt;
        public string manDownExt;

        public int interval;    //Time between polls in milliseconds - Default for testing 10 seconds

        public string interchangeControlNumber;    //Index needed for Mitsui ASN - START @ 00060000

        public int logLevel;    //Verboseness [0 - No Log, 1 - Light, 2 - Heavy, 3 - All

        public List<Tuple<string, string, string>> pathList;
        public List<Tuple<string, string>> extList;

        public bool loadConfiguration(string configPath)
        {
            XmlDocument configFile = new XmlDocument();
            configFile.Load(configPath);
            try
            {
                mitUpDirectoryPath = configFile.SelectSingleNode(@"//mitUpPath").InnerText;
                mitDownDirectoryPath = configFile.SelectSingleNode(@"//mitDownPath").InnerText;

                manUpDirectoryPath = configFile.SelectSingleNode(@"//manUpPath").InnerText;
                manDownDirectoryPath = configFile.SelectSingleNode(@"//manDownPath").InnerText;

                manhattanProcessedDir = configFile.SelectSingleNode(@"//manhattanProcessedDir").InnerText;
                mitsuiProcessedDir = configFile.SelectSingleNode(@"//mitsuiProcessedDir").InnerText;

                mit2manMapPath = configFile.SelectSingleNode(@"//mit2manMapPath").InnerText;
                man2mitMapPath = configFile.SelectSingleNode(@"//man2mitMapPath").InnerText;

                configDirectoryPath = configFile.SelectSingleNode(@"//configDirectoryPath").InnerText;

                logDirectoryPath = configFile.SelectSingleNode(@"//logDirectoryPath").InnerText;
                templateDirectoryPath = configFile.SelectSingleNode(@"//templateDirectoryPath").InnerText;
                manhattanTemplate = configFile.SelectSingleNode(@"//manhattanTemplate").InnerText;
                mitsuiTemplate = configFile.SelectSingleNode(@"//mitsuiTemplate").InnerText;


                mitUpExt = configFile.SelectSingleNode(@"//mitUpExt").InnerText;
                mitDownExt = configFile.SelectSingleNode(@"//mitDownExt").InnerText;
                manUpExt = configFile.SelectSingleNode(@"//manUpExt").InnerText;
                manDownExt = configFile.SelectSingleNode(@"//manDownExt").InnerText;

                interval = Convert.ToInt32(configFile.SelectSingleNode(@"//interval").InnerText);

                interchangeControlNumber = configFile.SelectSingleNode(@"//interchangeControlNumber").InnerText;

                logLevel = Convert.ToInt32(configFile.SelectSingleNode(@"//logLevel").InnerText);

                loadPathList();
                loadExtList();
                return true;
            }
            catch (Exception ex)
            {
                Utilities utility = new Utilities();
                utility.Write_To_Log(Utilities.Source.Configuration, new Log("Failed to Load Configuration: " + ex.Message + '\t' + ex.StackTrace, 0));
                return false;
            }
        }
        public bool createConfigTemplate()
        {
            try
            {
                String configName = "config.config";
                Directory.CreateDirectory(@".\Configs\");
                XmlTextWriter xtw = new XmlTextWriter(@".\Configs\" + configName, Encoding.UTF8);
                xtw.Formatting = Formatting.Indented;
                xtw.Indentation = 3;
                //Start XML Document
                xtw.WriteStartDocument();
                //Root Element
                xtw.WriteStartElement("Configurations");
                //Upload/Download Directories
                xtw.WriteComment("Path to Mitsui Upload Directory");
                xtw.WriteStartElement("mitUpPath");
                xtw.WriteString(@".\MitsuiUploadDir");
                xtw.WriteEndElement();
                xtw.WriteComment("Path to Mitsui Download Directory");
                xtw.WriteStartElement("mitDownPath");
                xtw.WriteString(@".\MitsuiDownloadDir");
                xtw.WriteEndElement();
                xtw.WriteComment("Path to Manhattan Upload Directory");
                xtw.WriteStartElement("manUpPath");
                xtw.WriteString(@".\ManhattanUploadDir");
                xtw.WriteEndElement();
                xtw.WriteComment("Path to Manhattan Download Directory");
                xtw.WriteStartElement("manDownPath");
                xtw.WriteString(@".\ManhattanDownloadDir");
                xtw.WriteEndElement();
                //Processed Directories
                xtw.WriteComment("Path to Manhattan Processed File Directory");
                xtw.WriteStartElement("manhattanProcessedDir");
                xtw.WriteString(@".\manhattanProcessedDir");
                xtw.WriteEndElement();
                xtw.WriteComment("Path to Mitsui Processed File Directory");
                xtw.WriteStartElement("mitsuiProcessedDir");
                xtw.WriteString(@".\mitsuiProcessedDir");
                xtw.WriteEndElement();
                //Map Paths
                xtw.WriteComment("Path to Mitsui to Manhattan Map File");
                xtw.WriteStartElement("mit2manMapPath");
                xtw.WriteString(@".\Maps\Mitsui2Manhattan.map");
                xtw.WriteEndElement();
                xtw.WriteComment("Path to Manhattan to Mitsui Map File");
                xtw.WriteStartElement("man2mitMapPath");
                xtw.WriteString(@".\Maps\Manhattan2Mitsui.map");
                xtw.WriteEndElement();
                //Configuration Directory
                xtw.WriteComment("Path to Configuration Directory");
                xtw.WriteStartElement("configDirectoryPath");
                xtw.WriteString(@".\Configs\");
                xtw.WriteEndElement();
                //Log Directory
                xtw.WriteComment("Path to Logs Directory");
                xtw.WriteStartElement("logDirectoryPath");
                xtw.WriteString(@".\Logs\");
                xtw.WriteEndElement();
                //Template Directory
                xtw.WriteComment("Path to Templates Directory");
                xtw.WriteStartElement("templateDirectoryPath");
                xtw.WriteString(@".\Templates\");
                xtw.WriteEndElement();
                //Template File Names
                xtw.WriteComment("Manhattan Template Name");
                xtw.WriteStartElement("manhattanTemplate");
                xtw.WriteString(@"_SHOULD-MATCH-FIRST-LINE-IN-MAP_");
                xtw.WriteEndElement();
                xtw.WriteComment("Mitsui Template Name");
                xtw.WriteStartElement("mitsuiTemplate");
                xtw.WriteString(@"_SHOULD-MATCH-FIRST-LINE-IN-MAP_");
                xtw.WriteEndElement();
                //File Extensions
                xtw.WriteComment("Mitsui Upload Extension");
                xtw.WriteStartElement("mitUpExt");
                xtw.WriteString(@"xml");
                xtw.WriteEndElement();
                xtw.WriteComment("Mitsui Download Extension");
                xtw.WriteStartElement("mitDownExt");
                xtw.WriteString(@"muxml");
                xtw.WriteEndElement();
                xtw.WriteComment("Manhattan Upload Extension");
                xtw.WriteStartElement("manUpExt");
                xtw.WriteString(@"mupl");
                xtw.WriteEndElement();
                xtw.WriteComment("Manhattan Download Extension");
                xtw.WriteStartElement("manDownExt");
                xtw.WriteString(@"txt");
                xtw.WriteEndElement();
                //Interval
                xtw.WriteComment("Run Interval");
                xtw.WriteStartElement("interval");
                xtw.WriteString((10000).ToString());
                xtw.WriteEndElement();
                //Interchange Control Number
                xtw.WriteComment("Interchange Control Number - Needed for Mitsui ASN - Start @ 00060000");
                xtw.WriteStartElement("interchangeControlNumber");
                xtw.WriteString("00060000");
                xtw.WriteEndElement();
                //Logging Level
                xtw.WriteComment("The Verboseness of Logging - 0: None, ..., 4: All");
                xtw.WriteStartElement("logLevel");
                xtw.WriteString((1).ToString());
                xtw.WriteEndElement();
                //End Root
                xtw.WriteEndElement();
                //End XML Document
                xtw.WriteEndDocument();
                //Close Writer
                xtw.Close();
                Utilities utilities = new Utilities();
                //utilities.Write_To_Log(Utilities.Source.Configuration, "Succesfully Created Template");
                return true;
            }
            catch (Exception ex)
            {
                Utilities utility = new Utilities();
                //utility.Write_To_Log(Utilities.Source.Configuration, "Failed to Create Config Template: " + ex.Message);
                return false;
            }
        }

        public bool saveConfiguration(string configPath, Configuration configuration)
        {
            Utilities utilities = new Utilities();

            try
            {

                XmlDocument configFile = new XmlDocument();

                configFile.Load(configPath);

                configFile.SelectSingleNode(@"//mitUpPath").InnerText = configuration.mitUpDirectoryPath;
                configFile.SelectSingleNode(@"//mitDownPath").InnerText = configuration.mitDownDirectoryPath;

                configFile.SelectSingleNode(@"//manUpPath").InnerText = configuration.manUpDirectoryPath;
                configFile.SelectSingleNode(@"//manDownPath").InnerText = configuration.manDownDirectoryPath;

                configFile.SelectSingleNode(@"//manhattanProcessedDir").InnerText = configuration.manhattanProcessedDir;
                configFile.SelectSingleNode(@"//mitsuiProcessedDir").InnerText = configuration.mitsuiProcessedDir;

                configFile.SelectSingleNode(@"//mit2manMapPath").InnerText = configuration.mit2manMapPath;
                configFile.SelectSingleNode(@"//man2mitMapPath").InnerText = configuration.man2mitMapPath;

                configFile.SelectSingleNode(@"//configDirectoryPath").InnerText = configuration.configDirectoryPath;

                configFile.SelectSingleNode(@"//logDirectoryPath").InnerText = configuration.logDirectoryPath;
                configFile.SelectSingleNode(@"//templateDirectoryPath").InnerText = configuration.templateDirectoryPath;
                configFile.SelectSingleNode(@"//manhattanTemplate").InnerText = configuration.manhattanTemplate;
                configFile.SelectSingleNode(@"//mitsuiTemplate").InnerText = configuration.mitsuiTemplate;


                configFile.SelectSingleNode(@"//mitUpExt").InnerText = configuration.mitUpExt;
                configFile.SelectSingleNode(@"//mitDownExt").InnerText = configuration.mitDownExt;
                configFile.SelectSingleNode(@"//manUpExt").InnerText = configuration.manUpExt;
                configFile.SelectSingleNode(@"//manDownExt").InnerText = configuration.manDownExt;

                configFile.SelectSingleNode(@"//interval").InnerText = configuration.interval.ToString();

                configFile.SelectSingleNode(@"//interchangeControlNumber").InnerText = configuration.interchangeControlNumber;

                configFile.Save(configuration.configDirectoryPath + @"config.config");

                return true;
            }catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.Configuration, new Log("[ERROR] Could Not Save Configurations: " + ex.Message + '\t' + ex.StackTrace, 0));
                return false;
            }
        }

        public void loadPathList()
        {
            pathList = new List<Tuple<string, string, string>>
            {
                new Tuple<string, string, string>("mitUpDirectoryPath", mitUpDirectoryPath, "d"),
                new Tuple<string, string, string>("mitDownDirectoryPath", mitDownDirectoryPath, "d"),

                new Tuple<string, string, string>("manUpDirectoryPath", manUpDirectoryPath, "d"),
                new Tuple<string, string, string>("manDownDirectoryPath", manDownDirectoryPath, "d"),

                new Tuple<string, string, string>("manhattanProcessedDir", manhattanProcessedDir, "d"),
                new Tuple<string, string, string>("mitsuiProcessedDir", mitsuiProcessedDir, "d"),

                new Tuple<string, string, string>("mit2manMapPath", mit2manMapPath, "f"),
                new Tuple<string, string, string>("man2mitMapPath", man2mitMapPath, "f"),

                new Tuple<string, string, string>("configDirectoryPath", configDirectoryPath, "d"),

                new Tuple<string, string, string>("logDirectoryPath", logDirectoryPath, "d"),
                new Tuple<string, string, string>("templateDirectoryPath", templateDirectoryPath, "d")
            };
        }

        public void loadExtList()
        {
            extList = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("mitUpExt", mitUpExt),
                new Tuple<string, string>("mitDownExt", mitDownExt),
                new Tuple<string, string>("manUpExt", manUpExt),
                new Tuple<string, string>("manDownExt", manDownExt)
            };
        }

        public string outputConfigs()
        {
            Utilities utilities = new Utilities();
            StringBuilder outputBuilder = new StringBuilder();
            utilities.Write_To_Log(Utilities.Source.Configuration, new Log("Logging Paths...", 2));
            outputBuilder.AppendLine("PATHS");
            foreach(Tuple<string, string, string> pair in pathList)
            {
                outputBuilder.AppendLine(pair.Item1 + ":\t" + pair.Item2);
            }
            utilities.Write_To_Log(Utilities.Source.Configuration, new Log("Logging Extensions...", 2));
            outputBuilder.AppendLine("EXTENSTIONS");
            foreach (Tuple<string, string> pair in extList)
            {
                outputBuilder.AppendLine(pair.Item1 + ":\t" + pair.Item2);
            }
            utilities.Write_To_Log(Utilities.Source.Configuration, new Log("Logging Misc Data...", 2));
            outputBuilder.AppendLine("INTERVAL");
            outputBuilder.AppendLine("interval:\t" + interval);

            outputBuilder.AppendLine("INTERCHANGE CONTROL NUMBER");
            outputBuilder.AppendLine("interchangeControlNumber:\t" + interchangeControlNumber);
            outputBuilder.AppendLine("LOG LEVEL");
            outputBuilder.AppendLine("logLevel:\t" + logLevel);

            return outputBuilder.ToString();
        }
    }
}
