﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Herschel.Lib;
using Jhu.Spherical;

namespace Herschel.Lib.Test
{
    [TestClass]
    public class DetectorTest
    {
        [TestMethod]
        public void TestGetCorners()
        {
            var d = new DetectorPacsPhoto();
            var c = d.GetCorners(new Cartesian(0, 0), 0);
        }

        [TestMethod]
        public void TestGetFootprintPacsPhoto()
        {
            var d = new DetectorPacsPhoto();
            var r = d.GetFootprint(new Cartesian(0, 0), 0, 0);
            var a = r.Area;
        }

        [TestMethod]
        public void TestGetCornersPacsSpectro()
        {
            var d = new DetectorPacsSpectro();
            var c = d.GetCorners(new Cartesian(208.492853956836, -66.5113588517865), 297.655055918696);
        }

        [TestMethod]
        public void TestGetFootprintPacsSpectro()
        {
            var d = new DetectorPacsSpectro();
            var r = d.GetFootprint(new Cartesian(208.492853956836, -66.5113588517865), 297.655055918696, 0);
            var a = r.Area;
        }

        [TestMethod]
        public void TestGetFootprintHifiMap()
        {
            var d = new DetectorHifiMap()
            {
                Width = 0.032 * 60,
                Height = 0.0213333 * 60
            };

            var r = d.GetFootprint(new Cartesian(168.799587287364, -61.2738498090187), 145, 0.0);
            var a = r.Area;
        }

    }
}
