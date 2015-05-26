// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

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
            if (args.Length > 0)
            {
                foreach(string s in args)
                {
                    //データの保存に必要なフォルダーの作成
                    //観測所情報の取得
                    if( s== "/s")
                    {
                        Observation.SetupIni();
                        Stations stations = new Stations();
                        stations.GetStationInformations();
                        LoggerClass.NLogInfo("/s 処理終了");
                        return;
                    }
                    
                    //観測所データの書き込み
                    //初期設定の場合は、パラメータなしで実行させて累積データを作成後に実行
                    if (s == "/r")
                    {
                        var rainfall = new Rainfall();
                        rainfall.SetRainInfo();
                        var riverLevel = new RiverLevel();
                        riverLevel.SetRiverInfo();
                        var damInfo = new DamInfo();
                        damInfo.SetDamInfo();
                        var tideLevel = new TideLevel();
                        tideLevel.SetTideInfo();
                        var roadWeather = new RoadWeather();
                        roadWeather.SetRoadInfo();
                        LoggerClass.NLogInfo("/r 処理終了");
                        return;
                    }
                }
            }
            LoggerClass.NLogInfo("処理開始");

            Observation.ObservationIni();
            List<Task> ObsTask = new List<Task>();
            DateTime prevObservationTime;
            List<WeatherRain> weatherRainList = new List<WeatherRain>();
            try {
                if (Observation.IsUpdateRequired("RainfallObservationTime.text", out prevObservationTime))
                {
                    var rainfall = new Rainfall();
                    int number = rainfall.GetRainfallData(prevObservationTime, weatherRainList);
                    if (number > 0)
                    {
                        LoggerClass.NLogInfo("雨量処理件数: " + number + "件");
#if !DEBUG
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Rain", 0));
#endif
                    }
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
                    var riverLevel = new RiverLevel();
                    int number = riverLevel.GetRiverLevelData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.NLogInfo("水位処理件数: " + number + "件");
#if !DEBUG
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("River", 0));
#endif
                    }
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
                    bool dailyDataUpLoad;
                    var roadWeather = new RoadWeather();
                    int number = roadWeather.GetRoadWeatherData(prevObservationTime, weatherRainList, out dailyDataUpLoad);
                    if (number > 0)
                    {
                        LoggerClass.NLogInfo("道路気象処理件数: " + number + "件");
#if !DEBUG
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Road", 0));
                        if(dailyDataUpLoad)
                            ObsTask.Add(Observation.AmazonS3DirctoryUpload("Weather", 0));
#endif
                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("道路気象ルーチンエラー: " + e1.Message);
            }

            //ダム情報
            try
            {
                if (Observation.IsUpdateRequired("DamObservationTime.text", out prevObservationTime))
                {
                    var damInfo = new DamInfo();
                    int number = damInfo.GetDamInfoData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.NLogInfo("ダム情報処理件数: " + number + "件");
#if !DEBUG
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Dam", 0));
#endif
                    }            
                }
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("ダム情報ルーチンエラー: " + e1.Message);
            }

            //潮位
            try
            {
                if (Observation.IsUpdateRequired("TideLevelObservationTime.text", out prevObservationTime))
                {
                    var tideLevel = new TideLevel();
                    int number = tideLevel.GetTideLevelData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.NLogInfo("潮位処理件数: " + number + "件");
#if !DEBUG
                        ObsTask.Add(Observation.AmazonS3DirctoryUpload("Tide", 0));
#endif
                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.NLogInfo("潮位ルーチンエラー: " + e1.Message);
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
