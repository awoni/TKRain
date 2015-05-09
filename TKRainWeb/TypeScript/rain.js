var xmlhttp = new XMLHttpRequest();
var url = "http://tk.ecitizen.jp/Data/Rain/RainData.json";
var url0 = "http://tk.ecitizen.jp/Data/Rain/";
xmlhttp.onreadystatechange = function () {
    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        var data = JSON.parse(xmlhttp.responseText);
        rainSummaryTable(data, url);
    }
};
xmlhttp.open("GET", url, true);
xmlhttp.send();
function rainSummaryTable(data, url) {
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>場所</th><th>時間雨量</th><th>累計雨量</th><th>観測時間<th>Jsonデータ</th></tr>";
    var i;
    for (i = 0; i < data.hr.length; i++) {
        out += '<tr><td>' + data.hr[i].obn + '</td><td>' + data.hr[i].d10_1h_val + '</td><td>' + data.hr[i].d70_10m_val + '</td><td>' + data.hr[i].dt + '</td><td>' + url0 + data.hr[i].ofc + '-' + data.hr[i].obc + '.json' + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id01").innerHTML = out;
    rainGetDetail(data.hr[0].ofc + '-' + data.hr[0].obc);
}
function rainGetDetail(place) {
    var xmlhttp1 = new XMLHttpRequest();
    var url1 = url0 + place + ".json";
    xmlhttp1.onreadystatechange = function () {
        if (xmlhttp1.readyState == 4 && xmlhttp1.status == 200) {
            var data = JSON.parse(xmlhttp1.responseText);
            rainDetailTable(data, url1);
        }
    };
    xmlhttp1.open("GET", url1, true);
    xmlhttp1.send();
}
function rainDetailTable(data, url) {
    document.getElementById("place0").innerHTML = data.obn;
    var out = "<table class='table table-bordered'>";
    out += "<tr><th>観測時間</th><th>10分雨量</th><th>時間雨量</th><th>累計雨量</th></tr>";
    var i;
    for (i = 0; i < data.ot.length; i++) {
        out += '<tr><td>' + data.ot[i] + '</td><td>' + data.d10_10m_val[i] + '</td><td>' + data.d10_1h_val[i] + '</td><td>' + data.d70_10m_val[i] + '</td></tr>';
    }
    out += "<table>";
    out += "<p>データ: " + url + "</p>";
    document.getElementById("id02").innerHTML = out;
}
