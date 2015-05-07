using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 平面直角座標から緯度、経度への変換については国土地理院のホームページに計算サイトと計算式があります。
 * 計算サイト http://surveycalc.gsi.go.jp/sokuchi/surveycalc/xy2blf.html
 * 計算式 http://surveycalc.gsi.go.jp/sokuchi/surveycalc/algorithm/xy2bl/xy2bl.htm
 * 以下の計算は上の計算式を使っています。
 * 
 * XyToBl.Calcurate(int kei, double x, double y, out double lat, out double lng)
 * kei: 平面直角座標系の系番号 1から19
 * x: 単位はm 座標系原点において子午線に一致する軸とし、真北に向う値を正
 * y: 単位はm 座標系原点において座標系のＸ軸に直交する軸とし、真東に向う値を正
 * lat: 緯度 度の十進数
 * lng: 経度 度の十進数
 * 
 * Ⅳ系以外はテストをしていません。
 * 利用するときは必ずテストをしてください。
 */

namespace TKRain
{
    public class XyToBl
    {
        //private const double a = 6378137.0; //地球の長半径、この数字は使わず計算式のAを使用
        private const double F = 298.257222101; //地球の逆扁平率
        private const double A = 6366.812400856471;

        private static readonly double[] Sφ0 =
        {
            3652.382768270787, 3652.382768270787,
            3985.144116029222, 3652.382768270787,
            3985.144116029222, 3985.144116029222,
            3985.144116029222, 3985.144116029222,
            3985.144116029222, 4429.086077333565,
            4873.334987359201, 4873.334987359201,
            4873.334987359201, 2876.546889061122,
            2876.546889061122, 2876.546889061122,
            2876.546889061122, 2212.142017477571,
            2876.546889061122
        };

        private static readonly double[] λ0 =
        {
            129.5, 131.0,
            132.166666666667, 133.5,
            134.333333333333, 136.0,
            137.166666666667, 138.5,
            139.833333333333, 140.833333333333,
            140.25, 142.25,
            144.25, 142.0,
            127.5, 124.0,
            131.0, 136.0,
            154.0
        };


        public static void Calcurate(int kei, double x, double y, out double lat, out double lng)
        {
            double n = 1.0 / (2 * F - 1);
            double ξ = (x / 1000 + Sφ0[kei - 1]) / A;
            double η = y / 1000 / A;

            double β1 = n/2.0 - 2.0*n*n/3.0 + 37.0*n*n*n/96.0 - n*n*n*n/360.0 - 81.0*n*n*n*n*n/512.0;
            double β2 = n*n/48.0 + n*n*n/15.0 - 437.0*n*n*n*n/1440.0 - 46.0*n*n*n*n*n/105.0;
            double β3 = 17.0*n*n*n/480.0 - 37.0*n*n*n*n/840.0 - 209.0*n*n*n*n*n/4480.0;
            double β4 = 4397.0*n*n*n*n/161280.0 - 11.0*n*n*n*n*n/504.0;
            double β5 = 4583.0*n*n*n*n*n/161280.0;

            double ξξ = ξ - β1 * Math.Sin(2 * ξ) * Math.Cosh(2 * η) - β2 * Math.Sin(4 * ξ) * Math.Cosh(4 * η) -
                        β3 * Math.Sin(6 * ξ) * Math.Cosh(6 * η) - β4 * Math.Sin(8 * ξ) * Math.Cosh(8 * η) -
                        β5 * Math.Sin(10 * ξ) * Math.Cosh(10 * η);

            double ηη = η - β1 * Math.Cos(2 * ξ) * Math.Sinh(2 * η) - β2 * Math.Cos(4 * ξ) * Math.Sinh(4 * η) -
                        β3 * Math.Cos(6 * ξ) * Math.Sinh(6 * η) - β4 * Math.Cos(8 * ξ) * Math.Sinh(8 * η) -
                        β5 * Math.Cos(10 * ξ) * Math.Sinh(10 * η);

            /*
            double σσ = 1 - 2 * β1 * Math.Cos(2 * ξ) * Math.Cosh(2 * η) - 4 * β2 * Math.Cos(4 * ξ) * Math.Cosh(4 * η) -
                        6 * β3 * Math.Cos(6 * ξ) * Math.Cosh(6 * η) - 8 * β4 * Math.Cos(8 * ξ) * Math.Cosh(8 * η) -
                        10 * β5 * Math.Cos(10 * ξ) * Math.Cosh(10 * η);

            double ττ = 2 * β1 * Math.Sin(2 * ξ) * Math.Sinh(2 * η) + 4 * β2 * Math.Sin(4 * ξ) * Math.Sinh(4 * η) +
                        6 * β3 * Math.Sin(6 * ξ) * Math.Sinh(6 * η) + 8 * β4 * Math.Sin(8 * ξ) * Math.Sinh(8 * η) +
                        10 * β5 * Math.Sin(10 * ξ) * Math.Sinh(10 * η);
            */

            double χ = Math.Asin(Math.Sin(ξξ) / Math.Cosh(ηη));

            double δ1 = 2*n - 2*n*n/3.0 - 2*n*n*n - 116*n*n*n*n/45 + 26*n*n*n*n*n/45 - 2854*n*n*n*n*n*n/675;
            double δ2 = 7*n*n/3.0 - 8*n*n*n/5 - 227*n*n*n*n/45 + 2704*n*n*n*n*n/315 - 2323*n*n*n*n*n*n/945;
            double δ3 = 56*n*n*n/15 - 136*n*n*n*n/35 - 1262*n*n*n*n*n/105 + 73814*n*n*n*n*n*n/2835;
            double δ4 = 4279*n*n*n*n/630 + 332*n*n*n*n*n/35 - 399572*n*n*n*n*n*n/14175;
            double δ5 = 4174*n*n*n*n*n/315 - 144838*n*n*n*n*n*n/6237;
            double δ6 = 601676*n*n*n*n*n*n/22275;

            lat = 180 / Math.PI * (χ + δ1 * Math.Sin(2 * χ) + δ2 * Math.Sin(4 * χ) + δ3 * Math.Sin(6 * χ)
                       + δ4 * Math.Sin(8 * χ) + δ5 * Math.Sin(10 * χ) + δ6 * Math.Sin(12 * χ));

            lng = λ0[kei - 1] + 180 / Math.PI * Math.Atan(Math.Sinh(ηη) / Math.Cos(ξξ));
        }
    }
}
