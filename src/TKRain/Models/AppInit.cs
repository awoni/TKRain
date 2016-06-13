// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace TKRain.Models
{
    public static class AppInit
    {
        public static string Host { get; private set; }
        public static string AWSAccessKey { get; private set; }
        public static string AWSSecretKey { get; private set; }
        public static string BucketName { get; private set; }
        public static string DataDir { get; private set; }

        public static void SetupIni(IConfigurationRoot configuration)
        {
            ApplicationEnvironment env = PlatformServices.Default.Application;
            LoggerClass.Ini(env.ApplicationBasePath);

            Host = configuration["Host"];
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
