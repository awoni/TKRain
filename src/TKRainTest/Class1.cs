using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using TKRain;

namespace TKRainTest
{
    public class Class1
    {
        [Theory]
        [InlineData(0)]
        public void TestMethod1(int value)
        {
            double lat, lng;
            XyToBl.Calcurate(4, 10000, 10000, out lat, out lng);
            Assert.True(Math.Abs(lat - 33.09013) < 0.00001);
            Assert.True(Math.Abs(lng - 133.60713) < 0.00001);
            XyToBl.Calcurate(4, 98000, 123000, out lat, out lng);
            Assert.True(Math.Abs(lat - 33.87650) < 0.00001);
            Assert.True(Math.Abs(lng - 134.82955) < 0.00001);
            XyToBl.Calcurate(4, -123000, -111000, out lat, out lng);
            Assert.True(Math.Abs(lat - 31.88531) < 0.00001);
            Assert.True(Math.Abs(lng - 132.32669) < 0.00001);
        }
    }
}
