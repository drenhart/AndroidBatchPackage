using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public class FileManager {
        private static readonly ILog log = LogManager.GetLogger(typeof(FileManager));

        public void TraverseFolder() {
            if (!Directory.Exists(AppConfig.WorkingDir)) {
                throw new DirectoryNotFoundException("working directory not found");
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(AppConfig.WorkingDir);
            foreach (DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories()) {
                // 31152 -- C:\Users\soumi\Desktop\323TEST\31152
                //log.Info($"{subDirectoryInfo.Name} -- {subDirectoryInfo.FullName}");

                if (File.Exists(Path.Combine(subDirectoryInfo.FullName, "apks", "signed.aab"))) {
                    log.Info($"signed.aab exists in: {subDirectoryInfo.FullName}");
                    continue;
                }

                // D:\workspace\smtech\AppPublish2\Android\英语\31156-D
                string channelFolderPath = SearchDirectories(subDirectoryInfo.Name);
                log.Info($"channelFolderPath: {channelFolderPath}");
                var dirInfo = new DirectoryInfo($@"{subDirectoryInfo.FullName}\apks");
                string aabFileName = string.Empty;
                foreach (FileInfo fileInfo in dirInfo.GetFiles()) {
                    if (fileInfo.Extension == ".aab") {
                        aabFileName = fileInfo.Name;
                        log.Info($"aab file name: {aabFileName}");
                    }
                }
                if (string.IsNullOrEmpty(aabFileName)) {
                    throw new Exception("aab file not found");
                }

                string jksFilePath = $@"{channelFolderPath}\打包\{subDirectoryInfo.Name}.jks";

                var apkManager = new ApkManager(subDirectoryInfo.FullName);
                apkManager.GetSignedAab(aabFileName, channelFolderPath, jksFilePath);
            }
        }

        private string SearchDirectories(string channel) {
            try {
                foreach (string dir in Directory.GetDirectories(AppConfig.AppPublishDir, "*", SearchOption.AllDirectories)) {
                    if (dir.Contains(channel)) {
                        return dir;
                    }
                }
                return null;
            } catch (Exception e) {
                // Access denied to the directory.
                log.Error(e);
                return null;
            }
        }

    }
}
