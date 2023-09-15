using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ProcessBallotFile
{
    internal class Program
    {
        private static string input = string.Empty;
        static void Main(string[] args)
        {
            int counter = 0;

            if (args.Count() != 4)
            {
                Console.WriteLine("Command example:");
                Console.WriteLine("ProcessBallotFile.exe args = Path\\file.txt ColumnToSearchForContinuousNumber IsThisInkjetFile?(true/false) NeedsSpecialSplit?(true/false");
                Console.WriteLine("ProcessBallotFile.exe C:\\Users\\User\\Desktop\\(File.txt) SortKey false true");
            }
            else
            {
                input = args[0];
                string column = args[1];
                bool isInkjet = Convert.ToBoolean(args[2]);
                bool needsDivider = Convert.ToBoolean(args[3]);                

                if (!input.Equals(""))
                {
                    Console.WriteLine($"Input File: {input}");
                    Console.WriteLine($"Searching column: {column}");
                    Console.WriteLine($"Is file processing an Inkjet file: {isInkjet}");

                    // isInkjet field to true if it is inkjet file
                    RecordProcessor rp = new RecordProcessor(input, column, isInkjet, needsDivider);

                    if (input.ToLower().EndsWith(".txt") || input.ToLower().EndsWith(".csv"))
                    {
                        if (File.Exists(input))
                        {
                            if (rp.RunJob(ref counter))
                            {
                                Console.WriteLine("Successfully processed...");
                            }
                            Console.WriteLine($"{rp.ProcessCounter} files successfully processed.");
                            ProcessErrors(rp);

                            rp.Dispose();
                            rp = null;
                            Console.WriteLine("Job completed");
                        }
                        else
                        {
                            Console.WriteLine("Process terminated: Input file could not be found");
                        }
                    }
                    else if (Directory.Exists(input))
                    {
                        if (rp.RunMultiJobs(ref counter))
                        {
                            Console.WriteLine("Successfully processed...");
                        }                        

                        Console.WriteLine($"{rp.ProcessCounter} files successfully processed.");
                        ProcessErrors(rp);

                        rp.Dispose();
                        Console.WriteLine("\nMulti-Job completed");
                    }
                }
                else
                {
                    Console.WriteLine("Process terminated: Input file could not be found");
                }
                
            }
            Console.WriteLine($"\nProcessed {counter} total records.\n");
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static void ProcessErrors(RecordProcessor rp)
        {
            Console.WriteLine($"\n{rp.ErrorCounter} files with errors.");

            try
            {
                if (rp.FileNameErrors.Count() > 0)
                {

                    if (!Directory.Exists($"{input}\\errors"))
                    {
                        Directory.CreateDirectory(input + "\\errors");
                    }

                    Console.WriteLine("**************************************************************************************************");
                    foreach (var item in rp.FileNameErrors)
                    {
                        if (File.Exists($"{input}\\errors\\{item}"))
                        {
                            File.Replace($"{input}\\{item}", $"{input}\\errors\\{item}", Directory.CreateDirectory(input + "\\errors\\backup").ToString());
                        }
                        else
                            File.Move($"{input}\\{item}", $"{input}\\errors\\{item}");

                        Console.WriteLine($"***({item})");
                    }

                    Console.WriteLine($"Files with errors have been moved to: {input}\\errors");
                    Console.WriteLine("**************************************************************************************************");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message} - {ex.StackTrace} - {new StackTrace().GetFrame(0).GetMethod().Name}");
                throw;
            }            
        }
    }
}
