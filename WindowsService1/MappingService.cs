using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WindowsService1
{
    public partial class MappingService : ServiceBase
    {
        readonly System.Timers.Timer timeDelay;
        bool firstRun = true;
        int count;
        Utilities Master_Utilities = new Utilities();
        //Utilities Master_Utilities = new Utilities();
        public MappingService()
        {
            InitializeComponent();
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            timeDelay = new Timer();
            timeDelay.Elapsed += new ElapsedEventHandler(WorkProcess);
            Master_Utilities.Write_To_Log(Utilities.Source.Service_Main, new Log("Service Initializing...", 1));
            Master_Utilities.Write_To_Log(Utilities.Source.Service_Main, new Log("Working Directory: " + Directory.GetCurrentDirectory(), 1));

        }

        private void WorkProcess(object sender, ElapsedEventArgs e)
        {
            if (firstRun)
            {
                timeDelay.Interval = Master_Utilities.Master_Configs.interval;
                firstRun = false;
            }
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Checking Mitsui Upload Directory", 1));
            string[] mitDownFiles = CheckDirectory(Master_Utilities.Master_Configs.mitUpDirectoryPath, Master_Utilities.Master_Configs.mitUpExt);
            if(mitDownFiles != null)
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(mitDownFiles.Length + " Files Found", 1));
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Mapping Mitsui upload data to Manhattan download format..."));
                MappingToolSet mappingToolSet = new MappingToolSet();
                int fileIndex = 0;
                foreach (string file in mitDownFiles)
                {
                    Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(file));
                    if(!mappingToolSet.mit2manMappingV2(Master_Utilities.Master_Configs, file, fileIndex++))
                    {
                        Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("[ERROR] Failed to map file: " + file, 0));
                    }
                }
            }

            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Checking Manhattan Upload Directory", 1));
            string[] manUpFiles = CheckDirectory(Master_Utilities.Master_Configs.manUpDirectoryPath, Master_Utilities.Master_Configs.manUpExt);
            if(manUpFiles != null)
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(manUpFiles.Length + " Files Found", 1));
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Mapping Manhattan upload data to Mitsui download format..."));
                MappingToolSet mappingToolSet = new MappingToolSet();
                int fileIndex = 0;
                foreach(string file in manUpFiles)
                {
                    Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(file));
                    if(!mappingToolSet.man2mitMapping(Master_Utilities.Master_Configs, file, fileIndex++))
                    {
                        Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("[ERROR] Failed to map file: " + file, 0));
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Starting Service..."));
            if (!File.Exists(@".\Configs\config.config"))
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("[ERROR] No Existing Configuration File", 0));
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Creating Default Configuration File"));
                Master_Utilities.Master_Configs.createConfigTemplate();
            }
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Loading Configurations..."));
            if (!Master_Utilities.Load_Configurations(@".\Configs\config.config"))
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("[ERROR] Failed to load Configurations", 0));
            }
            else
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Printing Configurations...", 1));
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(Master_Utilities.Master_Configs.outputConfigs(), 1));
            }

            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Service Kickoff"));
            timeDelay.Interval = 1000;
            timeDelay.Enabled = true;
        }

        protected override void OnStop()
        {
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Stopping Service..."));
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Saving Configurations..."));
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Printing Configurations...", 2));
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log(Master_Utilities.Master_Configs.outputConfigs(), 2));
            Master_Utilities.Save_Configurations(@".\Configs\config.config");
            timeDelay.Enabled = false;
        }

        private string[] CheckDirectory(string directoryPath, string extension)
        {
            Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("Checking " + directoryPath + " with extension " + extension, 2));
            if (!Directory.Exists(directoryPath))
            {
                Master_Utilities.Write_To_Log(Utilities.Source.Service, new Log("[ERROR] Directory does not exist!", 0));
                return null;
            }
            return Directory.GetFiles(directoryPath, "*." + extension).Where(item => item.EndsWith("." + extension)).ToArray();
        }
    }
}
