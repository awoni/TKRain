# TKRain
徳島県県土防災情報管理システムでは、雨量・水位・潮位等のデータがCC-BYでXMLファイルにより提供されています。このプログラムは、そのデータをJavaScriptから利用しやすいように、JSON及びGeoJSON形式のデータに変換するプログラムです。

また、データーを累積して時系列データも作成できるようにしています。

S3 にデータをアップロードしたい場合の appsettings.json の例

```appsettings.json
{
  "Host": "http://www1.road.pref.tokushima.jp",
  "DataDir": "C:\\Data\\TKRain",
  "AWSAccessKey": "xxxxx",
  "AWSSecretKey": "xxxxx",
  "BucketName": "tkrain"
}
```


