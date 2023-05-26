﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Vts.MonteCarlo.IO;

namespace Vts.MonteCarlo.PostProcessor.Test
{
    [TestFixture]
    public class ProgramTests
    {
        /// <summary>
        /// clear all previously generated folders and files.
        /// </summary>
        
        // Note: needs to be kept current with PostProcessorInputProvider.  If an infile is added there, it should be added here.
        private readonly List<string> _listOfMcppInfiles = new List<string>()
        {
            "PostProcessor_ROfRhoTOfRho", 
            "PostProcessor_pMC_ROfRhoROfRhoAndTime",
            "PostProcessor_pMC_ROfRhoROfXAndYVariants",
            "PostProcessor_pMC_ROfFxROfFxAndTime",
        };

        private readonly List<string> _listOfMcclInfiles = new List<string>()
        {
            "ellip_FluenceOfRhoAndZ",
            "infinite_cylinder_AOfXAndYAndZ",
            "multi_infinite_cylinder_AOfXAndYAndZ",
            "fluorescence_emission_AOfXAndYAndZ_source_infinite_cylinder",
            "embedded_directional_circular_source_ellip_tissue",
            "Flat_2D_source_one_layer_ROfRho",
            "Flat_2D_source_two_layer_bounded_AOfRhoAndZ",
            "Gaussian_2D_source_one_layer_ROfRho",
            "Gaussian_line_source_one_layer_ROfRho",
            "one_layer_all_detectors",
            "one_layer_FluenceOfRhoAndZ_RadianceOfRhoAndZAndAngle",
            "one_layer_ROfRho_FluenceOfRhoAndZ",
            "pMC_one_layer_ROfRho_DAW",
            "three_layer_ReflectedTimeOfRhoAndSubregionHist",
            "two_layer_momentum_transfer_detectors",
            "two_layer_ROfRho",
            "two_layer_ROfRho_TOfRho_with_databases",
            "voxel_ROfXAndY_FluenceOfXAndYAndZ",
            "surface_fiber_detector"
        };

        /// <summary>
        /// clear all previously generated folders and files, then regenerate sample infiles using "geninfiles" option.
        /// </summary>
        [OneTimeSetUp]
        public async Task Setup()
        {
            Clear_folders_and_files();

            var arguments = new string[] { "geninfiles" };
            // generate sample MCPP infiles because unit tests below rely on infiles being generated
            Program.Main(arguments);

            // generate MCCL output so that MCPP can post-process
            // generate MCCL infiles - this is overkill but it verifies that we have an MCCL infile
            // that pairs with a MCPP infile
            CommandLineApplication.Program.Main(arguments);
            // run MCCL with generation of reflectance and transmittance databases
            arguments = new string[] { "infile=infile_two_layer_ROfRho_TOfRho_with_databases" };
            await Task.Run(() => CommandLineApplication.Program.Main(arguments));
            // run MCCL to post-process with pMC
            arguments = new string[] { "infile=infile_pMC_one_layer_ROfRho_DAW" };
            await Task.Run(() => CommandLineApplication.Program.Main(arguments));
        }

        [OneTimeTearDown]
        public void Clear_folders_and_files()
        {
            // delete any previously generated infiles to test that "geninfiles" option creates them
            foreach (var infile in _listOfMcppInfiles)
            {
                if (File.Exists("infile_" + infile + ".txt"))
                {
                    File.Delete("infile_" + infile + ".txt");
                }
                if (Directory.Exists(infile))
                {
                    Directory.Delete(infile, true); // delete recursively
                }
            }
            foreach (var infile in _listOfMcclInfiles)
            {
                if (File.Exists("infile_" + infile + ".txt"))
                {
                    File.Delete("infile_" + infile + ".txt");
                }
                if (Directory.Exists(infile))
                {
                    Directory.Delete(infile, true); // delete recursively
                }
            }
            // temporary folders generated by unit tests
            if (Directory.Exists("test"))
            {
                Directory.Delete("test", true); // delete recursively
            }
            if (Directory.Exists("test2"))
            {
                Directory.Delete("test2", true); // delete recursively
            }
        }

        /// <summary>
        /// Test to verify "geninfiles" option works successfully. 
        /// </summary>
        [Test]
        public void Validate_geninfiles_option_generates_all_infiles()
        {
            foreach (var infile in _listOfMcppInfiles)
            {
                Assert.IsTrue(File.Exists("infile_" + infile + ".txt"));
            }
        }

        /// <summary>
        /// Test to verify correct output files generated when post process MCCL reflectance
        /// and transmittance database
        /// </summary>
        [Test]
        public void Validate_output_folders_and_files_correct_when_using_reflectance_transmittance_database()
        {
            var arguments = new string[] { "infile=infile_PostProcessor_ROfRhoTOfRho.txt" };
            Program.Main(arguments);
            // verify results folder exists
            Assert.IsTrue(Directory.Exists("PostProcessor_ROfRhoTOfRho"));
            // verify infile gets written to output folder
            Assert.IsTrue(File.Exists("PostProcessor_ROfRhoTOfRho/PostProcessor_ROfRhoTOfRho.txt"));
            // verify detectors specified in MCPP infile get written
            Assert.IsTrue(File.Exists("PostProcessor_ROfRhoTOfRho/ROfRho.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_ROfRhoTOfRho/TOfRho.txt"));
        }

        /// <summary>
        /// Test to verify correct output files generated when post process MCCL pMC database
        /// </summary>
        [Test]
        public void Validate_output_folders_and_files_correct_when_using_pmc_geninfile_infile()
        {
            var arguments = new string[] { "infile=infile_PostProcessor_pMC_ROfRhoROfRhoAndTime.txt" };
            Program.Main(arguments);
            // verify results folder exists
            Assert.IsTrue(Directory.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime"));
            // verify infile gets written to output folder
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/PostProcessor_pMC_ROfRhoROfRhoAndTime.txt"));
            // verify detectors specified in MCPP infile get written
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoReference.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoReference"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoReference_2"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus1p5.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus1p5"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus1p5_2"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus0p5.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus0p5"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRho_mus0p5_2"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoAndTime_mus1p5.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoAndTime_mus1p5"));
            Assert.IsFalse(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/pMCROfRhoAndTime_mus1p5_2"));
            // added regular detectors into this infile in PPInputProvider -> verify they are there
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/ROfRho.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/ROfRho"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/ROfRhoAndTime.txt"));
            Assert.IsTrue(File.Exists("PostProcessor_pMC_ROfRhoROfRhoAndTime/ROfRhoAndTime"));
            // check that can read detectors in
            var readDetector = DetectorIO.ReadDetectorFromFile(
                "pMCROfRhoReference", "PostProcessor_pMC_ROfRhoROfRhoAndTime");
            Assert.IsInstanceOf<IDetector>(readDetector);
            readDetector = DetectorIO.ReadDetectorFromFile(
                "pMCROfRhoAndTime_mus1p5", "PostProcessor_pMC_ROfRhoROfRhoAndTime");
            Assert.IsInstanceOf<IDetector>(readDetector);
            readDetector = DetectorIO.ReadDetectorFromFile(
                "ROfRho", "PostProcessor_pMC_ROfRhoROfRhoAndTime");
            Assert.IsInstanceOf<IDetector>(readDetector);
            readDetector = DetectorIO.ReadDetectorFromFile(
                "ROfRhoAndTime", "PostProcessor_pMC_ROfRhoROfRhoAndTime");
            Assert.IsInstanceOf<IDetector>(readDetector);
        }

        /// <summary>
        /// Test to verify correct output folder generated when commandline "outpath" directive given
        /// </summary>
        [Test]
        public void validate_output_folders_correct_when_using_commandline_outpath_directive()
        {
            string[] arguments = new string[]
            {
                "infile=infile_PostProcessor_pMC_ROfRhoROfRhoAndTime.txt", "outpath=test"
            };
            Program.Main(arguments);
            Assert.IsTrue(Directory.Exists("test"));
        }

        /// <summary>
        /// Test to verify correct output folder generated when commandline "inpath" directive given
        /// </summary>
        [Test]
        public void validate_output_folders_correct_when_using_commandline_inpath_directive()
        {
            // run MCCL with outpath specified
            string[] arguments = new string[] { "infile=infile_pMC_one_layer_ROfRho_DAW", "outpath=test" };
            CommandLineApplication.Program.Main(arguments);
            // give inpath that matches one specified by outpath
            arguments = new string[]
            {
                "infile=infile_PostProcessor_pMC_ROfRhoROfRhoAndTime.txt", "inpath=test", "outpath=test2"
            };
            Program.Main(arguments);
            Assert.IsTrue(Directory.Exists("test2"));
        }
    }
}
