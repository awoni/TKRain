using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TKRain.Models;

namespace TKRain
{
    public class Program
    {
        public static IConfigurationRoot Configuration;

        private static readonly string[] _rainFiles = { "RainData.json", "Rainfall.xml", "Rainfall.json", "Rainfall.geojson" };
        private static readonly string[] _riverFiles = { "RiverData.json", "RiverLevel.xml", "RiverLevel.json", "RiverLevel.geojson" };
        private static readonly string[] _roadFiles = { "RoadData.json", "RoadWeather.xml", "RoadWeather.json", "RoadWeather.geojson" };
        private static readonly string[] _roadSeries = {"1-1001.json", "3-1002.json", "4-1003.json", "5-1004.json", "7-1005.json", "8-1006.json" };
        private static readonly string[] _damFiles = { "DamData.json", "DamInfo.xml", "DamInfo.json"};
        private static readonly string[] _tideFiles = { "TideData.json"};

        public static void Main(string[] args)
        {
            string os = "x";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = "Windows";

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{os}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            AppInit.SetupIni(Configuration);

            if (args.Length > 0)
            {
                foreach (string s in args)
                {
                    //データの保存に必要なフォルダーの作成
                    //観測所情報の取得
                    if (s == "/s")
                    {
                        AppInit.DirectoryIni();
                        Stations stations = new Stations();
                        stations.GetStationInformations();
                        LoggerClass.LogInfo("/s 処理終了");
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
                        LoggerClass.LogInfo("/r 処理終了");
                        return;
                    }

                    //テスト
                    if (s == "/t")
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        Encoding enc = Encoding.GetEncoding(932);
                        File.WriteAllText(Path.Combine(AppInit.DataDir, "RoadDaily", "test.txt"), "Shift-JISテスト", enc);
                        File.WriteAllText(Path.Combine(AppInit.DataDir, "RoadDaily", "test-utf.txt"), "Shift-JISテスト");
                        return;
                    }
                }
            }
            LoggerClass.LogInfo("処理開始");

            List<Task> ObsTask = new List<Task>();
            DateTime prevObservationTime;
            List<WeatherRain> weatherRainList = new List<WeatherRain>();
            try
            {
                if (Observation.IsUpdateRequired("RainfallObservationTime.txt", out prevObservationTime))
                {
                    var rainfall = new Rainfall();
                    int number = rainfall.GetRainfallData(prevObservationTime, weatherRainList);
                    if (number > 0)
                    {
                        LoggerClass.LogInfo("雨量処理件数: " + number + "件");

                        ObsTask.Add(Observation.AmazonS3ListUpload("Rain", _rainFiles));

                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.LogInfo("雨量ルーチンエラー: " + e1.Message);
            }

            try
            {
                if (Observation.IsUpdateRequired("RiverLevelObservationTime.txt", out prevObservationTime))
                {
                    var riverLevel = new RiverLevel();
                    int number = riverLevel.GetRiverLevelData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.LogInfo("水位処理件数: " + number + "件");

                        ObsTask.Add(Observation.AmazonS3ListUpload("River", _riverFiles));
                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.LogInfo("水位ルーチンエラー: " + e1.Message);
            }

            try
            {
                if (Observation.IsUpdateRequired("RoadWeatherObservationTime.txt", out prevObservationTime))
                {
                    var roadWeather = new RoadWeather();
                    List<string> filenames = new List<string>();
                    int number = roadWeather.GetRoadWeatherData(prevObservationTime, weatherRainList, filenames);
                    if (number > 0)
                    {
                        LoggerClass.LogInfo("道路気象処理件数: " + number + "件");
                        ObsTask.Add(Observation.AmazonS3ListUpload("Road", _roadFiles));
                        
                        ObsTask.Add(Observation.AmazonS3ListUpload("Road", _roadSeries));
                        if (filenames.Any())
                            ObsTask.Add(Observation.AmazonS3ListUpload("RoadDaily", filenames.ToArray()));
                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.LogInfo("道路気象ルーチンエラー: " + e1.Message);
            }

            //ダム情報
            try
            {
                if (Observation.IsUpdateRequired("DamObservationTime.txt", out prevObservationTime))
                {
                    var damInfo = new DamInfo();
                    int number = damInfo.GetDamInfoData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.LogInfo("ダム情報処理件数: " + number + "件");

                        ObsTask.Add(Observation.AmazonS3ListUpload("Dam", _damFiles));

                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.LogInfo("ダム情報ルーチンエラー: " + e1.Message);
            }

            //潮位
            try
            {
                if (Observation.IsUpdateRequired("TideLevelObservationTime.txt", out prevObservationTime))
                {
                    var tideLevel = new TideLevel();
                    int number = tideLevel.GetTideLevelData(prevObservationTime);
                    if (number > 0)
                    {
                        LoggerClass.LogInfo("潮位処理件数: " + number + "件");

                        ObsTask.Add(Observation.AmazonS3ListUpload("Tide", _tideFiles));

                    }
                }
            }
            catch (Exception e1)
            {
                LoggerClass.LogInfo("潮位ルーチンエラー: " + e1.Message);
            }

            Task.WaitAll(ObsTask.ToArray());
            LoggerClass.LogInfo("処理終了");
        }
    }
}
