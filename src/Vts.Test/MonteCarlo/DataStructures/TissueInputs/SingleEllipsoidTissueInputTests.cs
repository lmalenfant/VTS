﻿using System.Collections.Generic;
using NUnit.Framework;
using Vts.Common;
using Vts.IO;
using Vts.MonteCarlo;
using Vts.MonteCarlo.Tissues;

namespace Vts.Test.MonteCarlo
{
    [TestFixture]
    public class SingleEllipsoidTissueInputTests
    {
        /// <summary>
        /// list of temporary files created by these unit tests
        /// </summary>
        List<string> listOfFiles = new List<string>()
        {
            "SingleEllipsoidTissue.txt"
        };

        /// <summary>
        /// clear previously generated folders and files
        /// </summary>
        [TestFixtureSetUp]
        public void clear_previously_generated_folders_and_files()
        {
            foreach (var file in listOfFiles)
            {
                // ckh: should there be a check prior to delete that checks for file existence?
                FileIO.FileDelete(file);
            }
        }
        /// <summary>
        /// clear all newly generated folders and files
        /// </summary>
        [TestFixtureTearDown]
        public void clear_newly_generated_folders_and_files()
        {
            foreach (var file in listOfFiles)
            {
                // ckh: should there be a check prior to delete that checks for file existence?
                FileIO.FileDelete(file);
            }
        }
        [Test]
        public void validate_deserialized_class_is_correct()
        {
            var i = new SingleEllipsoidTissueInput(new EllipsoidTissueRegion(new Position(0, 0, 1), 0.5, 0.5, 0.5,
            new OpticalProperties(0.05, 1.0, 0.8, 1.4)), new ITissueRegion[]
                    { 
                        new LayerTissueRegion(
                            new DoubleRange(double.NegativeInfinity, 0.0),
                            new OpticalProperties(0.0, 1e-10, 1.0, 1.0)),
                        new LayerTissueRegion(
                            new DoubleRange(0.0, 100.0),
                            new OpticalProperties(0.01, 1.0, 0.8, 1.4)),
                        new LayerTissueRegion(
                            new DoubleRange(100.0, double.PositiveInfinity),
                            new OpticalProperties(0.0, 1e-10, 1.0, 1.0))
                    }
                );

            var iCloned = i.Clone();

            Assert.AreEqual(iCloned.Regions[1].RegionOP.Mua, i.Regions[1].RegionOP.Mua);
        }

        [Test]
        public void validate_deserialized_class_is_correct_when_using_FileIO()
        {
            var i = new SingleEllipsoidTissueInput(new EllipsoidTissueRegion(new Position(0, 0, 1), 0.5, 0.5, 0.5,
            new OpticalProperties(0.05, 1.0, 0.8, 1.4)), new ITissueRegion[]
                    { 
                        new LayerTissueRegion(
                            new DoubleRange(double.NegativeInfinity, 0.0),
                            new OpticalProperties(0.0, 1e-10, 1.0, 1.0)),
                        new LayerTissueRegion(
                            new DoubleRange(0.0, 100.0),
                            new OpticalProperties(0.01, 1.0, 0.8, 1.4)),
                        new LayerTissueRegion(
                            new DoubleRange(100.0, double.PositiveInfinity),
                            new OpticalProperties(0.0, 1e-10, 1.0, 1.0))
                    }
                );
            i.WriteToJson("SingleEllipsoidTissue.txt");
            var iCloned = FileIO.ReadFromJson<SingleEllipsoidTissueInput>("SingleEllipsoidTissue.txt");

            Assert.AreEqual(iCloned.Regions[1].RegionOP.Mua, i.Regions[1].RegionOP.Mua);
        }
    }
}
