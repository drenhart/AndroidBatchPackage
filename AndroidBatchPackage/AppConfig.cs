using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public static class AppConfig {
        public static string WorkingDir = @"C:\Users\soumi\Desktop\323TEST";
        public static string AppPublishDir = @"D:\workspace\smtech\AppPublish2\Android\";
        public static string BundleToolJarFilePath = @"D:\workspace\smtech\tool_set\Android\AndroidBatchPackage\bundletool-all-1.14.0.jar";
        public static string ApkToolPath = @"D:\workspace\smtech\tool_set\Android\AndroidBatchPackage\apktool.bat";
        public static string JavaBinPath = @"C:\Program Files\Java\jdk1.8.0_202\bin\java.exe";
        public static string AndroidJarFilePath = @"C:\Users\soumi\AppData\Local\Android\Sdk\platforms\android-30\android.jar";
        public static string AndroidSdkToolPath = @"C:\Users\soumi\AppData\Local\Android\Sdk\build-tools\30.0.2";
        public static string GitBinPath = @"C:\Program Files\Git\cmd\git.exe";
        public static string GitUserHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static int minSdkVersion = 21;
        public static int maxSdkVersion = 32;
        public static string jksPassword = "1234qwer";
        public static string jksAlias = "key0";
    }

}
