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
            LoggerClass.NLogInfo("処理開始");
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
            DateTime prevObservationTime;

            try {
                if (Observation.IsUpdateRequired("RainfallObservationTime.text", out prevObservationTime))
                {
                    var rainfall = new Rainfall();
                    int number = rainfall.GetRainfallData(prevObservationTime);
                    LoggerClass.NLogInfo("雨量処理件数: " + number + "件");
#if !DEBUG
                    if(number > 0)
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Rain", 0));
#endif
                }
            }
            catch(Exception e1)
            {
                LoggerClass.NLogInfo("雨量ルーチンエラー: " + e1.Message);
            }

            try
            {
                if (Observation.IsUpdateRequired("RiverLevelObservationTime.text", out prevObservationTime))
                {
                    var riverLevel = new RiverLebel();
                    int number = riverLevel.GetRiverLevelData(prevObservationTime);
                    LoggerClass.NLogInfo("水位処理件数: " + number + "件");
#if !DEBUG
                    if(number > 0)
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("River", 0));
#endif
                }
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("水位ルーチンエラー: " + e1.Message);
            }

            try
            {
                if (Observation.IsUpdateRequired("RoadWeatherObservationTime.text", out prevObservationTime))
                {
                    var roadWeather = new RoadWeather();
                    int number = roadWeather.GetRoadWeatherData(prevObservationTime);
                    LoggerClass.NLogInfo("道路気象処理件数: " + number + "件");
#if !DEBUG
                    if(number > 0)
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Road", 0));
#endif
                }
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("道路気象ルーチンエラー: " + e1.Message);
            }

            try
            {
                if (Observation.IsUpdateRequired("DamObservationTime.text", out prevObservationTime))
                {
                    var damInfo = new DamInfo();
                    int number = damInfo.GetDamInfoData(prevObservationTime);
                    LoggerClass.NLogInfo("ダム情報処理件数: " + number + "件");
#if !DEBUG
                    if (number > 0)
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Road", 0));
#endif              
                }
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
