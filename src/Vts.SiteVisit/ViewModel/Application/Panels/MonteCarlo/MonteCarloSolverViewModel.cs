﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using SLExtensions.Input;
using Vts.Common;
using Vts.Common.Logging;
using Vts.Extensions;
using Vts.IO;
using Vts.MonteCarlo;
using Vts.MonteCarlo.IO;
using Vts.MonteCarlo.Tissues;
using Vts.SiteVisit.Input;
using Vts.SiteVisit.Model;
using Vts.SiteVisit.View;

namespace Vts.SiteVisit.ViewModel
{
    /// <summary> 
    /// View model implementing the Monte Carlo panel functionality (experimental)
    /// </summary>
    public class MonteCarloSolverViewModel : BindableObject
    {
        private static ILogger logger = LoggerFactoryLocator.GetDefaultNLogFactory().Create(typeof(MonteCarloSolverViewModel));

        //private SimulationInput _simulationInput;
        private MonteCarloSimulation _simulation;

        private Output _output;

        private SimulationInputViewModel _simulationInputVM;

        private RangeViewModel _independentVariableRangeVM;

        private SolutionDomainOptionViewModel _solutionDomainTypeOptionVM;

        private CancellationTokenSource _currentCancellationTokenSource;

        private bool _firstTimeSaving;

        private double[] _mapArrayBuffer;

        public MonteCarloSolverViewModel()
        {
            var simulationInput = GetDefaultSimulationInput();

            _simulationInputVM = new SimulationInputViewModel(simulationInput);

            var rho = ((ROfRhoDetectorInput)simulationInput.VirtualBoundaryInputs.
                Where(g => g.VirtualBoundaryType == VirtualBoundaryType.DiffuseReflectance).First().
                DetectorInputs.Where(d => d.TallyType == TallyType.ROfRho).First()).Rho;
            //Where(d => d.TallyType == TallyType.ROfRho).First()).Rho;

            _independentVariableRangeVM = new RangeViewModel(rho, "mm", "Independent Variable Range");

            SolutionDomainTypeOptionVM = new SolutionDomainOptionViewModel("Solution Domain", SolutionDomainType.RofRho);

            //Commands.MC_ExecuteMonteCarloSolver.Executed += MC_ExecuteMonteCarloSolver_Executed;
            Commands.FS_SetIndependentVariableRange.Executed += SetIndependentVariableRange_Executed;

            ExecuteMonteCarloSolverCommand = new RelayCommand(() => MC_ExecuteMonteCarloSolver_Executed(null, null));
            CancelMonteCarloSolverCommand = new RelayCommand(() => MC_CancelMonteCarloSolver_Executed(null, null));
            LoadSimulationInputCommand = new RelayCommand(() => MC_LoadSimulationInput_Executed(null, null));
            DownloadDefaultSimulationInputCommand = new RelayCommand(() => MC_DownloadDefaultSimulationInput_Executed(null, null));
            SaveSimulationResultsCommand = new RelayCommand(() => MC_SaveSimulationResults_Executed(null, null));

            _firstTimeSaving = true;
        }

        public RelayCommand ExecuteMonteCarloSolverCommand { get; private set; }
        public RelayCommand CancelMonteCarloSolverCommand { get; private set; }
        public RelayCommand LoadSimulationInputCommand { get; private set; }
        public RelayCommand DownloadDefaultSimulationInputCommand { get; private set; }
        public RelayCommand SaveSimulationResultsCommand { get; private set; }

        public SolutionDomainOptionViewModel SolutionDomainTypeOptionVM
        {
            get { return _solutionDomainTypeOptionVM; }
            set
            {
                _solutionDomainTypeOptionVM = value;
                OnPropertyChanged("SolutionDomainTypeOptionVM");
            }
        }

        public SimulationInputViewModel SimulationInputVM
        {
            get { return _simulationInputVM; }
            set
            {
                _simulationInputVM = value;
                OnPropertyChanged("SimulationInputVM");
            }
        }

        public RangeViewModel IndependentVariableRangeVM
        {
            get { return _independentVariableRangeVM; }
            set
            {
                _independentVariableRangeVM = value;
                OnPropertyChanged("IndependentVariableRangeVM");
            }
        }

        private SimulationInput GetDefaultSimulationInput()
        {
            var simulationInput = new SimulationInput
            {
                TissueInput = new MultiLayerTissueInput(
                    new List<ITissueRegion> 
                    { 
                        new LayerRegion(
                            new DoubleRange(double.NegativeInfinity, 0.0),
                            new OpticalProperties( 0.0, 1e-10, 0.0, 1.0)),
                        new LayerRegion(
                            new DoubleRange(0.0, 1.0),
                            new OpticalProperties(0.1, 1.0, 0.8, 1.4)),
                        new LayerRegion(
                            new DoubleRange(1.0, 100.0),
                            new OpticalProperties(0.01, 2.0, 0.8, 1.4)),
                        new LayerRegion(
                            new DoubleRange(100.0, double.PositiveInfinity),
                            new OpticalProperties(0.0, 1e-10, 0.0, 1.0))
                    }
                ),
                OutputName = "MonteCarloOutput",
                N = 100,
                Options = new SimulationOptions(
                    0, // Note seed = 0 is -1 in linux
                    RandomNumberGeneratorType.MersenneTwister,
                    AbsorptionWeightingType.Discrete),
                SourceInput = new DirectionalPointSourceInput(),
                VirtualBoundaryInputs = new List<IVirtualBoundaryInput>
                {
                    new SurfaceVirtualBoundaryInput(
                        VirtualBoundaryType.DiffuseReflectance,
                        new List<IDetectorInput>
                        {
                            new ROfRhoDetectorInput(new DoubleRange(0.0, 20.0, 201)), // rho: nr=200 dr=0.2mm used for workshop)
                        },
                        false, // write database
                        VirtualBoundaryType.DiffuseReflectance.ToString()
                    ),
                    new SurfaceVirtualBoundaryInput(
                        VirtualBoundaryType.DiffuseTransmittance,
                        new List<IDetectorInput>
                        {
                            //new TOfRhoDetectorInput(new DoubleRange(0.0, 40.0, 201)), // rho: nr=200 dr=0.2mm used for workshop)
                        },
                        false, // write database
                        VirtualBoundaryType.DiffuseTransmittance.ToString()
                    )
                }
            };

            return simulationInput;
        }

        //private IEnumerable<Point> ExecuteMonteCarloSolver()
        //{
        //    IEnumerable<double> independentValues = IndependentVariableRangeVM.Values.ToList();

        //    var input = _simulationInputVM.SimulationInput;

        //    _simulation = new MonteCarloSimulation(input);

        //    _output = simulation.Run();

        //    return EnumerableEx.Zip(independentValues, output.R_r, (x, y) => new Point(x, y));
        //}

        private void SetIndependentVariableRange_Executed(object sender, ExecutedEventArgs e)
        {
            if (e.Parameter is RangeViewModel)
            {
                IndependentVariableRangeVM = (RangeViewModel)e.Parameter;
            }
        }

        //private Thread _currentBackgroundThread;
        private void MC_ExecuteMonteCarloSolver_Executed(object sender, ExecutedEventArgs e)
        {
            var input = _simulationInputVM.SimulationInput;
            var nPhotons = _simulationInputVM.SimulationInput.N;

            ROfRhoDetectorInput rOfRho = (ROfRhoDetectorInput)(input.VirtualBoundaryInputs.Where(
                v => v.VirtualBoundaryType == VirtualBoundaryType.DiffuseReflectance).Select(
                d => d.DetectorInputs.Where(i => i.Name == TallyType.ROfRho.ToString()).First()).First());

            IEnumerable<double> independentValues = rOfRho.Rho.AsEnumerable();

            _simulation = new MonteCarloSimulation(input);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var t = Task.Factory.StartNew(() => _simulation.Run());
            //{
            //    _currentBackgroundThread = Thread.CurrentThread; // crappy work-around todo: fix (see here: http://stackoverflow.com/questions/4783865/how-do-i-abort-cancel-tpl-tasks)

            //    return _simulation.Run();
            //});

            _currentCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancelToken = _currentCancellationTokenSource.Token;
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            var c = t.ContinueWith((antecedent) =>
                {
                    SolverDemoView.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        stopwatch.Stop();
                        _output = antecedent.Result;
                        if (_output.R_r != null)
                        {
                            //var showPlusMinusStdev = true;
                            IEnumerable<Point> points = null;
                            //if(showPlusMinusStdev && _output.R_r2 != null)
                            //{
                            //    var stdev = Enumerable.Zip(_output.R_r, _output.R_r2, (r, r2) => Math.Sqrt((r2 - r * r) / nPhotons)).ToArray();
                            //    var rMinusStdev = Enumerable.Zip(_output.R_r, stdev, (r,std) => r-std).ToArray();
                            //    var rPlusStdev = Enumerable.Zip(_output.R_r, stdev, (r,std) => r+std).ToArray();
                            //    points = Enumerable.Zip(
                            //        independentValues.Concat(independentValues).Concat(independentValues),
                            //        rMinusStdev.Concat(_output.R_r).Concat(rPlusStdev),
                            //        (x, y) => new Point(x, y));
                            //}
                            //else
                            //{
                            points = Enumerable.Zip(
                                independentValues,
                                _output.R_r,
                                (x, y) => new Point(x, y));
                            //}

                            //IEnumerable<Point> points = ExecuteMonteCarloSolver();

                            PlotAxesLabels axesLabels = GetPlotLabels();
                            Commands.Plot_SetAxesLabels.Execute(axesLabels);

                            string plotLabel = GetPlotLabel();
                            Commands.Plot_PlotValues.Execute(new PlotData(points, plotLabel));
                        }

                        var detectorInputs = _simulationInputVM.SimulationInput.VirtualBoundaryInputs
                            .Where(vb => vb.VirtualBoundaryType == VirtualBoundaryType.GenericVolumeBoundary)
                            .SelectMany(vb => vb.DetectorInputs)
                            .Where(di => di.Name == "FluenceOfRhoAndZ");

                        if (detectorInputs.Any())
                        {
                            var detectorInput = (FluenceOfRhoAndZDetectorInput)detectorInputs.First();
                            var rhosMC = detectorInput.Rho.AsEnumerable().ToArray();
                            var zsMC = detectorInput.Z.AsEnumerable().ToArray();

                            var rhos = Enumerable.Zip(rhosMC.Skip(1), rhosMC.Take(rhosMC.Length - 1), (first, second) => (first + second) / 2).ToArray();
                            var zs = Enumerable.Zip(zsMC.Skip(1), rhosMC.Take(zsMC.Length - 1), (first, second) => (first + second) / 2).ToArray();
                            
                            //var dRhos = Enumerable.Zip(rhosMC.Skip(1), rhosMC.Take(rhosMC.Length - 1), (first, second) => (first - second)).ToArray();
                            //var dZs = Enumerable.Zip(zsMC.Skip(1), rhosMC.Take(zsMC.Length - 1), (first, second) => (first - second)).ToArray();
                            var dRhos = Enumerable.Select(rhos, rho => 2 * Math.PI *Math.Abs(rho) * detectorInput.Rho.Delta).ToArray();
                            var dZs = Enumerable.Select(zs, z => detectorInput.Rho.Delta).ToArray();

                            if (_mapArrayBuffer == null || _mapArrayBuffer.Length != _output.Flu_rz.Length*2)
                            {
                                _mapArrayBuffer = new double[_output.Flu_rz.Length*2];
                            }
                            // flip the array (since it goes over zs and then rhos, while map wants rhos and then zs
                            //long index = 0;
                            for (int zi = 0; zi < zs.Length; zi++)
                            {
                                for (int rhoi = 0; rhoi < rhos.Length; rhoi++)
                                {
                                    //destinationArray[rhoi + rhos.Length * zi] = _output.Flu_rz[rhoi, zi];
                                    _mapArrayBuffer[rhoi + rhos.Length + rhos.Length * 2 * zi] = _output.Flu_rz[rhoi, zi];
                                }
                                var localRhoiForReverse = 0;
                                for (int rhoi = rhos.Length - 1; rhoi >= 0; rhoi--, localRhoiForReverse++)
                                {
                                    _mapArrayBuffer[localRhoiForReverse + rhos.Length * 2 * zi] = _output.Flu_rz[rhoi, zi];
                                }
                            }

                            var twoRhos = Enumerable.Concat(rhos.Reverse().Select(rho => -rho), rhos).ToArray();
                            var twoDRhos = Enumerable.Concat(dRhos.Reverse(), dRhos).ToArray();

                            var mapData = new MapData(_mapArrayBuffer, twoRhos, zs, twoDRhos, dZs);

                            Commands.Maps_PlotMap.Execute(mapData);
                        }

                        logger.Info(() => "Monte Carlo simulation complete (N = " + nPhotons + " photons; simulation time = "
                            + stopwatch.ElapsedMilliseconds / 1000f + " seconds).\r");
                    });
                },
                cancelToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                scheduler);
        }

        // mre is used to block and release threads manually.
        // private static ManualResetEvent mre = new ManualResetEvent(false);
        private void MC_CancelMonteCarloSolver_Executed(object sender, ExecutedEventArgs e)
        { // crappy work-around todo: fix (see here: http://stackoverflow.com/questions/4783865/how-do-i-abort-cancel-tpl-tasks)
            //if (_currentBackgroundThread != null)
            //{
            //    _currentBackgroundThread.
            //    logger.Info(() => "Simulation cancelled.\n");
            //}
            Task.Factory.StartNew(() => _simulation.Cancel());
            if (_currentCancellationTokenSource != null)
            {
                _currentCancellationTokenSource.Cancel(true);
                logger.Info(() => "Simulation cancelled.\n");
                _currentCancellationTokenSource = null;
            }
        }

        private void MC_LoadSimulationInput_Executed(object sender, ExecutedEventArgs e)
        {
            using (var stream = StreamFinder.GetLocalFilestreamFromOpenFileDialog("xml"))
            {
                if (stream != null)
                {
                    var simulationInput = FileIO.ReadFromStream<SimulationInput>(stream);

                    var validationResult = SimulationInputValidation.ValidateInput(simulationInput);
                    if (validationResult.IsValid)
                    {
                        _simulationInputVM.SimulationInput = simulationInput;
                        logger.Info(() => "Simulation input loaded.\r");
                    }
                    else
                    {
                        logger.Info(() => "Simulation input not loaded - XML format not valid.\r");
                    }
                }
            }
        }

        private void MC_DownloadDefaultSimulationInput_Executed(object sender, ExecutedEventArgs e)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();

            if (_firstTimeSaving)
            {
                Commands.IsoStorage_IncreaseSpaceQuery.Execute();
                _firstTimeSaving = false;
                return;
            }

            using (var stream = StreamFinder.GetLocalFilestreamFromSaveFileDialog("zip"))
            {
                if (stream != null)
                {
                    var resourcesPath = "MonteCarlo/Resources/DataStructures/SimulationInput/";
                    var xmlFilenames = new[]
                        {
                            "infile_ROfRho.xml",
                            "infile_database.xml",
                            "infile_pMC_database.xml",
                            "infile.xml"
                        };

                    var files = xmlFilenames.Select(fileName =>
                        new
                        {
                            Name = fileName,
                            Input = FileIO.ReadFromXMLInResources<SimulationInput>(resourcesPath + fileName, "Vts")
                        });

                    foreach (var file in files)
                    {
                        file.Input.ToFile(file.Name);
                    }

                    //var infile_ROfRho = FileIO.ReadFromXMLInResources<SimulationInput>(resourcesPath + "infile_ROfRho.xml", "Vts");
                    //var infile_database = FileIO.ReadFromXMLInResources<SimulationInput>(resourcesPath + "infile_database.xml", "Vts");
                    //var infile_pMC_database = FileIO.ReadFromXMLInResources<SimulationInput>(resourcesPath + "infile_pMC_database.xml", "Vts");
                    //var infile = FileIO.ReadFromXMLInResources<SimulationInput>(resourcesPath + "infile.xml", "Vts");
                    //infile_ROfRho.ToFile("infile_ROfRho.xml");
                    //infile_database.ToFile("infile_database.xml");
                    //infile_pMC_database.ToFile("infile_pMC_database.xml");
                    //infile.ToFile("infile.xml");

                    //// then, zip all these together and SaveFileDialog to .zip...
                    //using (var zipStream = StreamFinder.GetLocalFilestreamFromSaveFileDialog("zip"))
                    //{
                    //if (zipStream != null)
                    //{
                    FileIO.ZipFiles(xmlFilenames, "", stream);
                    logger.Info(() => "Template simulation input files exported to a zip file.\r");
                    //}
                    //}

                    // FileIO.WriteToStream(simulationInput, stream);

                    //logger.Info(() => "Simulation input exported to file.\r");
                }
            }
        }

        private void MC_SaveSimulationResults_Executed(object sender, ExecutedEventArgs e)
        {
            if (_output != null)
            {
                var input = _simulationInputVM.SimulationInput;
                string resultsFolder = input.OutputName;
                FileIO.CreateDirectory(resultsFolder);
                input.ToFile(resultsFolder + "\\" + input.OutputName + ".xml");

                var store = IsolatedStorageFile.GetUserStoreForApplication();

                if (_firstTimeSaving)
                {
                    Commands.IsoStorage_IncreaseSpaceQuery.Execute();
                    _firstTimeSaving = false;
                    return;
                }

                foreach (var result in _output.ResultsDictionary.Values)
                {
                    // save all detector data to the specified folder
                    DetectorIO.WriteDetectorToFile(result, resultsFolder);
                }

                if (store.DirectoryExists(resultsFolder))
                {
                    var fileNames = store.GetFileNames(resultsFolder + @"\*");

                    // then, zip all these together and SaveFileDialog to .zip...
                    using (var stream = StreamFinder.GetLocalFilestreamFromSaveFileDialog("zip"))
                    {
                        if (stream != null)
                        {
                            FileIO.ZipFiles(fileNames, resultsFolder, stream);
                            logger.Info(() => "Simulation results exported to file.\r");
                        }
                    }
                }
            }
        }

        private PlotAxesLabels GetPlotLabels()
        {
            var sd = this.SolutionDomainTypeOptionVM;
            PlotAxesLabels axesLabels = null;
            if (sd.IndependentVariableAxisOptionVM.Options.Count > 1)
            {
                axesLabels = new PlotAxesLabels(
                    sd.IndependentAxisLabel, sd.IndependentAxisUnits,
                    sd.SelectedDisplayName, "", sd.ConstantAxisLabel,
                    sd.ConstantAxisUnits, sd.ConstantAxisValue);
            }
            else
            {
                axesLabels = new PlotAxesLabels(sd.IndependentAxisLabel,
                    sd.IndependentAxisUnits, sd.SelectedDisplayName, "");
            }
            return axesLabels;
        }

        private string GetPlotLabel()
        {
            string nString = "N: " + _simulationInputVM.SimulationInput.N;
            string awtString = "AWT: ";
            switch (_simulationInputVM.SimulationInput.Options.AbsorptionWeightingType)
            {
                case AbsorptionWeightingType.Analog:
                    awtString += "analog";
                    break;
                case AbsorptionWeightingType.Discrete:
                    awtString += "discrete";
                    break;
                case AbsorptionWeightingType.Continuous:
                    awtString += "continuous";
                    break;
            }

            return "Model - MC\r" + nString + "\r" + awtString + "";
        }
    }
}