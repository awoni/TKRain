// Copyright 2015 (c) Yasuhiro Niji
// Use of this source code is governed by the MIT License,
// as found in the LICENSE.txt file.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TKRain;

namespace TKRainTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            double lat, lng;
            XyToBl.Calcurate(4, 10000, 10000, out lat, out lng);
            Assert.IsTrue(Math.Abs(lat - 33.09013) < 0.00001);
            Assert.IsTrue(Math.Abs(lng - 133.60713) < 0.00001);
            XyToBl.Calcurate(4, 98000, 123000, out lat, out lng);
            Assert.IsTrue(Math.Abs(lat - 33.87650) < 0.00001);
            Assert.IsTrue(Math.Abs(lng - 134.82955) < 0.00001);
            XyToBl.Calcurate(4, -123000, -111000, out lat, out lng);
            Assert.IsTrue(Math.Abs(lat - 31.88531) < 0.00001);
            Assert.IsTrue(Math.Abs(lng - 132.32669) < 0.00001);
        }
    }
}
