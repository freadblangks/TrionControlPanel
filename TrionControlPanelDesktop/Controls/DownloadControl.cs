﻿using System.Diagnostics;
using System.IO.Compression;
using System.Security.Policy;
using TrionControlPanelDesktop.FormData;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace TrionControlPanelDesktop.Controls
{
    public partial class DownloadControl : UserControl
    {
        private static bool ListFull = false;
        // List of URLs to download
        private static List<URLList> DownloadList = new();
        // Counting Downloaded URLs
        private int TotalDownloads = 0;
        private int CurrentDownload = 0;
        public DownloadControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }
        static List<string> ReadLinesFromString(string inputString)
        {
            // Split the inputString into lines using newline characters
            string[] linesArray = inputString.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Create a new list and add the lines to it
            List<string> linesList = new List<string>(linesArray);

            return linesList;
        }
        public async static Task AddToList(string Weblink)
        {
            using (HttpClient client = new())
            {
                if (!string.IsNullOrEmpty(Weblink))
                {
                    // Download the text file content
                    string fileContent = await client.GetStringAsync(Weblink);
                    List<string> strings = ReadLinesFromString(fileContent);
                    // Split the file content by comma and add each item to the list
                    foreach (var lines in strings)
                    {
                        string[] Entry = lines.Split(',');

                        URLList newUrlList = new();
                        
                        newUrlList.FileName = Entry[0];
                        if (Entry[1].Contains("1drv.ms"))
                        { newUrlList.FileWebLink = UIData.DownloadOneDriveAPI(Entry[1]);}
                        else{ newUrlList.FileWebLink = Entry[1];}
                        newUrlList.Zipfile = int.Parse(Entry[2]);
                        DownloadList.Add(newUrlList);
                        MessageBox.Show($"{Entry[0]} \n {Entry[1]} \n {Entry[2]}");
                    }
                    ListFull = true;
                }
                else
                {
                    //error!
                }
            }
        }
        private async Task Download()
        {
            string downloadDirectory = Directory.GetCurrentDirectory();
            using (HttpClient client = new())
            {
                foreach (var url in DownloadList)
                {
                    TotalDownloads = DownloadList.Count;
                     // Update Downloaded URLs
                    CurrentDownload++;
                   
                    try
                    {
                        int Unzip;
                        // Send GET request to the server
                        using (HttpResponseMessage response = await client.GetAsync(url.FileWebLink, HttpCompletionOption.ResponseHeadersRead))
                        {
                            // Update Label
                            LBLQueue.Text = $@"{CurrentDownload} / {TotalDownloads}";
                            LBLStatus.Text = "Status: Connect!";
                            // Check if request was successful
                            response.EnsureSuccessStatusCode();
                            LBLStatus.Text = "Status: Read File!";
                            // Get the file name from the URL
                            string fileName = Path.GetFileName(url.FileName);
                            string downloadPath = Path.Combine(downloadDirectory, fileName + ".zip");
                            LBLDownloadName.Text = @$"Name: {fileName}";
                            LBLStatus.Text = "Status: Prepare Download!";
                            Unzip = url.Zipfile;
                            // Create a file stream to write the downloaded content
                            using (FileStream fileStream = new(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                // Get the response stream
                                using (Stream stream = await response.Content.ReadAsStreamAsync())
                                {
                                    byte[] buffer = new byte[8192]; // 8 KB buffer
                                    int bytesRead;
                                    long totalBytesRead = 0;
                                    long totalDownloadSize = response.Content.Headers.ContentLength ?? -1;
                                    Stopwatch stopwatch = Stopwatch.StartNew();
                                    // Read from the stream in chunks and write to the file stream
                                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                    {
                                        LBLStatus.Text = "Status: Downloading!";
                                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                        totalBytesRead += bytesRead;
                                        // Calculate download speed
                                        double elapsedTimeSeconds = stopwatch.Elapsed.TotalSeconds;
                                        double speedMBps = totalBytesRead / 1024 / 1024 / elapsedTimeSeconds; // bytes to MBps
                                        // Display progress
                                        double totalDownloadSizeMB = (double)totalDownloadSize / 1024 / 1024; // bytes to MB
                                        double totalBytesReadMB = (double)totalBytesRead / 1024 / 1024; // bytes to MB
                                        // Update PBar Maximum Value
                                        PBARDownload.LabelText = "MB";
                                        PBARDownload.Maximum = (int)totalDownloadSizeMB;
                                        PBARDownload.Value = (int)totalBytesReadMB;
                                        LBLDownloadSize.Text = $@"Size: {totalDownloadSizeMB:F2} MB";
                                        LBLDownloadSpeed.Text = $@"Speed: {speedMBps:F2}MBps";
                                    }
                                }
                            }
                            LBLStatus.Text = "Status: Done!";
                            // Delay task so we dont get Still in use error
                            await Task.Delay(1500);
                            if(Unzip== 1)
                            {
                                await UnzipFileAsync(Path.Combine(Directory.GetCurrentDirectory(), url.FileName + ".zip"), Directory.GetCurrentDirectory());
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        private async Task UnzipFileAsync(string zipFilePath, string extractPath)
        {
            try
            {
                PBARDownload.Maximum = 100;
                PBARDownload.LabelText = "%";
                LBLStatus.Text = "Status: Prepare to Unpackage!";
                using (FileStream zipFileStream = File.OpenRead(zipFilePath))
                {
                    LBLStatus.Text = "Status: Read File!";
                    using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
                    {
                        long totalBytes = 0;
                        long extractedBytes = 0;
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            totalBytes += entry.Length;
                        }
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string fullPath = Path.Combine(extractPath, entry.FullName);
                            string? directoryName = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                            {
                                LBLStatus.Text = "Status: Prepare directorys!";
                                Directory.CreateDirectory(directoryName); // Create directory if it doesn't exist
                            }
                            if (!entry.Name.Equals(string.Empty))
                            {
                                LBLStatus.Text = "Status: Write Data!";
                                LBLFIleName.Text = @$"File: {entry.Name}";
                                using (Stream entryStream = entry.Open())
                                using (FileStream outputStream = File.Create(fullPath))
                                {
                                    byte[] buffer = new byte[4096]; //8 KB buffer
                                    int bytesRead;
                                    while ((bytesRead = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        // Calculate writing speed
                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                        double speedMbps = (extractedBytes / (1024 * 1024)) / elapsedSeconds;

                                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                                        extractedBytes += bytesRead;
                                        double progress = (double)extractedBytes / totalBytes * 100;
                                        PBARDownload.Value = (int)progress;
                                        LBLDownloadSpeed.Text = $@"Speed: {speedMbps:F2}MBps";
                                    }
                                }
                            }
                        }
                    }
                }
                LBLStatus.Text = "Unzip operation completed successfully.";
                File.Delete(zipFilePath);
                if(TotalDownloads == CurrentDownload)
                {
                    MainForm.LoadDownloadControl();
                    MainForm.StartDirectoryScan(Directory.GetCurrentDirectory());
                } 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        private void TimerWacher_Tick(object sender, EventArgs e)
        {
            if(ListFull == false)
            {
                TimerDownloadStart.Start();
            }
        }
        private void DownloadControl_Load(object sender, EventArgs e)
        {
            LBLQueue.Text = $@"{TotalDownloads} / {DownloadList.Count}";
        }
        private async void TimerDownloadStart_Tick(object sender, EventArgs e)
        {
            if(ListFull == true)
            {
                TimerDownloadStart.Stop();
                await Download();
            }
        }
    }
}
