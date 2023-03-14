using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace WindowsService1
{
    internal class MappingToolSet
    {
        Utilities utilities = new Utilities();
        Dictionary<string, Tuple<string, char>> mappingMaster = new Dictionary<string, Tuple<string, char>>();

        public bool createManhattanTemplate(Configuration configuration)
        {
            try
            {
                string mapPath = configuration.mit2manMapPath;
                string templateDir = configuration.templateDirectoryPath;
                using (StreamReader sr = new StreamReader(mapPath))
                {
                    string line;
                    StringBuilder output = new StringBuilder();
                    int index = 0;
                    string fileName;
                    if ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("_"))
                        {
                            Directory.CreateDirectory(templateDir);
                            String templateName = @"\" + line + ".tmplt";
                            String templatePath = templateDir + templateName;
                            configuration.manhattanTemplate = line;
                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template name: " + templatePath);

                            Stack<String> sect = new Stack<String>();
                            sect.Push("FAILFAILFAIL");
                            fileName = line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.StartsWith("_"))
                                {
                                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] ILLEGAL MAP FORMAT - MULTIPLE ID LINES " + line);
                                }
                                else if (line.StartsWith("#"))
                                {
                                    char multiplier = line.ToCharArray()[line.Length - 1];
                                    String trimmed = line.Substring(1, line.Length - 2);
                                    String[] lineSplit = trimmed.Split(' ');
                                    String tag = lineSplit[0];
                                    String action = lineSplit[1];

                                    if (action.StartsWith("S"))
                                    {
                                        output.AppendLine('<' + tag + '>');
                                        index++;
                                        sect.Push(tag);
                                    }
                                    else
                                    {
                                        index--;
                                        output.AppendLine(" </" + tag + '>');
                                        sect.Pop();
                                    }
                                }
                                else if (!line.StartsWith("/"))
                                {
                                    String[] lineSplit = line.Split('&');
                                    String key = lineSplit[0];
                                    String value = lineSplit[1];

                                    if (value.StartsWith("/"))
                                    {
                                        output.Append('<' + key + '>');
                                        output.Append('`' + sect.Peek() + '*' + key);
                                        output.AppendLine("</" + key + '>');
                                    }
                                    else
                                    {
                                        output.Append('<' + key + '>');
                                        output.Append(value);
                                        output.AppendLine("</" + key + '>');
                                    }
                                }
                            }
                            using (StreamWriter sw = File.CreateText(templatePath))
                            {
                                sw.WriteLine(output.ToString());
                                Console.WriteLine("File Succesfully Created");
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template File Created: " + templatePath);
                                sw.Close();
                            }
                        }
                    }

                }
                return true;
            }catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + '\t' + ex.StackTrace);
                return false;
            }
        }

        public bool mit2manMappingV2(Configuration configuration, string inputPath, int fileIdex)
        {
            if (loadManhattanMapping(configuration))
            {
                try
                {
                    createManhattanTemplate(configuration);
                    XmlDocument input = new XmlDocument();

                    printManhattanMapping();



                }catch (Exception ex)
                {
                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + '\t' + ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Error Loading Map");
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Cannot Load Map");
            }

            return false;
        }

        public bool mit2manMapping(Configuration configuration, string inputPath, int fileIndex)
        {
            if (loadManhattanMapping(configuration))
            {
                try
                {
                    createManhattanTemplate(configuration);
                    //mit2ManMapping.createSample(mapPath, inputPath);
                    XmlDocument input = new XmlDocument();
                    List<Tuple<string, string, char>> mappedValues = mapManhattanFromSource(input, inputPath);
                    Dictionary<string, List<string>> mappings = new Dictionary<string, List<string>>();

                    HashSet<string> orderHeaderTags = new HashSet<string>();
                    HashSet<string> orderDetailTags = new HashSet<string>();

                    string filledFileName = null;
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Decoding Mappings...");
                    if (decodeManhattanMappings(mappedValues, mappings, orderHeaderTags, orderDetailTags))
                    {
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Decoding Mappings Successful");
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Header Tags: " + orderHeaderTags.Count + " Detail Tags: " + orderDetailTags.Count);

                        OrderHeader orderHeader = getManhattanOrderHeader(orderHeaderTags, mappings);
                        List<OrderDetail> orderDetailList = getManhattanOrderDetails(orderDetailTags, mappings);
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Order Details: " + orderDetailList.Count);
                        filledFileName = fillManhattanTemplate(configuration, orderHeader, orderDetailList, fileIndex);
                    }
                    else
                    {
                        utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Could not decode");
                        throw new Exception("Could Not Decode");
                        
                    }
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "FilledFileName: " + filledFileName);
                    if (filledFileName != null)
                    {
                        filledFileName = (filledFileName.Split('.')[0] + '.' + configuration.mitUpExt);
                        string processedFileName = configuration.manhattanProcessedDir + filledFileName;
                        if (File.Exists(processedFileName))
                        {
                            File.Copy(processedFileName, processedFileName + "OLD", true);
                            File.Delete(processedFileName);
                        }
                        File.Move(inputPath, processedFileName);
                    }
                    else
                    {
                        throw new Exception("Template not filled");
                    }

                    return true;
                }
                catch (Exception ex)
                {

                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + '\t' + ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Error Loading Map");
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Cannot Load Map");
            }
            return false;
        }

        public string fillManhattanTemplate(Configuration configuration, OrderHeader header, List<OrderDetail> details, int fileIndex)
        {
            try
            {
                //string templatePath = configuration.templateDirectoryPath + configuration.manhattanTemplate;
                string templatePath = @".\Templates\_MIT2MAN-MAP_.tmplt";
                using (StreamReader sr = new StreamReader(templatePath))
                {
                    string line;
                    bool isDetails = false;
                    StringBuilder templateData = new StringBuilder();
                    //int fileIndex = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "line: " + line);
                        if (line.ToUpper().Contains("<SHIPMENTDETAIL>"))
                        {
                            //Console.WriteLine("Enter Details");
                            isDetails = true;
                        }
                        //Console.WriteLine("Ignore Check");
                        if (!isDetails)
                        {
                            String outputLine = line;
                            if (line.Contains('`'))
                            {
                                //Console.WriteLine("DATA FILL LINE");
                                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Line: " + line);
                                Regex regex = new Regex(@"`.+(?=<)");
                                Match match = regex.Match(line);
                                string value = "";
                                if (match.Value.ToLower().Contains("carrier*carrier"))
                                {
                                    value = header.getValue(match.Value.Substring(1));
                                    if(value.Length > 17) value = value.Substring(0, 18);
                                }else if (match.Value.ToLower().Contains("userdef16"))
                                {
                                    value = header.getValue(match.Value.Substring(1));
                                    if (value.Length > 30) value = value.Substring(0, 31);
                                }
                                else
                                {
                                    value = header.getValue(match.Value.Substring(1));
                                }
                                //Console.WriteLine(match.Value + '\t' + value);
                                outputLine = line.Replace(match.Value, value);
                            }
                            templateData.Append(outputLine);
                        }
                        else
                        {
                            List<String> detailTagList = new List<String>();
                            StringBuilder detailSection = new StringBuilder();
                            while (!(line = sr.ReadLine()).ToUpper().Contains("</SHIPMENTDETAIL>"))
                            {
                                detailTagList.Add(line);
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "detailTagList: " + line);
                            }

                            int erpLineOrderNum = 1;
                            foreach (OrderDetail detail in details)
                            {
                                detailSection.Append("<ShipmentDetail>");
                                foreach (String detailTag in detailTagList)
                                {
                                    String outputLine = detailTag;
                                    //Console.WriteLine(detailTag);
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "detailTag: " + detailTag);
                                    if (detailTag.Contains('`'))
                                    {

                                        Regex regex = new Regex(@"`.+(?=<)");
                                        Match match = regex.Match(detailTag);
                                        String value = null;

                                        value = detail.getValue(match.Value.Substring(1));
                                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, value);
                                        if (value.Equals(null))
                                        {
                                            value = header.getValue(match.Value.Substring(1));
                                        }
                                        
                                        /*if (match.Value.Contains("TotalQuantity"))
                                        {
                                            value = header.getValue(match.Value.Substring(1));
                                        }
                                        else
                                        {
                                            value = detail.getValue(match.Value.Substring(1));
                                            utilities.Write_To_Log(Utilities.Source.MappingToolSet, value);
                                            if (value.Equals(null))
                                            {
                                                value = header.getValue(match.Value.Substring(1));
                                            }
                                        }*/

                                        outputLine = detailTag.Replace(match.Value, value);
                                    }
                                    else if (detailTag.Contains("ErpOrderLineNum"))
                                    {
                                        //Console.WriteLine('\n' + detailTag + '\n');
                                        outputLine = detailTag.Replace("INDEX", erpLineOrderNum.ToString());
                                        erpLineOrderNum++;
                                    }
                                    detailSection.Append(outputLine);
                                }
                                detailSection.Append("</ShipmentDetail>");
                            }
                            //Console.WriteLine(detailSection.ToString());
                            //Console.WriteLine("Exit Details");
                            isDetails = false;
                            templateData.Append(detailSection.ToString());
                        }
                    }
                    //sr.Close();
                    //Console.WriteLine("~~~~~~~~Filled Template~~~~~~~~~~~~~~~");
                    //Console.WriteLine(templateData);
                    string timestamp = DateTime.UtcNow.ToString("fff_ss_mm_HH_dd-mm-yyyy",                         //yyyy-MM-dd HH:mm:ss.fff
                                            CultureInfo.InvariantCulture);
                    Directory.CreateDirectory(configuration.manDownDirectoryPath);
                    String mappedName = @"\" + fileIndex + timestamp + "." + configuration.manDownExt;
                    String mappedPath = configuration.manDownDirectoryPath + mappedName;
                    using (StreamWriter sw = File.CreateText(mappedPath))
                    {
                        sw.WriteLine(templateData.ToString());
                        //Console.WriteLine("File Succesfully Created");
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template File Succesfully Filled: " + mappedPath);
                    }
                    return mappedName;
                }
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "RETURNING TRUE");
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message + '\t' + ex.StackTrace);
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public string fillMitsuiTemplate(Configuration configuration, OrderHeader header, List<OrderDetail> details, int fileIndex)
        {
            try
            {
                //string templatePath = configuration.templateDirectoryPath + configuration.manhattanTemplate;
                string templatePath = @".\Templates\_MAN2MIT-MAP_.tmplt";
                using (StreamReader sr = new StreamReader(templatePath))
                {
                    string line;
                    bool isDetails = false;
                    StringBuilder templateData = new StringBuilder();
                    bool countUDI = false;
                    int udiCount = 0;
                    string shipDate = String.Empty;
                    string shipDateShort = String.Empty;
                    string shipTime = String.Empty;
                    string shipTimeShort = String.Empty;
                    string interchangeControlNumber = configuration.interchangeControlNumber;
                    //int fileIndex = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.ToUpper().Contains("HL-2"))
                        {
                            //Console.WriteLine("Enter Details");
                            isDetails = true;
                        }
                        //Console.WriteLine("Ignore Check");
                        if (line.Contains("GS")) countUDI = true;
                        //if (line.Contains("CTT")) countUDI = false;
                        if (countUDI) udiCount++;
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, udiCount + " Line: " + line);
                        if (!isDetails)
                        {
                            String outputLine = line;
                            if (line.Contains('`'))
                            {
                                //Console.WriteLine("DATA FILL LINE");
                                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Line: " + line);
                                Regex regex = new Regex(@"`[^\*]+(?=\*)");
                                MatchCollection matches = regex.Matches(line);
                                foreach(Match match in matches)
                                {
                                    String value = header.getValue(match.Value.Substring(1));
                                    //Console.WriteLine(match.Value + '\t' + value);
                                    if (match.Value.Contains("MEA03"))
                                    {
                                        value = value.Split('.')[0];
                                    }
                                    if (match.Value.Contains("SN02"))
                                    {
                                        value = value.Split('.')[0];
                                    }

                                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "\t" + match.Value + '\t' + value);
                                    outputLine = outputLine.Replace(match.Value, value);
                                }

                            }
                            templateData.AppendLine(outputLine);
                        }
                        else
                        {
                            List<String> detailTagList = new List<String>();
                            StringBuilder detailSection = new StringBuilder();
                            
                            while (!(line = sr.ReadLine()).ToUpper().Contains("CTT"))
                            {
                                detailTagList.Add(line);
                                //udiCount++;
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, udiCount + " DetailTag: " + line);

                            }

                            int index = 2;
                            foreach (string key in header.dataGetAll().Keys)
                            {
                                if (key.Contains("SHIPDATE"))
                                {
                                    shipDate = header.getValue(key);
                                }
                                if (key.Contains("SHIPDATESHORT"))
                                {
                                    shipDateShort = header.getValue(key);
                                }
                                if (key.Contains("SHIPTIME"))
                                {
                                    shipTime = header.getValue(key);
                                }
                                if (key.Contains("SHIPTIMESHORT"))
                                {
                                    shipTimeShort = header.getValue(key);
                                }
                            }
                            foreach (OrderDetail detail in details)
                            {
                                //detailSection.Append("<ShipmentDetail>");
                                foreach (String detailTag in detailTagList)
                                {
                                    String outputLine = detailTag;
                                    //Console.WriteLine(detailTag);
                                    udiCount++;
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, udiCount + " DetailTag: " + line);
                                    if (detailTag.Contains('`'))
                                    {

                                        Regex regex = new Regex(@"`[^\*]+(?=\*)");
                                        MatchCollection matches = regex.Matches(detailTag);
                                        foreach(Match match in matches)
                                        {
                                            String value;
                                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, match.Value);

                                            if (header.dataGetAll().Keys.Contains(match.Value.Substring(1)))
                                            {
                                                value = header.getValue(match.Value.Substring(1));
                                            }
                                            else
                                            {
                                                value = detail.getValue(match.Value.Substring(1));
                                            }
                                            /*if (match.Value.Contains("REF02"))
                                            {
                                                value = header.getValue(match.Value.Substring(1));
                                            }
                                            else
                                            {
                                                value = detail.getValue(match.Value.Substring(1));
                                            }*/
                                        
                                            outputLine = outputLine.Replace(match.Value, value);
                                        }
                                        
                                    }
                                    else if (detailTag.Contains("HL-2"))
                                    {
                                        //Console.WriteLine('\n' + detailTag + '\n');
                                        outputLine = detailTag.Replace("INDEX", index.ToString());
                                        index++;
                                    }
                                    detailSection.AppendLine(outputLine);
                                }
                            }
                            //Console.WriteLine(detailSection.ToString());
                            //Console.WriteLine("Exit Details");
                            isDetails = false;
                            templateData.Append(detailSection.ToString());

                            //HANDLE CTT LINE
                            String tmpout = line;
                            if (line.Contains('`'))
                            {
                                //Console.WriteLine("DATA FILL LINE");
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Line: " + line);
                                Regex regex = new Regex(@"`[^\*]+(?=\*)");
                                MatchCollection matches = regex.Matches(line);
                                foreach (Match match in matches)
                                {
                                    String value = header.getValue(match.Value.Substring(1));
                                    //Console.WriteLine(match.Value + '\t' + value);
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "\t" + match.Value + '\t' + value);
                                    tmpout = tmpout.Replace(match.Value, value);
                                }

                            }
                            templateData.AppendLine(tmpout);
                            countUDI = false;
                        }
                    }
                    templateData.Replace("*~", "~");

                    Regex sectPattern = new Regex(@".+?-\d(?=\*)");
                    MatchCollection sectCollection = sectPattern.Matches(templateData.ToString());
                    foreach (Match m in sectCollection)
                    {
                        templateData.Replace(m.Value, m.Value.Substring(0, m.Value.Length - 2));
                    }

                    Regex pattern = new Regex(@"_.+?_");
                    MatchCollection matchList = pattern.Matches(templateData.ToString());
                    foreach (Match match in matchList)
                    {
                        string key = match.Value;
                        switch (key)
                        {
                            case "_CONTROLNUMBER_":
                                templateData.Replace(match.Value, interchangeControlNumber);
                                break;
                            case "_TAB_":
                                templateData.Replace(match.Value, "          ");
                                break;
                            case "_BLANK_":
                                templateData.Replace(match.Value, String.Empty);
                                break;
                            case "_UDI_":
                                String udiDetails = udiCount.ToString();
                                templateData.Replace(match.Value, udiDetails);
                                break;
                            case "_SHIPDATE_":
                                templateData.Replace(match.Value, shipDate);
                                break;
                            case "_SHIPDATESHORT_":
                                templateData.Replace(match.Value, shipDateShort);
                                break;
                            case "_SHIPTIME_":
                                templateData.Replace(match.Value, shipTime);
                                break;
                            case "_SHIPTIMESHORT_":
                                templateData.Replace(match.Value, shipTimeShort);
                                break;
                        }
                    }
                    string timestamp = DateTime.UtcNow.ToString("fff_ss_mm_HH_dd-mm-yyyy",                         //yyyy-MM-dd HH:mm:ss.fff
                                            CultureInfo.InvariantCulture);
                    Directory.CreateDirectory(configuration.mitDownDirectoryPath);
                    String mappedName = @"\" + fileIndex + timestamp + "." + configuration.mitDownExt;
                    String mappedPath = configuration.mitDownDirectoryPath + mappedName;
                    using (StreamWriter sw = File.CreateText(mappedPath))
                    {
                        sw.WriteLine(templateData.ToString());
                        //Console.WriteLine("File Succesfully Created");
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template File Succesfully Filled: " + mappedPath);
                    }
                    return mappedName;
                }
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "RETURNING TRUE");
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message + '\t' + ex.StackTrace);
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public string fillMitsuiTemplate(Configuration configuration, List<Tuple<string, string>> mappedValues)
        {
            try
            {
                //string templatePath = configuration.templateDirectoryPath + configuration.manhattanTemplate;
                string templatePath = @".\Templates\_MAN2MIT-MAP_.tmplt";
                using (StreamReader sr = new StreamReader(templatePath))
                {
                    string line;
                    bool isDetails = false;
                    StringBuilder templateData = new StringBuilder();
                    bool countUDI = false;
                    int udiCount = 0; ;
                    string shipDate = String.Empty;
                    string shipDateShort = String.Empty;
                    string shipTime = String.Empty;
                    string shipTimeShort = String.Empty;
                    string interchangeControlNumber = configuration.interchangeControlNumber;
                    int fileIndex = 0;


                    while ((line = sr.ReadLine()) != null)
                    {
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Line: " + line);

                        if (line.ToUpper().Contains("HL-2"))
                        {
                            //Console.WriteLine("Enter Details");
                            isDetails = true;
                        }
                        if (line.Contains("GS")) countUDI = true;
                        if (line.Contains("CTT")) countUDI = false;
                        if (countUDI) udiCount++;
                        //Console.WriteLine("Ignore Check");
                        if (!isDetails)
                        {
                            String outputLine = line;

                            if (line.Contains('`'))
                            {
                                //Console.WriteLine("DATA FILL LINE");
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Fill Location: " + line);
                                Regex regex = new Regex(@"`[^\*]+(?=\*)");
                                MatchCollection matches = regex.Matches(line);
                                foreach(Match m in matches)
                                {
                                    string mat = m.Value.Substring(1).Trim();
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "match: " + mat);
                                    String value = null;
                                    foreach(Tuple<string, string> kvp in mappedValues)
                                    {
                                        if(kvp.Item1 == mat)
                                        {
                                            value = kvp.Item2;
                                            break;
                                        }
                                    }
                                    if(value != null)
                                        outputLine = outputLine.Replace(m.Value, value);
                                }
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Filled Locations: " + outputLine);
                            }

                            templateData.AppendLine(outputLine);
                        }
                        else
                        {
                            List<String> detailTagList = new List<String>();
                            StringBuilder detailSection = new StringBuilder();
                            detailTagList.Add(line);
                            while (!(line = sr.ReadLine()).ToUpper().Contains("CTT"))
                            {
                                detailTagList.Add(line);
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Detail Tag: " + line);
                            }

                            int index = 2;
                            int detailCount = 0;
                            foreach(var kvp in mappedValues)
                            {
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, kvp.Item1 + " kvp list: " + kvp.Item2.Length);
                                if (kvp.Item1.Contains("LIN03"))
                                {
                                    detailCount++;
                                }

                                if (kvp.Item1.Contains("SHIPDATE"))
                                {
                                    shipDate = kvp.Item2;
                                }
                                if (kvp.Item1.Contains("SHIPDATESHORT"))
                                {
                                    shipDateShort = kvp.Item2;
                                }
                                if (kvp.Item1.Contains("SHIPTIME"))
                                {
                                    shipTime = kvp.Item2;
                                }
                                if (kvp.Item1.Contains("SHIPTIMESHORT"))
                                {
                                    shipTimeShort = kvp.Item2;
                                }
                            }
                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Details: " + detailCount);
                            List<Tuple<string, List<string>>> detailsMaster = new List<Tuple<string, List<string>>>();
                            foreach(string detailTag in detailTagList)
                            {
                                string key = detailTag;
                          
                            }
                            for (int i = 0; i < detailCount; i++)
                            {
                                foreach (String detailTag in detailTagList)
                                {
                                    String outputLine = detailTag;
                                    //Console.WriteLine(detailTag);
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "detailTag: " + detailTag);
                                    if (detailTag.Contains('`'))
                                    {
                                        Regex regex = new Regex(@"`[^\*]+(?=\*)");
                                        MatchCollection matches = regex.Matches(detailTag);
                                        foreach (Match m in matches)
                                        {
                                            string mat = m.Value.Substring(1).Trim();
                                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "match: " + mat);
                                            String value = null;
                                            foreach (Tuple<string, string> kvp in mappedValues)
                                            {
                                                if (kvp.Item1 == mat)
                                                {
                                                    value = kvp.Item2;
                                                    mappedValues.Remove(kvp);
                                                    break;
                                                }
                                            }
                                            if (value != null)
                                                outputLine = outputLine.Replace(m.Value, value);
                                        }
                                    }
                                    else if (detailTag.Contains("HL-2"))
                                    {
                                        //Console.WriteLine('\n' + detailTag + '\n');
                                        outputLine = detailTag.Replace("INDEX", index.ToString());
                                        index++;
                                    }
                                    detailSection.AppendLine(outputLine);
                                }
                            }
                            //Console.WriteLine(detailSection.ToString());
                            //Console.WriteLine("Exit Details");
                            isDetails = false;
                            templateData.Append(detailSection.ToString());
                        }
                    }

                    templateData.Replace("*~", "~");

                    Regex sectPattern = new Regex(@".+?-\d(?=\*)");
                    MatchCollection sectCollection = sectPattern.Matches(templateData.ToString());
                    foreach(Match m in sectCollection)
                    {
                        templateData.Replace(m.Value, m.Value.Substring(0, m.Value.Length - 2));
                    }

                    Regex pattern = new Regex(@"_.+?_");
                    MatchCollection matchList = pattern.Matches(templateData.ToString());
                    foreach(Match match in matchList)
                    {
                        string key = match.Value;
                        switch (key)
                        {
                            case "_CONTROLNUMBER_":
                                templateData.Replace(match.Value, interchangeControlNumber);
                                break;
                            case "_TAB_":
                                templateData.Replace(match.Value, "     ");
                                break;
                            case "_BLANK_":
                                templateData.Replace(match.Value, String.Empty);
                                break;
                            case "_UDI_":
                                String udiDetails = udiCount.ToString();
                                templateData.Replace(match.Value, udiDetails);
                                break;
                            case "_SHIPDATE_":
                                templateData.Replace(match.Value, shipDate);
                                break;
                            case "_SHIPDATESHORT_":
                                templateData.Replace(match.Value, shipDateShort);
                                break;
                            case "_SHIPTIME_":
                                templateData.Replace(match.Value, shipTime);
                                break;
                            case "_SHIPTIMESHORT_":
                                templateData.Replace(match.Value, shipTimeShort);
                                break;
                        }
                    }
                    //sr.Close();
                    //Console.WriteLine("~~~~~~~~Filled Template~~~~~~~~~~~~~~~");
                    //Console.WriteLine(templateData);
                    string timestamp = DateTime.UtcNow.ToString("fff_ss_mm_HH_dd-mm-yyyy",                         //yyyy-MM-dd HH:mm:ss.fff
                                            CultureInfo.InvariantCulture);
                    Directory.CreateDirectory(configuration.mitDownDirectoryPath);
                    String mappedName = @"\" + fileIndex++ + timestamp + "." + configuration.mitDownExt;
                    String mappedPath = configuration.mitDownDirectoryPath + mappedName;
                    using (StreamWriter sw = File.CreateText(mappedPath))
                    {
                        sw.WriteLine(templateData.ToString());
                        //Console.WriteLine("File Succesfully Created");
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template File Succesfully Filled: " + mappedPath);
                    }
                    return mappedName;
                }
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "RETURNING TRUE");
                
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message + '\t' + ex.StackTrace);
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public void printManhattanMapping()
        {
            utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Printing Mappings...");
            foreach (string key in mappingMaster.Keys)
            {
                Tuple<string, char> valueSet = mappingMaster[key];
                string value = valueSet.Item1;
                char multiplier = valueSet.Item2;

                //Console.WriteLine(key + '\t' + value + '\t' + multiplier);
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, key + '\t' + value + '\t' + multiplier);
            }
        }

        public bool loadManhattanMapping(Configuration configuration)
        {
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "!!!LoadMapping");
            mappingMaster.Clear();
            try
            {
                string mapPath = configuration.mit2manMapPath;
                if (!File.Exists(mapPath))
                {
                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Map Doesnt Exist: " + mapPath);
                    throw new Exception("File Open Error");
                }
                else
                {
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Reading Map Data...");
                    using (StreamReader sr = new StreamReader(mapPath))
                    {
                        string line;
                        char multiplier = '!';
                        Stack<String> sect = new Stack<String>();
                        while ((line = sr.ReadLine()) != null)
                        {
                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, line);
                            if (line.StartsWith("#"))
                            {

                                //Console.WriteLine(line);
                                multiplier = line.ToCharArray()[line.Length - 1];
                                String trimmed = line.Substring(1, line.Length - 2);
                                String[] lineSplit = trimmed.Split(' ');
                                String tag = lineSplit[0].Replace("#", String.Empty);
                                String action = lineSplit[1];

                                if (action.StartsWith("S"))
                                {
                                    sect.Push(tag);
                                }
                                else
                                {
                                    sect.Pop();
                                }
                            }
                            else if (!(line.StartsWith("/") || line.StartsWith("_")))
                            {
                                String[] split = line.Split('&');
                                String key = sect.Peek() + '*' + split[0];
                                String value = split[1];
                                //Console.WriteLine(key);
                                Tuple<string, char> valueSet = new Tuple<string, char>(value, multiplier);
                                mappingMaster.Add(key, valueSet);

                            }
                        }
                        sr.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + ' ' + ex.StackTrace);
                //Console.WriteLine(ex.StackTrace);
                //Console.WriteLine(ex.Message);
                return false;
            }
        }

        public List<Tuple<string, string, char>> mapManhattanFromSource(XmlDocument input, string inputPath)
        {
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "!!!MapFromSource");
            List<Tuple<string, string, char>> mappedValues = new List<Tuple<string, string, char>>();

            if (!_inti_Manhattan_xml_file_(input, inputPath))
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] LOAD XML ERROR");
                throw new Exception("Load XML Error");
            }
            else
            {
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Mapping keys from Mapping Master: " + mappingMaster.Count);
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, input.InnerText);
                foreach (string key in mappingMaster.Keys)
                {
                    char multiplier = mappingMaster[key].Item2;
                    string xpath = mappingMaster[key].Item1;
                    //Console.WriteLine(key);
                    if (!xpath.StartsWith("/"))
                    {
                        mappedValues.Add(new Tuple<string, string, char>(key, xpath, multiplier));
                    }
                    else
                    {
                        switch (multiplier)
                        {
                            case '#':
                                XmlNode singleNode = getSingleTag(input, xpath);
                                mappedValues.Add(new Tuple<string, string, char>(key, singleNode.InnerText, multiplier));
                                break;
                            case '@':
                                XmlNodeList nodes = getMultipleTag(input, xpath);
                                foreach (XmlNode node in nodes)
                                {
                                    mappedValues.Add(new Tuple<string, string, char>(key, node.InnerText, multiplier));
                                }
                                break;
                            default:
                                Console.WriteLine("THIS SHOULDNT HAPPEN");
                                break;
                        }
                    }
                }
            }

            return mappedValues;
        }

        public List<Tuple<string, string, char>> mapMitsuiFromSource(XmlDocument input, string inputPath)
        {
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "!!!MapFromSource: " + inputPath);
            List<Tuple<string, string, char>> mappedValues = new List<Tuple<string, string, char>>();

            if (!_inti_Mitsui_xml_file_(input, inputPath))
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] LOAD XML ERROR");
                throw new Exception("Load XML Error");
            }
            else
            {
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Mapping keys from Mapping Master: " + mappingMaster.Count);
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, input.InnerText);
                
                foreach (string key in mappingMaster.Keys)
                {
                    char multiplier = mappingMaster[key].Item2;
                    string xpath = mappingMaster[key].Item1;
                    //Console.WriteLine(key);
                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, key + '\t' +xpath + '\t' + multiplier);
                    if (!xpath.StartsWith("/"))
                    {
                        mappedValues.Add(new Tuple<string, string, char>(key, xpath, multiplier));
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, '\t' + key + '\t' + xpath);
                    }
                    else
                    {
                        string value = "@[MISSING]@";

                        switch (multiplier)
                        {
                            case '#':
                                XmlNode singleNode = getNSSingleTag(input, xpath);
                                if(singleNode.InnerText != null) value = singleNode.InnerText;
                                mappedValues.Add(new Tuple<string, string, char>(key, value, multiplier));
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, '\t' + key + '\t' + value);
                                break;
                            case '@':
                                XmlNodeList nodes = getNSMultipleTag(input, xpath);
                                if(nodes.Count > 0)
                                {
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Nodes: " + nodes.Count);
                                    foreach (XmlNode node in nodes)
                                    {
                                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, key + '\t' + node.InnerText);
                                        if (node.InnerText != null) value = node.InnerText;
                                        mappedValues.Add(new Tuple<string, string, char>(key, value, multiplier));
                                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, '\t' + key + '\t' + value);
                                    }
                                }
                                else
                                {
                                    throw new Exception("XPath not found: " + key + '\t' + xpath);
                                }

                                break;
                            default:
                                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] THIS SHOULDNT HAPPEN");
                                //Console.WriteLine("THIS SHOULDNT HAPPEN");
                                break;
                        }
                    }
                }
                string dateTime = getNSSingleTag(input, @"//ActualShipDateTime").InnerText;
                string date = dateTime.Split('T')[0];
                string time = dateTime.Split('T')[1];
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "DateTime: " + date + '\t' + time);

                string shipDate = dateTime.Split('T')[0].Replace("-", string.Empty);
                mappedValues.Add(new Tuple<string, string, char>("_SHIPDATE_", shipDate, '#'));
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "SHIPDATE: " + shipDate);

                string shipTime = dateTime.Split('T')[1].Split('.')[0].Replace(":", string.Empty);
                mappedValues.Add(new Tuple<string, string, char>("_SHIPTIME_", shipTime, '#'));
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "SHIPTIME: " + shipTime);

                string shipDateShort = shipDate.Substring(2);
                mappedValues.Add(new Tuple<string, string, char>("_SHIPDATESHORT_", shipDateShort, '#'));
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "SHIPDATESHORT: " + shipDateShort);

                string shipTimeShort = shipTime.Substring(0, shipTime.Length - 2);
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "SHIPTIMESHORT: " + shipTimeShort);
                mappedValues.Add(new Tuple<string, string, char>("_SHIPTIMESHORT_", shipTimeShort, '#'));
            }

            return mappedValues;
        }

        public bool decodeManhattanMappings(List<Tuple<string, string, char>> mappedValues, Dictionary<string, List<string>> mappings, HashSet<string> orderHeaderTags, HashSet<string> orderDetailTags)
        {
            try
            {
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Mapped Values: " + mappedValues.Count);
                foreach (Tuple<string, string, char> mappedValue in mappedValues)
                {
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, mappedValue.Item1 + '\t' + mappedValue.Item2 + '\t' + mappedValue.Item3);
                    if (mappings.ContainsKey(mappedValue.Item1))
                    {
                        mappings[mappedValue.Item1].Add(mappedValue.Item2);
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "DETAIL " + orderDetailTags.Count + " FOUND ENTRY(" + mappings[mappedValue.Item1].Count + "):"  + mappedValue.Item1 + '\t' + mappedValue.Item2);
                    }
                    else
                    {
                        mappings.Add(mappedValue.Item1, new List<string>());
                        mappings[mappedValue.Item1].Add(mappedValue.Item2);
                        if(mappedValue.Item3 == '#')
                        {
                            orderHeaderTags.Add(mappedValue.Item1);
                        }
                        else
                        {
                            orderDetailTags.Add(mappedValue.Item1);
                        }
                    }
                    /*if (mappings.ContainsKey(mappedValue.Item1))
                    {
                        orderHeaderTags.Remove(mappedValue.Item1);
                        orderDetailTags.Add(mappedValue.Item1);
                        mappings[mappedValue.Item1].Add(mappedValue.Item2);
                        utilities.Write_To_Log(Utilities.Source.MappingToolSet, "DETAIL " + orderDetailTags.Count + " FOUND: " + mappedValue.Item1 + '\t' + mappedValue.Item2);
                    }
                    else
                    {
                        mappings.Add(mappedValue.Item1, new List<string>());
                        mappings[mappedValue.Item1].Add(mappedValue.Item2);
                        orderHeaderTags.Add(mappedValue.Item1);
                    }*/
                }
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + '\t' + ex.StackTrace);
                return false;
            }
        }

        public OrderHeader getManhattanOrderHeader(HashSet<string> orderHeaderTags, Dictionary<string, List<string>> mappings)
        {
            OrderHeader orderHeader = new OrderHeader();
            foreach (string header in orderHeaderTags)
            {
                orderHeader.dataAdd(header, mappings[header][0]);
            }
            return orderHeader;
        }

        public List<OrderDetail> getManhattanOrderDetails(HashSet<string> orderDetailTags, Dictionary<string, List<string>> mappings)
        {
            List<OrderDetail> orderDetailList = new List<OrderDetail>();
            int detailCount = 0;
            foreach (string detailHeader in orderDetailTags)
            {
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "detailHeader: " + detailHeader);
                int tmpCount = mappings[detailHeader].Count;
                if(tmpCount > detailCount)
                {
                    detailCount = tmpCount;
                }
            }
            int index = 0;
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "detailCount: " + detailCount);
            while (index < detailCount)
            {
                OrderDetail detail = new OrderDetail();
                foreach (string detailHeader in orderDetailTags)
                {
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, detailHeader);
                    if(index >= mappings[detailHeader].Count)
                    {
                        detail.dataAdd(detailHeader, mappings[detailHeader][mappings[detailHeader].Count - 1]);
                    }
                    else
                    {
                        detail.dataAdd(detailHeader, mappings[detailHeader][index]);
                    }
                    
                }
                orderDetailList.Add(detail);
                index++;
            }
            return orderDetailList;
        }

        public bool man2mitMapping(Configuration configuration, string inputPath, int fileIndex)
        {
            try
            {
                if (loadMitsuiMappings(configuration))
                {
                    createMitsuiTemplate(configuration);

                    XmlDocument input = new XmlDocument();
                    List<Tuple<string, string, char>> mappedValues = mapMitsuiFromSource(input, inputPath);
                    Dictionary<string, List<string>> mappings = new Dictionary<string, List<string>>();

                    HashSet<string> orderHeaderTags = new HashSet<string>();
                    HashSet<string> orderDetailTags = new HashSet<string>();
                    string filledFileName = null;
                    if (decodeManhattanMappings(mappedValues, mappings, orderHeaderTags, orderDetailTags))
                    {
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Decoding Mappings Successful");
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Header Tags: " + orderHeaderTags.Count + " Detail Tags: " + orderDetailTags.Count);

                        OrderHeader orderHeader = getManhattanOrderHeader(orderHeaderTags, mappings);
                        List<OrderDetail> orderDetailList = getManhattanOrderDetails(orderDetailTags, mappings);
                        //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Order Details: " + orderDetailList.Count);
                        
                        /*foreach(OrderDetail orderDetail in orderDetailList)
                        {
                            utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Order Detail" + i);
                            var od = orderDetail.dataGetAll();
                            foreach(string key in od.Keys)
                            {
                                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "\t" + key + '\t' + od[key]);
                            }
                        }*/
                        filledFileName = fillMitsuiTemplate(configuration, orderHeader, orderDetailList, fileIndex);
                    }

                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "FilledFileName: " + filledFileName);
                    if (filledFileName != null)
                    {
                        int tmpICN = Convert.ToInt32(configuration.interchangeControlNumber);
                        tmpICN++;
                        configuration.interchangeControlNumber = tmpICN.ToString("000000000");
                        filledFileName = (filledFileName.Split('.')[0] + '.' + configuration.manUpExt);
                        string processedFileName = configuration.mitsuiProcessedDir + filledFileName;
                        if (File.Exists(processedFileName))
                        {
                            File.Copy(processedFileName, processedFileName + "OLD", true);
                            File.Delete(processedFileName);
                        }
                        File.Move(inputPath, processedFileName);
                    }
                    else
                    {
                        utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Failed to fill template");
                        throw new Exception("Failed to fill template");
                    }
                }
                else
                {
                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Cannot Load Map");
                    throw new Exception("CANNOT LOAD MAP");
                }
                return true;
            }catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + ' ' + ex.StackTrace);
                return false;
            }
        }

        public bool createMitsuiTemplate(Configuration configuration)
        {
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "!!!CREATE MITSUI TEMPLATE");
            try
            {
                string mapPath = configuration.man2mitMapPath;
                string templateDir = configuration.templateDirectoryPath;
                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, templateDir + '\t' + mapPath);
                using (StreamReader sr = new StreamReader(mapPath))
                {
                    
                    string line;
                    StringBuilder output = new StringBuilder();
                    int index = 0;
                    string fileName;
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "CHECKING TITLE LINE");
                    if ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("_"))
                        {
                            Directory.CreateDirectory(templateDir);
                            String templateName = @"\" + line + ".tmplt";
                            String templatePath = templateDir + templateName;
                            configuration.mitsuiTemplate = line;
                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template name: " + templatePath);

                            Stack<String> sect = new Stack<String>();
                            sect.Push("FAILFAILFAIL");
                            fileName = line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.StartsWith("_"))
                                {
                                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] ILLEGAL MAP FORMAT - MULTIPLE ID LINES " + line);
                                }
                                else if (line.StartsWith("#"))
                                {
                                    char multiplier = line.ToCharArray()[line.Length - 1];
                                    String trimmed = line.Substring(1, line.Length - 2);
                                    String[] lineSplit = trimmed.Split(' ');
                                    String tag = lineSplit[0];
                                    String action = lineSplit[1];

                                    if (action.StartsWith("S"))
                                    {
                                        output.Append(tag + '*');
                                        index++;
                                        sect.Push(tag);
                                    }
                                    else
                                    {
                                        index--;
                                        //output.Remove(output.Length - 1, 1);
                                        output.AppendLine("~");
                                        sect.Pop();
                                    }
                                }
                                else if (!line.StartsWith("/"))
                                {
                                    String[] lineSplit = line.Split('&');
                                    String key = lineSplit[0];
                                    String value = lineSplit[1];

                                    if (value.StartsWith("/"))
                                    {
                                        output.Append('`' + sect.Peek() + '^' + key);
                                        output.Append("*");
                                    }
                                    else
                                    {
                                        output.Append(value);
                                        output.Append("*");
                                    }
                                }
                            }
                            using (StreamWriter sw = File.CreateText(templatePath))
                            {
                                sw.WriteLine(output.ToString());
                                Console.WriteLine("File Succesfully Created");
                                //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Template File Created: " + templatePath);
                                sw.Close();
                            }
                        }
                        else
                        {
                            utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] TITLE LINE INVALID");
                            throw new Exception("TITLE LINE INVALID");
                        }
                    }
                    else
                    {
                        utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] FIRST LINE NULL");
                        throw new Exception("FIRST LINE NULL");
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + '\t' + ex.StackTrace);
                return false;
            }
        }

        public bool loadMitsuiMappings(Configuration configuration)
        {
            ///utilities.Write_To_Log(Utilities.Source.MappingToolSet, "!!!LOAD MITSUI MAPPINGS");
            mappingMaster.Clear();
            try
            {
                string mapPath = configuration.man2mitMapPath;
                if (!File.Exists(mapPath))
                {
                    utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] Map Doesnt Exist: " + mapPath);
                    throw new Exception("File Open Error");
                }
                else
                {
                    //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Reading Map Data...");
                    using (StreamReader sr = new StreamReader(mapPath))
                    {
                        string line;
                        char multiplier = '!';
                        Stack<String> sect = new Stack<String>();
                        while ((line = sr.ReadLine()) != null)
                        {
                            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, line);
                            if (line.StartsWith("#"))
                            {

                                //Console.WriteLine(line);
                                multiplier = line.ToCharArray()[line.Length - 1];
                                String trimmed = line.Substring(1, line.Length - 2);
                                String[] lineSplit = trimmed.Split(' ');
                                String tag = lineSplit[0].Replace("#", String.Empty);
                                String action = lineSplit[1];

                                if (action.StartsWith("S"))
                                {
                                    sect.Push(tag);
                                }
                                else
                                {
                                    sect.Pop();
                                }
                            }
                            else if (!(line.StartsWith("/") || line.StartsWith("_")))
                            {
                                String[] split = line.Split('&');
                                String key = sect.Peek() + '^' + split[0];
                                String value = split[1];
                                //Console.WriteLine(key);
                                Tuple<string, char> valueSet = new Tuple<string, char>(value, multiplier);
                                mappingMaster.Add(key, valueSet);

                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + ' ' + ex.StackTrace);
                //Console.WriteLine(ex.StackTrace);
                //Console.WriteLine(ex.Message);
                return false;
            }
        }

        public XmlNodeList getMultipleTag(XmlDocument input, string xpath) { return input.SelectNodes(xpath); }
        public XmlNode getSingleTag(XmlDocument input, string xpath) { return input.SelectSingleNode(xpath); }

        public XmlNodeList getNSMultipleTag(XmlDocument input, string xpath) {
            var xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var outerXml = input.OuterXml;
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, outerXml);
            var matchCol = Regex.Matches(outerXml, xmlnsPattern);
            foreach (var match in matchCol)
                outerXml = outerXml.Replace(match.ToString(), "");
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, outerXml);
            XmlDocument tmp = new XmlDocument();
            tmp.LoadXml(outerXml);
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, "Multi-Nodes: " + tmp.SelectNodes(xpath).Count);
            return tmp.SelectNodes(xpath);
        }
        public XmlNode getNSSingleTag(XmlDocument input, string xpath) {
            var xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var outerXml = input.OuterXml;
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, outerXml);
            var matchCol = Regex.Matches(outerXml, xmlnsPattern);
            foreach (var match in matchCol)
                outerXml = outerXml.Replace(match.ToString(), "");
            //utilities.Write_To_Log(Utilities.Source.MappingToolSet, outerXml);
            XmlDocument tmp = new XmlDocument();
            tmp.LoadXml(outerXml);
            return tmp.SelectSingleNode(xpath);
        }

        private bool _inti_Manhattan_xml_file_(XmlDocument input, string inputPath)
        {
            try
            {
                input.Load(inputPath);
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + ' ' + ex.StackTrace);
                return false;
            }
        }

        private bool _inti_Mitsui_xml_file_(XmlDocument input, string inputPath)
        {
            try
            {
                input.Load(inputPath);
                input = RemoveAllNamespaces(input);
                return true;
            }
            catch (Exception ex)
            {
                utilities.Write_To_Log(Utilities.Source.MappingToolSet, "[ERROR] " + ex.Message + ' ' + ex.StackTrace);
                return false;
            }
        }

        public XmlDocument RemoveAllNamespaces(XmlNode documentElement)
        {
            var xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var outerXml = documentElement.OuterXml;
            var matchCol = Regex.Matches(outerXml, xmlnsPattern);
            foreach (var match in matchCol)
                outerXml = outerXml.Replace(match.ToString(), "");

            var result = new XmlDocument();
            result.LoadXml(outerXml);

            return result;
        }

    }
}
