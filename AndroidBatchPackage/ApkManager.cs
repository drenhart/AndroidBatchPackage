using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public class ApkManager {
        private static readonly ILog log = LogManager.GetLogger(typeof(ApkManager));
        private string DirectoryPath { get; set; }

        public ApkManager(string directoryPath) {
            DirectoryPath = $@"{directoryPath}\apks";
        }

        public void GetSignedAab(string aabFileName, string channelFolderPath, string jksFilePath) {
            string newApks = ConvertAabToApks(aabFileName, "newApks.apks", jksFilePath, AppConfig.jksPassword, AppConfig.jksAlias);
            string apkName = ConvertApksToApk(newApks);
            string decompileFolderName = DecodeApk(apkName, "decompile_apk");
            var assetManager = new AssetManager(DirectoryPath, channelFolderPath, decompileFolderName);
            assetManager.Replace();
            string resZipFile = CompileResFolder(decompileFolderName, "res.zip");
            string baseZipFile = Link(resZipFile, "base.zip", decompileFolderName, AppConfig.minSdkVersion, AppConfig.maxSdkVersion);
            string baseFolderPath = UnzipAndMoveFileToDirectory(baseZipFile, decompileFolderName);
            string baseFolderZipFile = ZipBaseFolder("baseFolder.zip", baseFolderPath);
            string baseAabFile = ConvertBaseZipToBaseAab(baseFolderZipFile, baseFolderPath, "base.aab");
            string signedAabFile = SignAab(baseFolderPath, baseAabFile, "signed.aab", jksFilePath, AppConfig.jksPassword, AppConfig.jksAlias);
            VerifySign(baseFolderPath, signedAabFile);

            //DeleteUselessFile();
        }

        public void DeleteUselessFile() {
            log.Info("step: delete useless files");

            string signedAabPath = Path.Combine(DirectoryPath, "base", "signed.aab");
            if (File.Exists(signedAabPath)) {
                string newSignedAabPath = Path.Combine(DirectoryPath, "signed.aab");
                File.Copy(signedAabPath, newSignedAabPath);
            } else {
                throw new Exception("signed.aab not exist");
            }

            try {
                DirectoryInfo directoryInfo = new DirectoryInfo(DirectoryPath);
                foreach (FileInfo fileInfo in directoryInfo.GetFiles()) {
                    List<string> excludeList = new List<string> { ".aab", ".json", ".txt"};
                    if (!excludeList.Contains(fileInfo.Extension)) {
                        fileInfo.Delete();
                    }
                }
                foreach (DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories()) {
                    subDirectoryInfo.Delete(true); // 删除所有子文件夹和其中的文件
                }
                log.Info("delete file success");
            } catch (Exception e) {
                log.Info($"delete file fail: {e}");
            }
        }

        private string ConvertAabToApks(string inputAabName, string outputApksName, string jksFilePath, string jksFilePassword, string keyAlias) {
            log.Info("step: convert aab to apks");

            try {
                string format = "-jar {0} build-apks --bundle={1} --output={2} --mode=universal --ks {3} --ks-pass pass:{4} --ks-key-alias {5}";
                string args = string.Format(format, AppConfig.BundleToolJarFilePath, inputAabName, outputApksName, jksFilePath, jksFilePassword, keyAlias);
                var result = ShellUtil.Execute(AppConfig.JavaBinPath, args, DirectoryPath);
                log.Info($"bundletool execute code: {result}");
            } catch (Exception ex) {
                log.Error($"bundletool execute error: {ex}");
            }
            return outputApksName;
        }

        private string ConvertApksToApk(string apksFileName) {
            log.Info("step: convert apks to apk");

            // rename
            var fileName = apksFileName.Split(".", StringSplitOptions.RemoveEmptyEntries)[0];
            var apksFullPath = $@"{DirectoryPath}\{apksFileName}";
            var zipPath = $@"{DirectoryPath}\{fileName}.zip";
            if (File.Exists(apksFullPath)) {
                File.Move(apksFullPath, zipPath);
            } else {
                log.Error("apks file not exists");
                throw new Exception("apks file not exists");
            }

            // unzip -> universal.apk
            FastZip fastZip = new FastZip();
            fastZip.ExtractZip(zipPath, DirectoryPath, null);
            return "universal.apk";
        }

        private string DecodeApk(string apkName, string folderName) {
            log.Info("step: using apkTool decode apk");

            try {
                string format = "d {0} -s -o {1} -f";
                string args = string.Format(format, apkName, folderName);
                var result = ShellUtil.Execute(AppConfig.ApkToolPath, args, DirectoryPath, 30);
                log.Info($"apktool execute result: {result}");
            } catch (Exception ex) {
                log.Error($"apktool execute error: {ex}"); 
            }
            return folderName;
        }

        private string CompileResFolder(string folderName, string zipFileName) {
            log.Info("step: using aapt2 compile res folder");

            try {
                string format = @"compile --dir {0}\res -o {1}";
                string args = string.Format(format, folderName, zipFileName);
                var result = ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\aapt2", args, DirectoryPath);
                log.Info($"aapt2 compile result: {result}");
            } catch (Exception ex) {
                log.Error($"aapt execute error: {ex}");
            }
            return zipFileName;
        }

        private string Link(string inputZipName, string outputZipName, string folderName, int minSdkVersion, int maxSdkVersion) {
            log.Info("step: using aapt2 link resources");

            try {
                string format = @"link --proto-format -o {0} -I {1} --manifest {2}\AndroidManifest.xml --min-sdk-version {3} --target-sdk-version {4} --version-code 1 --version-name 1.0 -R {5} --auto-add-overlay";
                string args = string.Format(format, outputZipName, AppConfig.AndroidJarFilePath, folderName, minSdkVersion, maxSdkVersion, inputZipName);
                var result = ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\aapt2", args, DirectoryPath);
                log.Info($"aapt execute result: {result}");
            } catch (Exception ex) {
                log.Error($"aapt execute error: {ex}");
            }
            return outputZipName;
        }

        private string UnzipAndMoveFileToDirectory(string zipFileName, string decompileFolderName) {
            log.Info("step: refractor base folder");

            // unzip
            string targetFolderName = zipFileName.Split(".").FirstOrDefault();
            string zipPath = $@"{DirectoryPath}\{zipFileName}";
            FastZip fastZip = new FastZip();
            fastZip.ExtractZip(zipPath, $@"{DirectoryPath}\{targetFolderName}", null);

            // move dir
            string baseFolderPath = Path.Combine(DirectoryPath, targetFolderName);
            string manifestFilePath = Path.Combine(baseFolderPath, "AndroidManifest.xml");
            string manifestFolderPath = Path.Combine(baseFolderPath, "manifest");
            if (!Directory.Exists(manifestFolderPath)) {
                Directory.CreateDirectory(manifestFolderPath);
            }
            File.Move(manifestFilePath, Path.Combine(manifestFolderPath, "AndroidManifest.xml"));

            string newAssetsFolderPath = Path.Combine(baseFolderPath, "assets");
            string assetsFolderPath = Path.Combine(DirectoryPath, decompileFolderName, "assets");
            if (Directory.Exists(assetsFolderPath)) {
                CopyDirectory(assetsFolderPath, newAssetsFolderPath);
            } else {
                Directory.CreateDirectory(newAssetsFolderPath);
            }

            string newLibFolderPath = Path.Combine(baseFolderPath, "lib");
            string libFolderPath = Path.Combine(DirectoryPath, decompileFolderName, "lib");
            CopyDirectory(libFolderPath, newLibFolderPath);

            string newRootFolderPath = Path.Combine(baseFolderPath, "root");
            string unkownFolderPath = Path.Combine(DirectoryPath, decompileFolderName, "unknown");
            CopyDirectory(unkownFolderPath, newRootFolderPath);

            string newKotlinFolderPath = Path.Combine(baseFolderPath, "root", "kotlin");
            string kotlinFolderPath = Path.Combine(DirectoryPath, decompileFolderName, "kotlin");
            CopyDirectory(kotlinFolderPath, newKotlinFolderPath);

            string dexFolderPath = Path.Combine(baseFolderPath, "dex");
            if (!Directory.Exists(dexFolderPath)) {
                Directory.CreateDirectory(dexFolderPath);
            }
            string decompileFolderPath = Path.Combine(DirectoryPath, decompileFolderName);
            var dexFiles = new DirectoryInfo(decompileFolderPath).GetFiles("*.dex", SearchOption.TopDirectoryOnly);
            foreach (var dexFile in dexFiles) {
                dexFile.CopyTo(Path.Combine(dexFolderPath, dexFile.Name));
            }

            return baseFolderPath;
        }

        private string ZipBaseFolder(string outputZipFileName, string baseFolderPath) {
            log.Info("step: zip base folder");

            try {
                string args = $"cMf {outputZipFileName} manifest dex res root lib assets resources.pb";
                var result = ShellUtil.Execute("jar", args, baseFolderPath);
                log.Info($"zip execute result: {result}");
            } catch (Exception ex) {
                log.Error($"zip error: {ex}");
            }
            return outputZipFileName;
        }

        private string ConvertBaseZipToBaseAab(string zipFileName, string baseFolderPath, string aabFileName) {
            log.Info("step: base.zip to base.aab");

            try {
                string format = "-jar {0} build-bundle --modules={1} --output={2}";
                string args = string.Format(format, AppConfig.BundleToolJarFilePath, zipFileName, aabFileName);
                var result = ShellUtil.Execute(AppConfig.JavaBinPath, args, baseFolderPath);
                log.Info($"bundletool execute code: {result}");
            } catch (Exception ex) {
                log.Error($"bundletool error: {ex}");
            }
            return aabFileName;
        }

        private string SignAab(string baseFolderPath, string inputAabFileName, string outputAabFileName, string jksFilePath, string jksFilePassword, string keyAlias) {
            log.Info("step: sign aab");

            try {
                string format = "-verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore {0} -storepass {1} -signedjar {2} {3} {4}";
                string args = string.Format(format, jksFilePath, jksFilePassword, outputAabFileName, inputAabFileName, keyAlias);
                var result = ShellUtil.Execute("jarsigner", args, baseFolderPath);
            } catch (Exception ex) {
                log.Error($"sign error: {ex}");
            }
            return outputAabFileName;
        }

        private string VerifySign(string baseFolderPath, string aabFileName) {
            log.Info("step: verify signed aab");

            try {
                string format = "-verify -verbose {0}";
                string args = string.Format(format, aabFileName);
                var result = ShellUtil.Execute("jarsigner", args, baseFolderPath);
            } catch (Exception ex) {
                log.Error($"sign error: {ex}");
            }
            return "";
        }

        private void CopyDirectory(string sourceDir, string targetDir) {
            DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDir);
            DirectoryInfo targetDirectory = new DirectoryInfo(targetDir);

            // 如果目标文件夹不存在则创建
            if (!Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
            }

            // 拷贝所有文件
            foreach (FileInfo file in sourceDirectory.GetFiles()) {
                string targetPath = Path.Combine(targetDirectory.FullName, file.Name);
                file.CopyTo(targetPath, true);
            }

            // 递归拷贝所有子文件夹
            foreach (DirectoryInfo sourceSubDir in sourceDirectory.GetDirectories()) {
                string targetSubDir = Path.Combine(targetDirectory.FullName, sourceSubDir.Name);
                CopyDirectory(sourceSubDir.FullName, targetSubDir);
            }
        }




        private string BuildApk(string inputFolderName, string outputApkName) {
            try {
                var code = ShellUtil.Execute(AppConfig.ApkToolPath, $"b {inputFolderName} --use-aapt2 -o {outputApkName}", DirectoryPath, 150);
                log.Info($"build apk with execute code: {code}");
            } catch (Exception ex) {
                log.Error($"apktool execute error: {ex}");
            }
            return outputApkName;
        }

        private string ZipAlign(string inputApkName, string outputApkName) {
            log.Info("zip align is executing");
            try {
                var code = ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\zipalign.exe", $"-p -f -v 4 {inputApkName} {outputApkName}", DirectoryPath, 30);
                log.Info($"zip align execute code is {code}");
            } catch (Exception ex) {
                log.Error($"zip align execute error: {ex}");
            }
            return outputApkName;
        }

        private string ValidZipAlign(string inputApkName) {
            string result = string.Empty;
            try {
                result = ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\zipalign.exe", $"-c -v 4 {inputApkName}", DirectoryPath, 30);
                log.Info($"valid zip align execute code: {result}");
            } catch (Exception ex) {
                log.Error($"zip align execute error: {ex}");
            }
            return result;
        }

        private void ApkSigner(string jksFilePath, string jksFilePassword, string inputApkName, string outputApkName) {
            try {
                string result = ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\apksigner.bat", $"sign --ks {jksFilePath} --ks-pass pass:{jksFilePassword} --out {outputApkName} {inputApkName}", DirectoryPath, 30);
                log.Info($"apk signer execute result: {result}");
                log.Info($"output apk name: {outputApkName}");
            } catch (Exception ex) {
                log.Error($"apk signer execute error: {ex}");
            }
        }

        private void ValidApkSigner() {
            try {
                ShellUtil.Execute($@"{AppConfig.AndroidSdkToolPath}\apksigner.bat", "verify .\\abc2.apk", DirectoryPath);
            } catch (Exception ex) {
                log.Error($"valid apk signer execute error: {ex}");
            }
        }



    }
}
