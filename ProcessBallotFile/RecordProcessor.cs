using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ProcessBallotFile
{
    internal class RecordProcessor
    {
        private const int ballotIndex = 4;
        private const int orderIndex = 5;
        private const int fileSize = 10000;
        private const int stackSize = 2500;
        private const int boxQty = 500;
        private const char delimiter = '|';
        private bool isDisposing = false;
        private bool _isInkjet;
        private string _input;
        private string _column;
        private bool _needsDivider;
        private string fileNameNew;
        private StreamWriter streamWriter;
        private double numberOfFiles;
        private double numberOfBoxes;
        private double numberOfStacks;
        private int rowCount;
        private int fileNum;
        private int stackNum;
        private int boxNum;
        private int _counter = 0;
        

        public RecordProcessor(string input, string column, bool isInkjet, bool needsDivider)
        {
            _input = input;
            _column = column;
            _isInkjet = isInkjet;
            _needsDivider = needsDivider;
        }

        public int ProcessCounter { get; set; }
        public int ErrorCounter { get; set; }
        public List<string> FileNameErrors { get; set; } = new List<string>();
        public bool IsErrors { get; set; } = false;

        private void HandleCatchErrors(Exception ex)
        {
            Console.WriteLine("**************************************************************************************************\n");
            Console.WriteLine($"Exception: {ex.Message} - {ex.StackTrace} - {new StackTrace().GetFrame(0).GetMethod().Name}");
            Console.WriteLine("\n**************************************************************************************************");

            FileNameErrors.Add(fileNameNew);
            --ProcessCounter;
            ++ErrorCounter;
            IsErrors = true;
        }

        public bool RunMultiJobs(ref int counter)
        {
            string[] files = Directory.GetFiles(_input);
            int taskId = 1;

            Console.WriteLine($"{files.Length} files processing...");

            try
            {
                Thread.Sleep(1000);
                foreach (var item in files)
                {
                    boxNum = 1;
                    fileNum = 0;
                    stackNum = 0;

                    if (File.Exists(item))
                    {
                        _input = item;

                        RunJob(ref counter);
                        Console.WriteLine($"Job running: Job# {taskId++}");
                    }
                    Thread.Sleep(1000);                    
                }

                return true;
            }
            catch (Exception ex)
            {                
                HandleCatchErrors(ex);
                return false;
            }
        }

        public bool RunJob(ref int counter)
        {
            // Take input file to string array
            string[] linesArray = File.ReadAllLines(_input);
            string[] fileSplitArray = new string[fileSize];
            string header = linesArray[0];

            // determine how many files
            numberOfFiles = Math.Ceiling((double)linesArray.Length / fileSize);
            // determine how many boxes
            numberOfBoxes = Math.Ceiling((double)linesArray.Length / boxQty);
            // determine how many stacks
            numberOfStacks = Math.Ceiling((double)linesArray.Length / stackSize);

            try
            {
                for (int i = 1; i < linesArray.Length; i++)
                {
                    if (i == (((int)numberOfFiles - 1) * fileSize) + 1)
                    {
                        int lastArrayLength = linesArray.Length - (((int)numberOfFiles - 1) * fileSize);
                        fileSplitArray = new string[lastArrayLength - 1];
                    }

                    fileSplitArray[rowCount] = linesArray[i];
                    rowCount++;

                    if (rowCount == fileSize || rowCount == fileSplitArray.Length)
                    {
                        ProcessBallots(fileSplitArray, header, _column, i);
                        fileSplitArray = new string[fileSize];
                        rowCount = 0;
                        counter = _counter;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                HandleCatchErrors(ex);
                return false;
            }

        }

        private void ProcessBallots(string[] lines, string header, string column, int records)
        {                         
            string[] columnLines = header.Split(delimiter);        
            int seqIndex = Array.IndexOf(columnLines, column);
            int ballotTypeIndex = Array.IndexOf(columnLines, "BallotStyle");
            int envIdIndex = Array.IndexOf(columnLines, "EnvID");
            int imageIndex = Array.IndexOf(columnLines, "BallotImageFront");
            int jobNumIndex = Array.IndexOf(columnLines, "JobNumber");
            int orderQty = 1;
            int count = 0;
            string boxDivider = "";
            string dividerCard = "";
            string stackDivider = "";

            try
            {
                // create fileName
                FileInfo file = new FileInfo(_input);
                fileNameNew = $"{Path.GetFileNameWithoutExtension(file.FullName)}-{++fileNum} of {numberOfFiles}.txt";

                using (streamWriter = new StreamWriter($"{file.Directory}\\{fileNameNew}".ToLower()))
                {
                    // print header to output file
                    streamWriter.WriteLine(header);

                    // iterate through each line and for each line run it as many times as quantity states updating sequence numbers
                    foreach (var line in lines)
                    {
                        bool isDivider = false;

                        if (line != header)
                        {
                            try
                            {
                                columnLines = line.Split(delimiter);
                                string countyID = columnLines[0];
                                // envId is the 1-N number per job
                                int envId = Convert.ToInt32(columnLines[envIdIndex]);
                                string ballotImage = columnLines[imageIndex];
                                string jobNum = columnLines[jobNumIndex];
                                string ballotType = columnLines[ballotTypeIndex].ToString();
                                // recordId is the 1-N number that is coninuous through the whole run.
                                int recordId = Convert.ToInt32(columnLines[seqIndex]);
                                if (count < 1)
                                {
                                    boxDivider = $"{countyID}|||BOX DIVIDER|Box#: {boxNum}|Boxes: {boxNum} - {numberOfBoxes}||KHAKI|BoxDividerFront.pdf|BoxDividerBack.pdf|KHAKI|KHAKI" +
                                                $"|||||||||||||||||||||||||||||0|||||{records}|{records + boxQty}|0|||0|||||BOD_ABS|";

                                    streamWriter.WriteLine(boxDivider);

                                    if (_needsDivider)
                                    {
                                        dividerCard = $"{countyID}|||{jobNum}|1|1|1|DIVIDER|{ballotImage}|{ballotImage}|KHAKI|KHAKI" +
                                        $"|||||||||||||||||||||||||||||{ballotType}|||||{records}|{records + boxQty}|0|||0|||||BOD_ABS|";

                                        streamWriter.WriteLine(dividerCard);
                                    }
                                }

                                if (count > 0 && _needsDivider)
                                {
                                    string[] prevLine = lines[count - 1].Split(delimiter);

                                    string prevBT = prevLine[ballotTypeIndex].ToString();

                                    if (ballotType != prevBT)
                                    {
                                        dividerCard = $"{countyID}|||{jobNum}|1|1|1|DIVIDER|{ballotImage}|{ballotImage}|KHAKI|KHAKI" +
                                            $"|||||||||||||||||||||||||||||{ballotType}|||||{records}|{records + boxQty}|0|||0|||||BOD_ABS|";

                                        streamWriter.WriteLine(dividerCard);
                                    }
                                }

                                // TODO refactor for divider sheets (Could this whole if statement be moved to the bottom???)
                                if (recordId % boxQty == 0 && !_isInkjet)
                                {
                                    boxNum++;
                                    streamWriter.WriteLine(line);
                                    isDivider = true;

                                    if (recordId % stackSize == 0 || count + 1 == lines.Length - 1)
                                    {
                                        // create STACK divider sheet record
                                        stackDivider = $"{countyID}|||STACK|STACK: {++stackNum} of {numberOfStacks}|||KHAKI|StackDividerFront.pdf|StackDividerBack.pdf|KHAKI|KHAKI" +
                                            $"|||||||||||||||||||||||||||||0|||||{recordId}|{fileSize}|0|||0|||||BOD_ABS|";

                                        streamWriter.WriteLine(stackDivider);
                                    }
                                    if (recordId % records != 0)
                                    {
                                        // create BOX divider sheet record
                                        boxDivider = $"{countyID}|||BOX DIVIDER|Box#: {boxNum}|Boxes: {boxNum} - {numberOfBoxes}||KHAKI|BoxDividerFront.pdf|BoxDividerBack.pdf|KHAKI|KHAKI" +
                                        $"|||||||||||||||||||||||||||||0|||||{recordId}|{recordId + boxQty}|0|||0|||||BOD_ABS|";

                                        streamWriter.WriteLine(boxDivider);
                                    }
                                }

                                orderQty = Convert.ToInt32(columnLines[orderIndex]);

                                if (orderQty > 1)
                                {
                                    CopyRecordsOfOrderQty(columnLines, orderQty);
                                }
                                else if (!isDivider) streamWriter.WriteLine(line);

                                count++;
                                _counter++;
                            }
                            catch (Exception ex)
                            {
                                HandleCatchErrors(ex);
                                streamWriter.Dispose();
                                break;
                            }

                        }
                    }
                }
                ++ProcessCounter;
            }
            catch (Exception ex)
            {
                HandleCatchErrors(ex);
                throw;
            }
        }

        private void CopyRecordsOfOrderQty(string[] arr, int orderNum)
        {
            try
            {
                int ballotStartNum = Convert.ToInt32(arr[orderIndex - 1]);
                for (int i = 0; i < orderNum; i++)
                {
                    string print = "";
                    arr[ballotIndex] = ballotStartNum.ToString();

                    for (int j = 0; j < arr.Length; j++)
                    {
                        if (j < arr.Length - 1) print += arr[j] + delimiter;
                        else print += arr[j];
                    }
                    streamWriter.WriteLine(print);
                    ballotStartNum++;
                }
            }
            catch (Exception ex)
            {
                HandleCatchErrors(ex);
                streamWriter.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!isDisposing)
                {
                    if (streamWriter != null)
                    {
                        streamWriter.Close(); 
                    }

                    isDisposing = true;
                }
            }
        }
    }
}
