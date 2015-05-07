using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TKRain.Models;

namespace TKRain
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(path);
            if (!Directory.Exists(Path.Combine("Data", "Rain")))
                Directory.CreateDirectory(Path.Combine("Data", "Rain"));
            if (!Directory.Exists(Path.Combine("Data", "River")))
                Directory.CreateDirectory(Path.Combine("Data", "River"));
            if (!Directory.Exists(Path.Combine("Data", "Road")))
                Directory.CreateDirectory(Path.Combine("Data", "Road"));
            if (!Directory.Exists(Path.Combine("Data", "Dam")))
                Directory.CreateDirectory(Path.Combine("Data", "Dam"));
            if (!Directory.Exists("Config"))
                Directory.CreateDirectory("Config");

            Observation.ObservationIni();

            List<Task> ObsTask = new List<Task>();

            try {
                LoggerClass.NLogInfo("処理開始");
                var rainfall = new Rainfall();
                int number = rainfall.GetRainfallData();
                LoggerClass.NLogInfo("雨量処理件数: " + number + "件");
                ObsTask.Add(Observation.AmazonS3DirctoryUpload("Rain", 0));
            }
            catch(Exception e1)
            {
                LoggerClass.NLogInfo("雨量ルーチンエラー: " + e1.Message);
            }

            try
            {
                var riverLevel = new RiverLebel();
                riverLevel.GetRiverLevelData();
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("水位ルーチンエラー: " + e1.Message);
            }

            try
            {
                var roadWeather = new RoadWeather();
                roadWeather.GetRoadWeatherData();
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("道路気象ルーチンエラー: " + e1.Message);
            }

            try { 
                var damInfo = new DamInfo();
                damInfo.GetDamInfoData();
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("ダム情報ルーチンエラー: " + e1.Message);
            }

            Task.WaitAll(ObsTask.ToArray());
            LoggerClass.NLogInfo("処理終了");
        }
    }

    public class LoggerClass
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void NLogInfo(string message)
        {
            logger.Info(message);
        }

        public static void NLogError(string message)
        {
            logger.Error(message);
        }
    }

}
