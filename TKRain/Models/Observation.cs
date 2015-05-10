﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Xml.Serialization;

//徳島県　雨量、水位情報
//最新の観測データ http://www1.road.pref.tokushima.jp/c6/xml92100/00000_00000_00001.xml
//
// 雨量：　00001
// 水位：　00004
// 道路気象：　00302
// ダム諸量：　00007

//所在地を取得するXML
//http://www1.road.pref.tokushima.jp/c6/xml90000/00000_00000_00000.xml

namespace TKRain.Models
{
    class Observation
    {
        private static string AWSAccesskey;
        private static string AWSSecretKey;
        private static string BucketName;


        public static void ObservationIni()
        {
            AWSAccesskey = Properties.Settings.Default.AWSAccessKey;
            AWSSecretKey = Properties.Settings.Default.AWSSecretKey;
            BucketName = Properties.Settings.Default.BucketName;
        }

        public static T TgGetStream<T>(string url, int ntry)
        {
            try
            {
                using (var client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                 | DecompressionMethods.Deflate
                }))
                {
                    Stream xml = client.GetStreamAsync(url).Result;
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(xml);
                }
            }
            catch(Exception e1)
            {
                ++ntry;
                if (ntry< 3)
                {
                    System.Threading.Thread.Sleep(30);
                    return TgGetStream<T>(url, ++ntry);
                }
                else
                {
                    LoggerClass.NLogInfo("データの取得に失敗しました。" + e1.Message + " URL: " +  url);
                    return default(T);
                }
            }
        }

        public static void SaveToXml(string path, object data, int ntry)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(data.GetType());
            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(fs, data);
                }
            }
            catch
            {
                ++ntry;
                if (ntry < 3)
                {
                    System.Threading.Thread.Sleep(30);
                    SaveToXml(path, data, ntry);
                }
                else
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    LoggerClass.NLogInfo("XMLファイルの保存に失敗しました。ファイル名: " + path);
                }
            }
        }

        //AmazonのS3へのアップロード
        public async Task AmazonS3Upload(string path, int ntry)
        {
            var s3Client = AWSClientFactory.CreateAmazonS3Client(AWSAccesskey, AWSSecretKey, RegionEndpoint.APNortheast1);
            try
            {
                var utility = new TransferUtility(s3Client, new TransferUtilityConfig());
                await utility.UploadAsync(path, BucketName);
                /*
                //S3のパケットポリシーでGetObjectの設定をしておくことで対応
                //http://stackoverflow.com/questions/7420209/amazon-s3-permission-problem-how-to-set-permissions-for-all-files-at-once
                s3Client.PutACL(new PutACLRequest
                {
                    BucketName = BucketName,
                    Key = filename,
                    CannedACL = S3CannedACL.PublicRead
                });
                */
            }
            catch (Exception e1)
            {
                if (ntry < 3)
                    await AmazonS3Upload(path, ++ntry);
                else
                    LoggerClass.NLogError("EC2アップロードエラー: " + e1.Message);
            }
        }

        public static async Task AmazonS3DirctoryUpload(string name, int ntry)
        {
            var s3Client = AWSClientFactory.CreateAmazonS3Client(AWSAccesskey, AWSSecretKey, RegionEndpoint.APNortheast1);
            try
            {
                var utility = new TransferUtility(s3Client, new TransferUtilityConfig());
                await utility.UploadDirectoryAsync(Path.Combine("Data", name), BucketName + "/" + name);
            }
            catch (Exception e1)
            {
                if (ntry < 3)
                    await AmazonS3DirctoryUpload(name, ++ntry);
                else
                    LoggerClass.NLogError("EC2アップロードエラー: " + e1.Message);
            }
        }

        public static bool IsUpdateRequired(string filename, out DateTime PrevObservationTime)
        {
            try
            {
                PrevObservationTime = DateTime.Parse(File.ReadAllText(Path.Combine("data", filename)));
                //10分ごとに更新
                if ((DateTime.Now - PrevObservationTime).Ticks >= 6000000000L)
                    return true;
                return false;
            }
            catch
            {
                PrevObservationTime = default(DateTime);
                return true;
            }
        }

        public static double? StringToDouble(string s)
        {
            double d;
            if (double.TryParse(s, out d))
                return d;
            return null;
        }
    }


    public class StationInfoList : List<StationInfo> { }

    public class StationInfo
    {
        /// 事務所コード
        public int ofc { get; set; }
        /// 観測局コード
        public int obc { get; set; }
        /// 観測局名称
        public string obn { get; set; }
        /// 緯度
        public double lat { get; set; }
        /// 経度
        public double lng { get; set; }
    }

    public class RainStationList : List<RainStationInfo> { }
    public class RainStationInfo: StationInfo
    {
        /// 所在地
        public string obl { get; set; }
    }

    /// 雨量状況図のデータ
    /// ここからは観測所の一データを取得
    public class SList
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Sym")]
        public List<SListSym> Sym { get; set; }
    }

    /// <remarks/>
    public class SListSym
    {
        /// <remarks/>
        //public string Cd { get; set; }
        /// 事務所コード、観測所コード
        public string Ocb { get; set; }
        /// <remarks/>
        //public byte Itm { get; set; }
        /// 観測所名称
        public string Nm { get; set; }
        /// <remarks/>
        //public string Data { get; set; }
        /// 座標（平面直角座標）
        public string Pt { get; set; }
        /// <remarks/>
        //public byte Eki {get; set; }
    }

    /// 観測データ詳細
    public class od
    {
        /// 観測時刻
        public string ot { get; set; }
        /// 観測データ
        public string ov { get; set; }
        /// 観測ステータス
        public string os { get; set; }
        /// 観測ステータス（0:正常, 1:未収集, 2:欠測, 9:初期状態）
        public int osi { get; set; }
        /// 警戒値コード
        public string cc { get; set; }
        /// 表示色情報を示す文字列
        public string dc { get; set; }
        /// 観測データの単位
        public string un { get; set; }
    }
}




