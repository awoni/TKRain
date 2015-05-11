// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.
var damUrl = "http://tk.ecitizen.jp/Data/Dam/";
function setDam() {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var data = JSON.parse(xmlhttp.responseText);
            damSummaryTable(data, damUrl + "DamData.json");
        }
    };
    xmlhttp.open("GET", damUrl + "DamData.json", true);
    xmlhttp.send();
}
function setDamData() {
    var regex = new RegExp("[\\?&]station=([^&#]*)");
    var results = regex.exec(location.search);
    if (results === null) {
        window.location.href = "Dam.html";
        return;
    }
    var station = results[1];
    damGetDetail(station);
}
function damSummaryTable(data, url) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>ダム名</th><th>貯水位</th><th>貯水量</th><th>貯水率</th><th>流入量</th><th>放流量</th><th>流域平均10分間雨量</th><th>流域平均時間雨量</th><th>流域平均累計雨量</th><th>リンク</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10_val + '</td><td>' + data.hr[i].d20_val + '</td><td>' + data.hr[i].d40_val + '</td><td>' + data.hr[i].d50_val + '</td><td>' + data.hr[i].d70_val + '</td><td>' + data.hr[i].d10010_10m_val + '</td><td>' + data.hr[i].d10010_1h_val + '</td><td>' + data.hr[i].d10070_val + '</td><td><a href="DamData.html?station=' + data.hr[i].ofc + '-' + data.hr[i].obc + '">リンク</a></td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id01").innerHTML = out;
}
function damGetDetail(place) {
    var xmlhttp1 = new XMLHttpRequest();
    var url = damUrl + place + ".json";
    xmlhttp1.onreadystatechange = function () {
        if (xmlhttp1.readyState == 4 && xmlhttp1.status == 200) {
            var data = JSON.parse(xmlhttp1.responseText);
            damDetailTable(data, url);
        }
    };
    xmlhttp1.open("GET", url, true);
    xmlhttp1.send();
}
function damDetailTable(data, url) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>ダム名</th><th>貯水位</th><th>貯水量</th><th>貯水率</th><th>流入量</th><th>放流量</th><th>流域平均10分間雨量</th><th>流域平均時間雨量</th><th>流域平均累計雨量</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10_val[i] + '</td><td>' + data.d20_val[i] + '</td><td>' + data.d40_val[i] + '</td><td>' + data.d50_val[i] + '</td><td>' + data.d70_val[i] + '</td><td>' + data.d10010_10m_val[i] + '</td><td>' + data.d10010_1h_val[i] + '</td><td>' + data.d10070_val[i] + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}
