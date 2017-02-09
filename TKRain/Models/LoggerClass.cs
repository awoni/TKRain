using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TKRain.Models
{
    public class LoggerClass
    {
        public static void Ini(string baseParh)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(baseParh, "logs", DateTime.Now.ToString("yyyy-MM-dd") + ".log"))
                .CreateLogger();
        }
        public static void LogInfo(string message)
        {
            Log.Information(message);
        }

        public static void LogError(string message)
        {
            Log.Error(message);
        }
    }
}
