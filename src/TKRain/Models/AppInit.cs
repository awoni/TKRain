using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKRain.Models
{
    public static class AppInit
    {

        public static string AWSAccessKey { get; set; }
        public static string AWSSecretKey { get; set; }
        public static string BucketName { get; set; }
        public static string DataDir { get; set; }

        public static void SetupIni(IConfigurationRoot configuration)
        {
            AWSAccessKey = configuration["AWSAccessKey"];
            AWSSecretKey = configuration["AWSSecretKey"];
            BucketName = configuration["BucketName"];
            DataDir = configuration["DataDir"];
        }

        public static void DirectoryIni()
        {
            if (!Directory.Exists(Path.Combine(DataDir, "Rain")))
                Directory.CreateDirectory(Path.Combine(DataDir, "Rain"));
            if (!Directory.Exists(Path.Combine(DataDir, "River")))
                Directory.CreateDirectory(Path.Combine(DataDir, "River"));
            if (!Directory.Exists(Path.Combine(DataDir, "Road")))
                Directory.CreateDirectory(Path.Combine(DataDir, "Road"));
            if (!Directory.Exists(Path.Combine(DataDir, "RoadDaily")))
                Directory.CreateDirectory(Path.Combine(DataDir, "RoadDaily"));
            if (!Directory.Exists(Path.Combine(DataDir, "Dam")))
                Directory.CreateDirectory(Path.Combine(DataDir, "Dam"));
            if (!Directory.Exists(Path.Combine(DataDir, "Tide")))
                Directory.CreateDirectory(Path.Combine(DataDir, "Tide"));
            if (!Directory.Exists(Path.Combine(DataDir, "Config")))
                Directory.CreateDirectory(Path.Combine(DataDir, "Config"));
        }
    }
}
