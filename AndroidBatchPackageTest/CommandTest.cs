using AndroidBatchPackage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AndroidBatchPackageTest;

[TestClass]
public class CommandTest {
    [TestMethod]
    public void ConvertAabToApksTest() {
        ApkManager obj = new ApkManager(@"C:\Users\soumi\Desktop\323TEST\31156\apks");
        Type type = typeof(ApkManager);
        MethodInfo? method = type.GetMethod("ConvertAabToApks", BindingFlags.NonPublic | BindingFlags.Instance);
        string aabFileName = "31156-casualfinder-1.4.5-2023-03-29-release.aab";
        string jksFilePath = @"D:\workspace\smtech\AppPublish2\Android\英语\31156-D\打包\31156.jks";
        string? newApks = method?.Invoke(obj, new object[] { aabFileName, "newApks.apks", jksFilePath, AppConfig.jksPassword, AppConfig.jksAlias }) as string;
        Console.WriteLine($"new apks name is: {newApks}");

        Assert.IsTrue(newApks == "newApks.apks");
    }


}

