using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DidoDownloader
{
    class Program
    {
        public static int DownloadPercentage { get; set; }
        public static ProgressBar DownloadProgressBar { get; set; }

        static void Main(string[] args)
        {
            string videpUrl = null;
            string key = "videoSourceUrl";
            bool hightQuality = false;
            string savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\didoVideos\\video-"+DateTime.Now.Ticks+".mp4";
            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\didoVideos\\");
            string command = null;
            if (args.Length == 0)
                DisplayHelp();
            for (int i = 0; i < args.Length; i++)
            {
                command = args[i];
                switch (command)
                {
                    case "-h":
                        DisplayHelp();
                        break;
                    case "-ver":
                        Assembly execAssembly = Assembly.GetCallingAssembly();
                        AssemblyName name = execAssembly.GetName();
                        Console.WriteLine("didoDownloader ");
                        Console.WriteLine("version " + name.Version);
                        break;
                    case "-url":
                        videpUrl = args[i + 1];
                        break;
                    case "-path":
                        savePath = args[i + 1];
                        break;
                    case "-key":
                        key = args[i + 1];
                        break;
                    case "-qul":
                        hightQuality = true;
                        break;
                }
            }
            if (videpUrl != null && savePath != null)
                initDido(videpUrl, savePath, key, hightQuality);
            Console.ReadLine();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Download Video From https://www.dideo.ir/");
            Console.WriteLine("author: https://github.com/asdword/didoDownloader");
            Console.WriteLine("\n parameters :");
            Console.WriteLine("__________________________\n");
            Console.WriteLine("\t -h \t\t Show Dido Help ");
            Console.WriteLine("\t -ver \t\t Show Version ");
            Console.WriteLine("\t -url \t\t Video Url From Dido ");
            Console.WriteLine("\t -path \t\t Your path for save download video file");
            Console.WriteLine("\t       \t\t by default save video files in 'Document\\didoVideos'");
            Console.WriteLine("\t -qul \t\t Try to get hight video resolution");
            Console.WriteLine("\t -key \t\t By default videoSourceUrl");
            Console.WriteLine("\n samples:");
            Console.WriteLine("\t dido -url[dido url]");
            Console.WriteLine("\t dido -url[dido url] -path[path save]");
            Environment.Exit(0);

        }

        public static void initDido(string videpUrl, string savePath, string key, bool hightQuality)
        {
            string pageContent = null;
            string tempUrl = null;
            string jsonStr = null;
            Console.WriteLine("init dido ...");
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(videpUrl);
            HttpWebResponse myres = (HttpWebResponse)myReq.GetResponse();

            using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
            {
                pageContent = sr.ReadToEnd();
            }

            if (pageContent.Contains(key))
            {
                Console.WriteLine("connected ^_^");
                tempUrl = pageContent.Substring(pageContent.IndexOf(key) + key.Length + 3);
                tempUrl = tempUrl.Substring(0, tempUrl.IndexOf(",") - 2);
                Console.WriteLine("json url:");
                Console.WriteLine(tempUrl);
                Console.WriteLine();

                myReq = (HttpWebRequest)WebRequest.Create(tempUrl);
                myres = (HttpWebResponse)myReq.GetResponse();
                using (StreamReader sr = new StreamReader(myres.GetResponseStream()))
                {
                    jsonStr = sr.ReadToEnd();
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                DidoModel result = serializer.Deserialize<DidoModel>(jsonStr);

                Console.WriteLine("==============================");
                int indexPlayable = 0;
                foreach (Playable item in result.Playable)
                {
                    Console.WriteLine("");
                    Console.WriteLine("id:" + ++indexPlayable);
                    Console.WriteLine("  title: " + item.Title);
                    Console.WriteLine("  resolution: " + item.Resolution);
                    Console.WriteLine("  format: " + item.Type);
                    Console.WriteLine("  url: " + item.Url);
                    Console.WriteLine("");
                }
                Console.WriteLine("==============================");
                Console.WriteLine("selected id: " + (hightQuality ? result.Playable.Count : 1));
                Uri selectedUrl = hightQuality ? result.Playable.Last().Url : result.Playable.First().Url;
                
                Download(selectedUrl, savePath);
            }
        }
        public static void Download(Uri url, string dist)
        {
            DownloadPercentage = 0;
            var options = new ProgressBarOptions
            {
                ProgressCharacter = '=',
                ProgressBarOnBottom = true
            };
            DownloadProgressBar = new ProgressBar(100, "", options);
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadDataCompleted += Wc_DownloadDataCompleted;
                wc.DownloadFileAsync(
                    new System.Uri(url.ToString()),
                    dist
                );
            }
        }

        private static void Wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            DownloadProgressBar.Dispose();
            Console.WriteLine("download conpleted !");
            Environment.Exit(0);
        }


        // Event to track the progress
        public static void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > DownloadPercentage)
            {
                DownloadPercentage = e.ProgressPercentage;
                DownloadProgressBar.Tick(string.Format("Received {0}kb from {1}kb", e.BytesReceived / 1024,e.TotalBytesToReceive / 1024));
                if (DownloadPercentage == 100)
                {
                    DownloadProgressBar.Dispose();
                    Console.WriteLine("download completed");
                    Environment.Exit(0);
                }
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
        public static void wc_DownloadProgressCompleted(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressBar.Dispose();
            Console.WriteLine("download completed");
        }
    }

    public partial class DidoModel
    {
        public List<Playable> Playable { get; set; }
        public List<object> Downloadable { get; set; }
        public List<Playable> TelegramPlayable { get; set; }
        public Uri Dideo { get; set; }
    }

    public partial class Playable
    {
        public Uri Url { get; set; }
        public long FormatId { get; set; }
        public string Type { get; set; }
        public long Format { get; set; }
        public long Resolution { get; set; }
        public string Title { get; set; }
        public string PersianTitle { get; set; }
    }

    public static class ObjectToDictionaryHelper
    {
        public static IDictionary<string, object> ToDictionary(this object source)
        {
            return source.ToDictionary<object>();
        }

        public static IDictionary<string, T> ToDictionary<T>(this object source)
        {
            if (source == null)
                ThrowExceptionWhenSourceArgumentIsNull();

            var dictionary = new Dictionary<string, T>();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
                AddPropertyToDictionary<T>(property, source, dictionary);
            return dictionary;
        }

        private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source, Dictionary<string, T> dictionary)
        {
            object value = property.GetValue(source);
            if (IsOfType<T>(value))
                dictionary.Add(property.Name, (T)value);
        }

        private static bool IsOfType<T>(object value)
        {
            return value is T;
        }

        private static void ThrowExceptionWhenSourceArgumentIsNull()
        {
            throw new ArgumentNullException("source", "Unable to convert object to a dictionary. The source object is null.");
        }
    }
}
