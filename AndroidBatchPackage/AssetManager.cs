using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public class AssetManager {
        private static readonly ILog log = LogManager.GetLogger(typeof(AssetManager));
        private string DirectoryPath { get; set; }
        private string ChannelFolderPath { get; set; }
        private string DecompileFolderName { get; set; }
        public AssetManager(string directoryPath, string channelFolderPath, string decompileFolderName) {
            DirectoryPath = directoryPath;
            ChannelFolderPath = channelFolderPath;
            DecompileFolderName = decompileFolderName;
        }

        public void Replace() {
            ChangeStringsFile();
            DeleteAvd();
            ChangeIconAndLaunchBackground();
        }

        private void ChangeStringsFile() {
            log.Info("step: change strings.xml");

            string stringXmlpath = $@"{DirectoryPath}\{DecompileFolderName}\res\values\strings.xml";

            string keyFilePath = @$"{ChannelFolderPath}\打包\key.txt";
            var keyFile = new KeyFile(keyFilePath);
            string newAppName = keyFile.Values.AppName;
            log.Info($"new app name: {newAppName}");

            var regex = new Regex(@"<string name=""app_name"">(?<appName>[^<]+)</string>");
            //string str = "<string name=\"app_name\">HookUpAndroid</string>";
            var text = File.ReadAllText(stringXmlpath);
            text = regex.Replace(text, $"<string name=\"app_name\">{newAppName}</string>");
            File.WriteAllText(stringXmlpath, text);
        }

        private void DeleteAvd() {
            log.Info("step: delete avd");

            string stringXmlpath = $@"{DirectoryPath}\{DecompileFolderName}\res\values\public.xml";
            var lines = File.ReadAllLines(stringXmlpath);
            log.Debug($"public xml: {string.Join(",", lines)}");
            File.WriteAllLines(stringXmlpath, lines.Where(l => !l.Contains("$avd")));
        }

        private void ChangeIconAndLaunchBackground() {
            log.Info("step: replace icon and launch background");

            // 将AppPublish中的文件夹复制到res中的文件夹覆盖
            string resFolderPath = Path.Combine(DirectoryPath, DecompileFolderName, "res");
            List<DirectoryInfo> directoryInfos = SearchDirectorieInfos(ChannelFolderPath, "mipmap");
            if (directoryInfos.Count == 0) {
                throw new Exception($"icon folder not found in {ChannelFolderPath}");
            }
            foreach (DirectoryInfo directoryInfo in directoryInfos) {
                string source = Path.Combine(directoryInfo.FullName, "ic_launcher.png");
                string target = Path.Combine(resFolderPath, directoryInfo.Name, "ic_launcher.png");
                File.Copy(source, target, true);
                string roundSource = Path.Combine(directoryInfo.FullName, "ic_launcher_round.png");
                string roundTarget = Path.Combine(resFolderPath, directoryInfo.Name, "ic_launcher_round.png");
                File.Copy(roundSource, roundTarget, true);
            }

            // launch_background
            List<DirectoryInfo> drawableDirectoryInfos = SearchDirectorieInfos(ChannelFolderPath, "drawable");
            if (drawableDirectoryInfos == null) {
                throw new Exception($"launch_background folder not found in {ChannelFolderPath}");
            }
            foreach (DirectoryInfo directoryInfo in drawableDirectoryInfos) {
                string source = Path.Combine(directoryInfo.FullName, "launch_background.png");
                string target = Path.Combine(resFolderPath, directoryInfo.Name, "launch_background.png");
                File.Copy(source, target, true);
            }
        }

        private List<DirectoryInfo> SearchDirectorieInfos(string directoryPath, string folderName) {
            try {
                var directoryInfos = new DirectoryInfo(directoryPath).GetDirectories("*", SearchOption.AllDirectories);
                List<DirectoryInfo> list = new List<DirectoryInfo>();
                foreach (DirectoryInfo directoryInfo in directoryInfos) {
                    if (directoryInfo.Name.Contains(folderName)) {
                        list.Add(directoryInfo);
                    }
                }
                return list; ;
            } catch (Exception e) {
                // Access denied to the directory.
                log.Error(e);
                return null;
            }
        }


    }
}
