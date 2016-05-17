using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edidev.FrameworkEDIx64;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Validate834File
{
    class Program
    {
        static void Main(string[] args)
        {
            ediDocument oEdiDoc = null;
            ediDataSegment oSegment = null;
            ediWarning oWarning = null;
            ediWarnings oWarnings = null;
            Int32 nWarningsCount;

            string schemaFilePath = string.Empty;
            string[] EdiFilePaths = new string[]{ };
            string errorLogsPath = string.Empty;
            string filesWithErrorsPath = string.Empty;
            string[] inputArguments = args;

            string getSchemaFilePath = string.Empty;
            string getEdiFilesPath = string.Empty;
            string getErrorLogsPath = string.Empty;
            string getFilesWithErrorsPath = string.Empty;

            if (args.Count() > 0)
            {
                 getSchemaFilePath = args[0].IndexOf(@"\") > -1 && IsValidFilename(args[0]) ? args[0] : string.Empty;
                 getEdiFilesPath = args[1].IndexOf(@"\") > -1 && IsValidFilename(args[1]) ? args[1] : string.Empty;
                 getErrorLogsPath = args[2].IndexOf(@"\") > -1 && IsValidFilename(args[2]) ? args[2] : string.Empty;
                 getFilesWithErrorsPath = args[3].IndexOf(@"\") > -1 && IsValidFilename(args[3]) ? args[3] : string.Empty;
            }

            try
            {
                schemaFilePath = !string.IsNullOrEmpty(getSchemaFilePath)? getSchemaFilePath : ConfigurationManager.AppSettings["schemaFilePath"].ToString();
                EdiFilePaths = !string.IsNullOrEmpty(getEdiFilesPath) ? Directory.GetFiles(getEdiFilesPath) : Directory.GetFiles(ConfigurationManager.AppSettings["ediFilesPath"].ToString());
                errorLogsPath = !string.IsNullOrEmpty(getErrorLogsPath) ? getErrorLogsPath : ConfigurationManager.AppSettings["errorLogsPath"].ToString();
                filesWithErrorsPath = !string.IsNullOrEmpty(getFilesWithErrorsPath) ? getFilesWithErrorsPath : ConfigurationManager.AppSettings["filesWithErrorsPath"].ToString();


                for (int i = 0; i <= EdiFilePaths.Count() - 1; i++)
                {
                    using (oEdiDoc = new ediDocument())
                    {
                        oEdiDoc.CursorType = DocumentCursorTypeConstants.Cursor_ForwardOnly;

                        oEdiDoc.LoadSchema(schemaFilePath, SchemaTypeIDConstants.Schema_Standard_Exchange_Format);
                        oEdiDoc.LoadEdi(EdiFilePaths[i]);

                        //iterate through each segment in the the EDI file so that they can be validated
                        ediDataSegment.Set(ref oSegment, oEdiDoc.FirstDataSegment);
                        while (oSegment != null)
                        {
                            ediDataSegment.Set(ref oSegment, oSegment.Next());
                        }

                        //check if FREDI found any errors
                        ediWarnings.Set(ref oWarnings, oEdiDoc.GetWarnings());
                        nWarningsCount = oWarnings.Count;

                        //display errors
                        if (nWarningsCount > 0)
                        {
                            string errorLogFile = errorLogsPath + @"\" + Path.GetFileNameWithoutExtension(EdiFilePaths[i]) + DateTime.Now.Ticks + ".txt";
                            using (FileStream fs = new FileStream(errorLogFile, FileMode.OpenOrCreate))
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.BaseStream.Seek(0, SeekOrigin.End);
                                sw.Write("Below is the list of errors for the file" + Environment.NewLine);
                                for (int j = 1; j <= nWarningsCount; j++)
                                {
                                    ediWarning.Set(ref oWarning, oWarnings.get_Warning(j));

                                    sw.Write("**************************************************************" + Environment.NewLine + Environment.NewLine);
                                    sw.Write(oWarning.Code + Environment.NewLine);
                                    sw.Write("**************************************************************" + Environment.NewLine + Environment.NewLine);
                                    sw.Write(oWarning.Description + Environment.NewLine);
                                    sw.Write("**************************************************************" + Environment.NewLine + Environment.NewLine);
                                    sw.Write(oWarning.ElementId + Environment.NewLine);
                                    sw.Write("**************************************************************" + Environment.NewLine + Environment.NewLine);
                                    sw.Write(oWarning.SegmentArea + Environment.NewLine);
                                    sw.Write("**************************************************************" + Environment.NewLine + Environment.NewLine);
                                }
                                sw.WriteLine(DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString());
                                sw.Flush();
                            }

                           // File.Move(EdiFilePaths[i], filesWithErrorsPath + Path.GetFileName(EdiFilePaths[i]));
                        }
                    }
                }

                Console.WriteLine("SuccessFully finished the writing errors to the file");
                Console.ReadKey();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

       public static bool IsValidFilename(string EdiFilePath)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()); ;
            string regexString = "[" + Regex.Escape("!@#$%^&*()+=?;:[]{}|`~<>,") + "]";
            Regex containsABadCharacter = new Regex(regexString);
            return containsABadCharacter.IsMatch(EdiFilePath) ? false : true;                    
        }
    }
}
