using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TKRain.Models
{
    public class LoggerClass
    {
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
