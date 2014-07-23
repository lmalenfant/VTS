﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using GalaSoft.MvvmLight.Command;
using SLExtensions.Input;
using Vts.Common;
using Vts.IO;
using Vts.Modeling.ForwardSolvers;
using Vts.MonteCarlo;
using Vts.MonteCarlo.Tissues;
using Vts.Factories;
using Vts.Gui.Silverlight.Extensions;
using Vts.Gui.Silverlight.Input;
using Vts.Gui.Silverlight.Model;

namespace Vts.Gui.Silverlight.ViewModel
{
    /// <summary>
    /// View model implementing Fluence Solver panel functionality
    /// </summary>
    public class FluenceSolverViewModel : BindableObject
    {
        private OptionViewModel<MapType> _MapTypeOptionVM;
        private FluenceSolutionDomainOptionViewModel _FluenceSolutionDomainTypeOptionVM;
        private FluenceSolutionDomainOptionViewModel _AbsorbedEnergySolutionDomainTypeOptionVM;
        private FluenceSolutionDomainOptionViewModel _PhotonHittingDensitySolutionDomainTypeOptionVM;
        private OptionViewModel<ForwardSolverType> _ForwardSolverTypeOptionVM;
        //private OptionViewModel<ForwardAnalysisType> _ForwardAnalysisTypeOptionVM;

        private RangeViewModel _RhoRangeVM;
        private RangeViewModel _ZRangeVM;
        private double _SourceDetectorSeparation;
        private double _TimeModulationFrequency;

        private OpticalPropertyViewModel _OpticalPropertyVM;

        private object _tissueInputVM; // either an OpticalPropertyViewModel or a MultiRegionTissueViewModel is stored here, and dynamically displayed

        // private fields to cache created instances of tissue inputs, created on-demand in GetTissueInputVM (vs up-front in constructor)
        private OpticalProperties _currentHomogeneousOpticalProperties;
        private SemiInfiniteTissueInput _currentSemiInfiniteTissueInput;
        private MultiLayerTissueInput _currentMultiLayerTissueInput;
        private SingleEllipsoidTissueInput _currentSingleEllipsoidTissueInput;

        public FluenceSolverViewModel()
        {
            RhoRangeVM = new RangeViewModel(new DoubleRange(0.1, 19.9, 100), "mm", IndependentVariableAxis.Rho, "");
            ZRangeVM = new RangeViewModel(new DoubleRange(0.1, 19.9, 100), "mm", IndependentVariableAxis.Z, "");
            SourceDetectorSeparation = 10.0;
            TimeModulationFrequency = 0.1;
            _tissueInputVM = new OpticalPropertyViewModel();

            OpticalPropertyVM = new OpticalPropertyViewModel() { Title = "Optical Properties:" };

            // right now, we're doing manual databinding to the selected item. need to enable databinding 
            // confused, though - do we need to use strings? or, how to make generics work with dependency properties?
            ForwardSolverTypeOptionVM = new OptionViewModel<ForwardSolverType>(
                "Forward Model:",
                false,
                new[]
                {
                    ForwardSolverType.DistributedPointSourceSDA,
                    ForwardSolverType.PointSourceSDA,
                    ForwardSolverType.DistributedGaussianSourceSDA,
                    ForwardSolverType.TwoLayerSDA
                }); // explicitly enabling these for the workshop;

            FluenceSolutionDomainTypeOptionVM = new FluenceSolutionDomainOptionViewModel("Fluence Solution Domain", FluenceSolutionDomainType.FluenceOfRhoAndZ);
            AbsorbedEnergySolutionDomainTypeOptionVM = new FluenceSolutionDomainOptionViewModel("Absorbed Energy Solution Domain", FluenceSolutionDomainType.FluenceOfRhoAndZ);
            PhotonHittingDensitySolutionDomainTypeOptionVM = new FluenceSolutionDomainOptionViewModel("PHD Solution Domain", FluenceSolutionDomainType.FluenceOfRhoAndZ);
            PropertyChangedEventHandler updateSolutionDomain = (sender, args) => 
            {
                if (args.PropertyName == "IndependentAxisType")
                {
                    RhoRangeVM = ((FluenceSolutionDomainOptionViewModel)sender).IndependentAxisType.GetDefaultIndependentAxisRange();
                }
                // todo: must this fire on ANY property, or is there a specific one we can listen to, as above?
                this.OnPropertyChanged("IsTimeFrequencyDomain");
            };
            FluenceSolutionDomainTypeOptionVM.PropertyChanged += updateSolutionDomain;
            AbsorbedEnergySolutionDomainTypeOptionVM.PropertyChanged += updateSolutionDomain;
            PhotonHittingDensitySolutionDomainTypeOptionVM.PropertyChanged += updateSolutionDomain;

            MapTypeOptionVM = new OptionViewModel<MapType>(
                "Map Type", 
                new[]
                {
                    MapType.Fluence, 
                    MapType.AbsorbedEnergy, 
                    MapType.PhotonHittingDensity
                });

            MapTypeOptionVM.PropertyChanged += (sender, args) =>
            {
                this.OnPropertyChanged("IsFluence");
                this.OnPropertyChanged("IsAbsorbedEnergy");
                this.OnPropertyChanged("IsPhotonHittingDensity");
                this.OnPropertyChanged("IsTimeFrequencyDomain");
            };

            ForwardSolverTypeOptionVM.PropertyChanged += (sender, args) =>
                    {
                        OnPropertyChanged("ForwardSolver");
                        OnPropertyChanged("IsGaussianForwardModel");

                        OnPropertyChanged("IsMultiRegion");
                        OnPropertyChanged("IsSemiInfinite");
                        TissueInputVM = GetTissueInputVM(IsMultiRegion ? MonteCarlo.TissueType.MultiLayer : MonteCarlo.TissueType.SemiInfinite);
                    };

            ExecuteFluenceSolverCommand = new RelayCommand(() => ExecuteFluenceSolver_Executed(null, null));
        }

        public RelayCommand ExecuteFluenceSolverCommand { get; set; }

        public IForwardSolver ForwardSolver
        {
            get
            {
                return SolverFactory.GetForwardSolver(
                    ForwardSolverTypeOptionVM.SelectedValue);
            }
        }

        public bool IsGaussianForwardModel
        {
            get { return ForwardSolverTypeOptionVM.SelectedValue.IsGaussianForwardModel(); }
        }
        public bool IsMultiRegion
        {
            get { return ForwardSolverTypeOptionVM.SelectedValue.IsMultiRegionForwardModel(); }
        }

        public bool IsSemiInfinite
        {
            get { return !ForwardSolverTypeOptionVM.SelectedValue.IsMultiRegionForwardModel(); }
        }

        public bool IsFluence { get { return MapTypeOptionVM.SelectedValue == MapType.Fluence; } }
        public bool IsAbsorbedEnergy { get { return MapTypeOptionVM.SelectedValue == MapType.AbsorbedEnergy; } }
        public bool IsPhotonHittingDensity { get { return MapTypeOptionVM.SelectedValue == MapType.PhotonHittingDensity; } }
        public bool IsTimeFrequencyDomain 
        { 
            get 
            { 
                return 
                    (MapTypeOptionVM.SelectedValue == MapType.Fluence && 
                     FluenceSolutionDomainTypeOptionVM.SelectedValue == FluenceSolutionDomainType.FluenceOfRhoAndZAndFt) || 
                    (MapTypeOptionVM.SelectedValue == MapType.AbsorbedEnergy && 
                     AbsorbedEnergySolutionDomainTypeOptionVM.SelectedValue == FluenceSolutionDomainType.FluenceOfRhoAndZAndFt) || 
                    (MapTypeOptionVM.SelectedValue == MapType.PhotonHittingDensity &&
                     PhotonHittingDensitySolutionDomainTypeOptionVM.SelectedValue == FluenceSolutionDomainType.FluenceOfRhoAndZAndFt); 
            } 
        }

        public OptionViewModel<MapType> MapTypeOptionVM
        {
            get { return _MapTypeOptionVM; }
            set
            {
                _MapTypeOptionVM = value;
                OnPropertyChanged("MapTypeOptionVM");
            }
        }
        public FluenceSolutionDomainOptionViewModel FluenceSolutionDomainTypeOptionVM
        {
            get { return _FluenceSolutionDomainTypeOptionVM; }
            set
            {
                _FluenceSolutionDomainTypeOptionVM = value;
                OnPropertyChanged("FluenceSolutionDomainTypeOptionVM");
            }
        }
        public FluenceSolutionDomainOptionViewModel AbsorbedEnergySolutionDomainTypeOptionVM
        {
            get { return _AbsorbedEnergySolutionDomainTypeOptionVM; }
            set
            {
                _AbsorbedEnergySolutionDomainTypeOptionVM = value;
                OnPropertyChanged("AbsorbedEnergySolutionDomainTypeOptionVM");
            }
        }
        public FluenceSolutionDomainOptionViewModel PhotonHittingDensitySolutionDomainTypeOptionVM
        {
            get { return _PhotonHittingDensitySolutionDomainTypeOptionVM; }
            set
            {
                _PhotonHittingDensitySolutionDomainTypeOptionVM = value;
                OnPropertyChanged("PhotonHittingDensitySolutionDomainTypeOptionVM");
            }
        }
        public OptionViewModel<ForwardSolverType> ForwardSolverTypeOptionVM
        {
            get { return _ForwardSolverTypeOptionVM; }
            set
            {
                _ForwardSolverTypeOptionVM = value;
                OnPropertyChanged("ForwardSolverTypeOptionVM");
            }
        }
        public RangeViewModel RhoRangeVM
        {
            get { return _RhoRangeVM; }
            set
            {
                _RhoRangeVM = value;
                OnPropertyChanged("RhoRangeVM");
            }
        }
        public double SourceDetectorSeparation
        {
            get { return _SourceDetectorSeparation; }
            set
            {
                _SourceDetectorSeparation = value;
                OnPropertyChanged("SourceDetectorSeparation");
            }
        }
        public double TimeModulationFrequency
        {
            get { return _TimeModulationFrequency; }
            set
            {
                _TimeModulationFrequency = value;
                OnPropertyChanged("TimeModulationFrequency");
            }
        }
        public RangeViewModel ZRangeVM
        {
            get { return _ZRangeVM; }
            set
            {
                _ZRangeVM = value;
                OnPropertyChanged("ZRangeVM");
            }
        }

        public OpticalPropertyViewModel OpticalPropertyVM
        {
            get { return _OpticalPropertyVM; }
            set
            {
                _OpticalPropertyVM = value;
                OnPropertyChanged("OpticalPropertyVM");
            }
        }
        public object TissueInputVM
        {
            get { return _tissueInputVM; }
            private set
            {
                _tissueInputVM = value;
                OnPropertyChanged("TissueInputVM");
            }
        }

        void ExecuteFluenceSolver_Executed(object sender, ExecutedEventArgs e)
        {
            var mapData = ExecuteForwardSolver();

            Commands.Maps_PlotMap.Execute(mapData);

            //PlotAxesLabels axesLabels = GetPlotLabels();
            //Commands.Plot_SetAxesLabels.Execute(axesLabels);

            //string plotLabel = GetLegendLabel();
            //Commands.Plot_PlotValues.Execute(new PlotData(points, plotLabel));

            Commands.TextOutput_PostMessage.Execute("Fluence Solver: " + OpticalPropertyVM + "\r");
        }

        private PlotAxesLabels GetPlotLabels()
        {
            var sd = GetSelectedSolutionDomain();
            PlotAxesLabels axesLabels = null;
            if (sd.IndependentVariableAxisOptionVM.Options.Count > 1)
            {
                axesLabels = new PlotAxesLabels(
                    sd.IndependentAxisLabel, sd.IndependentAxisUnits, sd.IndependentAxisType,
                    sd.SelectedDisplayName, sd.SelectedValue.GetUnits(),
                    sd.ConstantAxisLabel, sd.ConstantAxisUnits, sd.ConstantAxisValue,
                    "", "", 0);// sd.ConstantAxisTwoLabel, sd.ConstantAxisTwoUnits, sd.ConstantAxisTwoValue); // wavelength-dependence not implemented for fluence
                    
            }
            else
            {
                axesLabels = new PlotAxesLabels(sd.IndependentAxisLabel, sd.IndependentAxisUnits, 
                    sd.IndependentAxisType, sd.SelectedDisplayName, sd.SelectedValue.GetUnits());
            }
            return axesLabels;
        }

        private object GetTissueInputVM(Vts.MonteCarlo.TissueType tissueType)
        {
            // ops to use as the basis for instantiating multi-region tissues based on homogeneous values (for differential comparison)
            if (_currentHomogeneousOpticalProperties == null)
            {
                _currentHomogeneousOpticalProperties = new OpticalProperties(0.01, 1, 0.8, 1.4);
            }

            switch (tissueType)
            {
                case MonteCarlo.TissueType.SemiInfinite:
                    if (_currentSemiInfiniteTissueInput == null)
                    {
                        _currentSemiInfiniteTissueInput = new SemiInfiniteTissueInput(new SemiInfiniteRegion(_currentHomogeneousOpticalProperties));
                    }
                    return new OpticalPropertyViewModel(
                        ((SemiInfiniteTissueInput)_currentSemiInfiniteTissueInput).Regions.First().RegionOP,
                         IndependentVariableAxisUnits.InverseMM.GetInternationalizedString(),
                        "Optical Properties:");
                    break;
                case MonteCarlo.TissueType.MultiLayer:
                    if (_currentMultiLayerTissueInput == null)
                    {
                        _currentMultiLayerTissueInput = new MultiLayerTissueInput(new ITissueRegion[]
                            { 
                                new LayerRegion(new DoubleRange(0, 2), _currentHomogeneousOpticalProperties.Clone() ), 
                                new LayerRegion(new DoubleRange(2, double.PositiveInfinity), _currentHomogeneousOpticalProperties.Clone() ), 
                            });
                    }
                    return new MultiRegionTissueViewModel(_currentMultiLayerTissueInput);
                case MonteCarlo.TissueType.SingleEllipsoid:
                    if (_currentSingleEllipsoidTissueInput == null)
                    {
                        _currentSingleEllipsoidTissueInput = new SingleEllipsoidTissueInput(
                            new EllipsoidRegion(new Position(0, 0, 10), 5, 5, 5, new OpticalProperties(0.05, 1.0, 0.8, 1.4)),
                            new ITissueRegion[]
                            { 
                                new LayerRegion(new DoubleRange(0, double.PositiveInfinity), _currentHomogeneousOpticalProperties.Clone()), 
                            });
                    }
                    return new MultiRegionTissueViewModel(_currentSingleEllipsoidTissueInput);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        // todo: rename? this was to get a concise name for the legend
        private string GetLegendLabel()
        {
            string modelString =
                ForwardSolverTypeOptionVM.SelectedValue == ForwardSolverType.DistributedPointSourceSDA ||
                ForwardSolverTypeOptionVM.SelectedValue == ForwardSolverType.PointSourceSDA ||
                ForwardSolverTypeOptionVM.SelectedValue == ForwardSolverType.DistributedGaussianSourceSDA
                ? "Model - SDA\r" : "Model - MC scaled\r";
            string opString = "μa=" + OpticalPropertyVM.Mua + "\rμs'=" + OpticalPropertyVM.Musp;
            if (IsMultiRegion)
            {
                ITissueRegion[] regions = null;
                if (ForwardSolver is TwoLayerSDAForwardSolver)
                {
                    regions = ((MultiRegionTissueViewModel)TissueInputVM).GetTissueInput().Regions;
                    opString =
                        "μa1=" + regions[0].RegionOP.Mua + " μs'1=" + regions[0].RegionOP.Musp + "\r" +
                        "μa2=" + regions[1].RegionOP.Mua + " μs'2=" + regions[1].RegionOP.Musp;
                }
            }
            else
            {
                var opticalProperties = ((OpticalPropertyViewModel)TissueInputVM).GetOpticalProperties();
                opString = "μa=" + opticalProperties.Mua + " \rμs'=" + opticalProperties.Musp;
            }

            return modelString + opString;
        }

        public MapData ExecuteForwardSolver()
        {
            //double[] rhos = RhoRangeVM.Values.Reverse().Concat(RhoRangeVM.Values).ToArray();
            double[] rhos = RhoRangeVM.Values.Reverse().Select(rho => -rho).Concat(RhoRangeVM.Values).ToArray();
            double[] zs = ZRangeVM.Values.ToArray();

            double[][] independentValues = new[] { rhos, zs };

            var sd = GetSelectedSolutionDomain();
            // todo: too much thinking at the VM layer?
            double[] constantValues =
                ComputationFactory.IsSolverWithConstantValues(sd.SelectedValue)
                    ? new double[] { sd.ConstantAxisValue } : new double[0];

            IndependentVariableAxis[] independentAxes = 
                GetIndependentVariableAxesInOrder(
                    sd.IndependentVariableAxisOptionVM.SelectedValue,
                    IndependentVariableAxis.Z);

            double[] results = null;
            if (ComputationFactory.IsComplexSolver(sd.SelectedValue))
            {

               Complex[] fluence =
                    ComputationFactory.ComputeFluenceComplex(
                        ForwardSolverTypeOptionVM.SelectedValue,
                        sd.SelectedValue,
                        independentAxes,
                        independentValues,
                        OpticalPropertyVM.GetOpticalProperties(),
                        constantValues);

                switch (MapTypeOptionVM.SelectedValue)
                {
                    case MapType.Fluence:
                        results = fluence.Select(f=>f.Magnitude).ToArray();
                        break;
                    case MapType.AbsorbedEnergy:
                        results = ComputationFactory.GetAbsorbedEnergy(fluence, OpticalPropertyVM.GetOpticalProperties().Mua).Select(a => a.Magnitude).ToArray(); // todo: is this correct?? DC 12/08/12
                        break;
                    case MapType.PhotonHittingDensity:
                        switch (PhotonHittingDensitySolutionDomainTypeOptionVM.SelectedValue)
                        {
                            case FluenceSolutionDomainType.FluenceOfRhoAndZAndFt:
                                results = ComputationFactory.GetPHD(
                                    ForwardSolverTypeOptionVM.SelectedValue,
                                    fluence.ToArray(),
                                    SourceDetectorSeparation,
                                    TimeModulationFrequency,
                                    new[] { OpticalPropertyVM.GetOpticalProperties() },
                                    independentValues[0],
                                    independentValues[1]).ToArray();
                                break;
                            case FluenceSolutionDomainType.FluenceOfFxAndZAndFt:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("FluenceSolutionDomainType");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("MapType");
                }

            }
            else
            {
                double[] fluence;
                if (IsMultiRegion)
                {
                    IOpticalPropertyRegion[] regions = null;
                    if (ForwardSolver is TwoLayerSDAForwardSolver)
                    {
                        regions = ((MultiRegionTissueViewModel) TissueInputVM).GetTissueInput().Regions
                            .Select(region => (IOpticalPropertyRegion)region).ToArray();
                    }
                    if (regions == null)
                    {
                        return null;
                    }
                    fluence = ComputationFactory.ComputeFluence(
                            ForwardSolverTypeOptionVM.SelectedValue,
                            sd.SelectedValue,
                            independentAxes,
                            independentValues,
                            regions,
                            constantValues).ToArray();
                }
                else
                {
                    fluence = ComputationFactory.ComputeFluence(
                            ForwardSolverTypeOptionVM.SelectedValue,
                            sd.SelectedValue,
                            independentAxes,
                            independentValues,
                            OpticalPropertyVM.GetOpticalProperties(),
                            constantValues).ToArray();
                }

                switch (MapTypeOptionVM.SelectedValue)
                    {
                        case MapType.Fluence:
                            results = fluence;
                            break;
                        case MapType.AbsorbedEnergy:
                            if (IsMultiRegion)
                            {
                                if (ForwardSolver is TwoLayerSDAForwardSolver)
                                {
                                    var regions = ((MultiRegionTissueViewModel)TissueInputVM).GetTissueInput().Regions
                                        .Select(region => (ILayerOpticalPropertyRegion)region).ToArray();
                                    var muas = ComputationFactory.getRhoZMuaArrayFromLayerRegions(regions, rhos, zs);
                                    results = ComputationFactory.GetAbsorbedEnergy(fluence, muas).ToArray();
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                // Note: the line below was originally overwriting the multi-region results. I think this was a bug (DJC 7/11/14)
                                results = ComputationFactory.GetAbsorbedEnergy(fluence, OpticalPropertyVM.GetOpticalProperties().Mua).ToArray(); 
                            }
                            break;
                        case MapType.PhotonHittingDensity:
                            switch (PhotonHittingDensitySolutionDomainTypeOptionVM.SelectedValue)
                            {
                                case FluenceSolutionDomainType.FluenceOfRhoAndZ:
                                    results = ComputationFactory.GetPHD(
                                        ForwardSolverTypeOptionVM.SelectedValue,
                                        fluence,
                                        SourceDetectorSeparation,
                                        new[] { OpticalPropertyVM.GetOpticalProperties() },
                                        independentValues[0],
                                        independentValues[1]).ToArray();
                                    break;
                                case FluenceSolutionDomainType.FluenceOfFxAndZ:
                                    break;
                                case FluenceSolutionDomainType.FluenceOfRhoAndZAndTime:
                                    break;
                                case FluenceSolutionDomainType.FluenceOfFxAndZAndTime:
                                    break;
                                default:
                                throw new ArgumentOutOfRangeException("PhotonHittingDensitySolutionDomainTypeOptionVM.SelectedValue");
                            }
                            break;
                    default:
                        throw new ArgumentOutOfRangeException("MapTypeOptionVM.SelectedValue");
                    }  
                }

            // flip the array (since it goes over zs and then rhos, while map wants rhos and then zs
            double[] destinationArray = new double[results.Length];
            long index = 0;
            for (int rhoi = 0; rhoi < rhos.Length; rhoi++)
            {
                for (int zi = 0; zi < zs.Length; zi++)
                {
                    destinationArray[rhoi + rhos.Length * zi] = results[index++];
                }
            }

            var dRho = 1D;
            var dZ = 1D;
            var dRhos = Enumerable.Select(rhos, rho => 2 * Math.PI * Math.Abs(rho) * dRho).ToArray();
            var dZs = Enumerable.Select(zs, z => dZ).ToArray();
            //var twoRhos = Enumerable.Concat(rhos.Reverse(), rhos).ToArray();
            //var twoDRhos = Enumerable.Concat(dRhos.Reverse(), dRhos).ToArray();

            return new MapData(destinationArray, rhos, zs, dRhos, dZs);
        }

        private static IndependentVariableAxis[] GetIndependentVariableAxesInOrder(params IndependentVariableAxis[] axes)
        {
            if (axes.Length <= 0)
                throw new ArgumentNullException("axes");

            var sortedAxes = axes.OrderBy(ax => ax.GetMaxArgumentLocation()).ToArray();

            return sortedAxes;
        }

        private FluenceSolutionDomainOptionViewModel GetSelectedSolutionDomain()
        {
            switch (MapTypeOptionVM.SelectedValue)
            {
                case MapType.Fluence:
                    return this.FluenceSolutionDomainTypeOptionVM;
                case MapType.AbsorbedEnergy:
                    return AbsorbedEnergySolutionDomainTypeOptionVM;
                case MapType.PhotonHittingDensity:
                    return PhotonHittingDensitySolutionDomainTypeOptionVM;
                default:
                    throw new ArgumentException("No solution domain of the specified type exists.", "MapTypeOptionVM.SelectedValue");
            }
        }
    }
}
