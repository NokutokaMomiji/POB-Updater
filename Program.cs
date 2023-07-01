using System.IO.Compression;
using System.Net;
using System.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace POBUpdater
{
    class Program
    {
        static string appdataLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        static string appdataFolder = appdataLocation + "/Project_One_Bullet";
        static string hashFile = appdataFolder + "/ch";
        const string gameURL = "https://liatestingground.000webhostapp.com/data/";
        static string gameHash = gameURL + "info.txt";
        static string gameFile = gameURL + "latest.zip";
        static string storedHash = "";
        static bool shouldWait = false;
        
        static WebClient client = new WebClient();

        static bool ShouldDownload() {
            try
            {
                Console.WriteLine("> 1) Yes, update the game.");
                Console.WriteLine("> 2) No, skip this release.");
                string input = Console.ReadLine();
                if (input == "1")
                    return true;
                else if (input == "2")
                    return false;
                else
                    return ShouldDownload();
            }
            catch (Exception e) {
                return false;
            }
        }

        static string GetOnlineHash() {
            Stream stream = client.OpenRead(gameHash);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        static bool EqualHashes() {
            Console.WriteLine(hashFile);
            if (!File.Exists(hashFile))
                return false;
            
            string localHash = File.ReadAllText(hashFile);
            storedHash = GetOnlineHash();
            Console.WriteLine("> localHash: {0}", localHash);
            Console.WriteLine("> onlineHash: {0}", storedHash);

            return (localHash == storedHash);
        }

        static void CreateAppdataFolder() {
            if (!Directory.Exists(appdataFolder)) {
                Directory.CreateDirectory(appdataFolder);
                Console.WriteLine("> Setting up...");
            }
        }

        static void DownloadGame() {
            Console.WriteLine("> Downloading update...");
            //client.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
            using (var wc = new WebClient()) {
                wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(CompletedDownload);
                wc.DownloadFileAsync(new Uri(gameFile), "latest.zip");
            }
            shouldWait = true;
        }

        static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e) {
            if (e.ProgressPercentage == 100)
            {
                Console.WriteLine("> Downloaded {0} bytes / {1} bytes.", e.BytesReceived, e.TotalBytesToReceive);
            }
            else
            {
                Console.WriteLine("> Downloading {0} bytes / {1} bytes. {2}%", e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
            }
        }

        static void CompletedDownload(object sender, AsyncCompletedEventArgs e) {
            if (e.Cancelled) {
                shouldWait = false;
                Console.WriteLine("> [ERROR]: Download was cancelled.");
                return;
            }

            if (e.Error != null) {
                shouldWait = false;
                Console.WriteLine("> [ERROR]: An error occurred while downloading the update.");
                Console.WriteLine("> " + e.Error.Message);
                return;
            }
            Console.WriteLine("> Completed download!");
            if (Directory.Exists("bin"))
                Directory.Delete("bin", true);
            ZipFile.ExtractToDirectory("latest.zip", "bin");
            File.WriteAllText(hashFile, storedHash);
            if (File.Exists("latest.zip"))
                File.Delete("latest.zip");
            shouldWait = false;
        }

        static void RunGame() {
            Console.WriteLine("\n> Opening game.");
            if (!Directory.Exists("bin")) {
                Console.WriteLine("> [ERROR]: GAME DATA MISSING! PLEASE REINSTALL GAME.");
                Console.WriteLine("> [Press Enter to exit...]");
                Console.ReadLine();
                return;
            }
            System.Diagnostics.Process.Start("bin/Project One Bullet.exe");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("\n|----------------[Project: One Bullet]----------------|\n");
            Console.WriteLine("> Welcome to the Project: One Bullet Development Build.");
            
            CreateAppdataFolder();
            //Console.WriteLine("> Setup Completed!");

            try {
                if (!EqualHashes()) {
                    Console.WriteLine("> |------[UPDATE DETECTED!]------|");
                    Console.WriteLine("> An update for the game was detected. Do you wish to install the update?\n");
                    Console.WriteLine("> [WARNING]: Keep in mind that, if you update the game, the current game files will be overwritten INCLUDING THE ORIGINAL GAME MAP FILES.");
                    Console.WriteLine("> [WARNING]: Please make a backup of these if you have modified them.");
                    Console.WriteLine("> [WARNING]: Any custom maps and save file data will remain unaffected.");
                    Console.WriteLine("\n> Do you wish to proceed?");
                    if (ShouldDownload())
                        DownloadGame();
                    
                    while (shouldWait) {}
                }
            }
            catch (Exception e) {
                Console.WriteLine("> [ERROR]: An unexpected exception has occurred. Continuing...");
            }

            RunGame();
        }
    }
}