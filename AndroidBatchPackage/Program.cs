
using log4net.Config;
using log4net;
using System.Reflection;

namespace AndroidBatchPackage {

    public class Program {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args) {
            InitLog4Net();

            new FileManager().TraverseFolder();
        }

        public static void InitLog4Net() {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

    }

}


