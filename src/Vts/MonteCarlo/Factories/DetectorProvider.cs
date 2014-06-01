﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using Vts.Common;
using Vts.IO;
using AutoMapper;
using Vts.MonteCarlo.IO;

namespace Vts.MonteCarlo.Factories
{
    // interfaces
    public interface IDetectorProvider<TDetector, TDetectorInput, TDetectorOutput> : IProvider<IDetector>
        where TDetector : IDetector
        where TDetectorInput : IDetectorInput
        where TDetectorOutput : IDetectorOutput
    {
        Func<string, TDetectorInput> ReadInputFromXML { get; set; }
        Func<string, string, TDetectorInput> ReadInputFromXMLInResources { get; set; }
        Action<TDetectorInput, string> WriteInputToXML { get; set; }
        Func<TDetectorInput, TDetector> CreateDetector { get; set; }
        Func<TDetector, TDetectorOutput> CreateOutput { get; set; }
    }

    public interface IProvider<IDetector>
    {
        Type TargetType { get; set; }
    }

    //public interface IOutput<TDetector>
    //    where TDetector : IDetector
    //{
    //    string Name { get; set; }
    //}

    public interface IDetectorOutput
    {
        int[] Dimensions { get; set; }
        string Name { get; set; }
        string TallyType { get; set; }
    }

    public interface IDetectorOutput<T> : IDetectorOutput
    {
        /// <summary>
        /// Mean of detector tally
        /// </summary>
        T Mean { get; set; }
        /// <summary>
        /// Second moment of detector tally
        /// </summary>
        T SecondMoment { get; set; }
    }


    public class TallyDetails
    {
        public bool IsReflectanceTally { get; set; }
        public bool IsTransmittanceTally { get; set; }
        public bool IsSpecularReflectanceTally { get; set; }
        public bool IsInternalSurfaceTally { get; set; }
        public bool IspMCReflectanceTally { get; set; }
        public bool IsDosimetryTally { get; set; }
        public bool IsVolumeTally { get; set; }
        public bool IsCylindricalTally { get; set; }
        public bool IsNotImplementedForCAW { get; set; }
        public bool IsNotImplementedYet { get; set; }
    }

    // base class implementations

    ///// <summary>
    ///// Base class for all detectors.
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public abstract class DetectorBase<T> : IDetector<T>
    //{
    //    private T _mean;
    //    private T _secondMoment;
    //    private int[] _dimensions;
        
    //    protected DetectorBase()
    //    {
    //        TallySecondMoment = false;
    //        Name = "";

    //        TallyDetails = new TallyDetails
    //        {
    //            IsReflectanceTally = false,
    //            IsTransmittanceTally = false,
    //            IsSpecularReflectanceTally = false,
    //            IsInternalSurfaceTally = false,
    //            IspMCReflectanceTally = false,
    //            IsDosimetryTally = false,
    //            IsVolumeTally = false,
    //            IsCylindricalTally = false,
    //            IsNotImplementedForCAW = false,
    //            IsNotImplementedYet = false
    //        };
    //    }

    //    public string Name { get; set; } // shouldn't have public set_Name (need constructor injection for AutoMapper)
    //    public string TallyType { get; set; } // shouldn't have public set_TallyType (need constructor injection for AutoMapper)

    //    public T Mean
    //    {
    //        get
    //        {
    //            if (_mean == null != null)
    //            {
    //                _dimensions = GetDimensions();
    //                _mean = (T)((dynamic)Array.CreateInstance(typeof(T).GetElementType(), _dimensions));
    //            }
    //            return _mean;
    //        }
    //    }

    //    public T SecondMoment
    //    {
    //        get
    //        {
    //            if (_secondMoment == null && TallySecondMoment)
    //            {
    //                _dimensions = GetDimensions();
    //                _secondMoment = (T)((dynamic)Array.CreateInstance(typeof(T).GetElementType(), _dimensions));
    //            }
    //            return _secondMoment;
    //        }
    //    }

    //    public int[] Dimensions
    //    {
    //        get { return _dimensions; }
    //    }

    //    public bool TallySecondMoment { get; set; }
    //    public long TallyCount { get; set; } // shouldn't have public set_TallyCount

    //    protected TallyDetails TallyDetails { get; set; }

    //    protected abstract int[] GetDimensions();
    //    public abstract void Tally(Photon photon);
    //    public abstract void Normalize(long numPhotons);

    //    public bool IsSurfaceTally()
    //    {
    //        return TallyDetails.IsTransmittanceTally || TallyDetails.IsReflectanceTally ||
    //               TallyDetails.IsSpecularReflectanceTally || TallyDetails.IsInternalSurfaceTally;
    //    }
        
    //    public bool IsReflectanceTally() { return TallyDetails.IsReflectanceTally; }
    //    public bool IsTransmittanceTally() { return TallyDetails.IsTransmittanceTally; }
    //    public bool IsSpecularReflectanceTally() { return TallyDetails.IsSpecularReflectanceTally; }
    //    public bool IsInternalSurfaceTally() { return TallyDetails.IsInternalSurfaceTally; }
    //    public bool IspMCReflectanceTally() { return TallyDetails.IspMCReflectanceTally; }
    //    public bool IsVolumeTally() { return TallyDetails.IsVolumeTally; }
    //    public bool IsCylindricalTally() { return TallyDetails.IsCylindricalTally; }
    //    public bool IsNotImplementedForCAW() { return TallyDetails.IsNotImplementedForCAW; }
    //    public bool IsNotImplementedYet() { return TallyDetails.IsNotImplementedYet; }

    //}

    //public class DetectorProvider<TDetectorInput, TDetector, TDetectorOutput>
    //    where TDetector : IDetector
    //    where TDetectorOutput : IDetectorOutput
    //{
    //    static DetectorProvider()
    //    {
    //        Mapper.CreateMap<TDetectorInput, TDetector>();
    //        Mapper.CreateMap<TDetector, TDetectorOutput>();

    //        KnownTypes.Add(typeof(TDetectorInput));
    //        KnownTypes.Add(typeof(TDetectorOutput));
    //    }

    //    //public static DetectorProvider<TIn, TDet, TOut> Create<TIn, TDet, TOut>(Type inputType, Type detectorType, Type outputType)
    //    //{
    //    //    MethodInfo genericMethod = typeof(DetectorProvider).GetMethod("ContainSameValues");
    //    //    return new DetectorProvider<TIn, TDet, TOut>();
    //    //}

    //    public DetectorProvider()
    //    {
    //        CreateDetector = input => Mapper.Map<TDetectorInput, TDetector>(input);
    //        CreateOutput = detector => Mapper.Map<TDetector, TDetectorOutput>(detector);

    //        ReadInputFromFile = filename => Vts.IO.FileIO.ReadFromXML<TDetectorInput>(filename);
    //        WriteInputToFile = (input, filename) => Vts.IO.FileIO.WriteToXML(input, filename);
    //        ReadInputFromResources = (filename, projectName) => Vts.IO.FileIO.ReadFromXMLInResources<TDetectorInput>(filename, projectName);

    //        WriteOutputToFile = (output, filename) => DetectorIO.WriteDetectorOutputToFile(output, filename);
    //        ReadOutputFromFile = (filename, folderPath) => DetectorIO.ReadDetectorOutputFromFile<TDetectorOutput>(filename, folderPath);

    //        TargetType = typeof(TDetector);
    //    }

    //    public Type TargetType { get; set; }
    //    public Func<TDetectorInput, TDetector> CreateDetector { get; set; }
    //    public Func<TDetector, TDetectorOutput> CreateOutput { get; set; }
    //    public Func<string, TDetectorInput> ReadInputFromFile { get; set; }
    //    public Func<string, string, TDetectorInput> ReadInputFromResources { get; set; }
    //    public Action<TDetectorInput, string> WriteInputToFile { get; set; }
    //    public Func<string, string, TDetectorOutput> ReadOutputFromFile { get; set; }
    //    public Action<TDetectorOutput, string> WriteOutputToFile { get; set; }

    //}


    //public class DetectorProvider2<TDetectorInput, TDetector, TDetectorOutput>
    //    where TDetector : IDetector2
    //    where TDetectorOutput : IDetectorOutput2
    //{
    //    static DetectorProvider2()
    //    {
    //        KnownTypes.Add(typeof(TDetectorInput));
    //        KnownTypes.Add(typeof(TDetectorOutput));
    //    }

    //    //public static DetectorProvider<TIn, TDet, TOut> Create<TIn, TDet, TOut>(Type inputType, Type detectorType, Type outputType)
    //    //{
    //    //    MethodInfo genericMethod = typeof(DetectorProvider).GetMethod("ContainSameValues");
    //    //    return new DetectorProvider<TIn, TDet, TOut>();
    //    //}

    //    public DetectorProvider2()
    //    {
    //        CreateDetector = input => Mapper.Map<TDetectorInput, TDetector>(input);
    //        CreateOutput = detector => Mapper.Map<TDetector, TDetectorOutput>(detector);

    //        ReadInputFromFile = filename => Vts.IO.FileIO.ReadFromXML<TDetectorInput>(filename);
    //        WriteInputToFile = (input, filename) => Vts.IO.FileIO.WriteToXML(input, filename);
    //        ReadInputFromResources = (filename, projectName) => Vts.IO.FileIO.ReadFromXMLInResources<TDetectorInput>(filename, projectName);

    //        WriteOutputToFile = (output, filename) => DetectorIO.WriteDetectorOutputToFile(output, filename);
    //        ReadOutputFromFile = (filename, folderPath) => DetectorIO.ReadDetectorOutputFromFile<TDetectorOutput>(filename, folderPath);

    //        TargetType = typeof(TDetector);
    //    }

    //    public Type TargetType { get; set; }
    //    public Func<TDetectorInput, TDetector> CreateDetector { get; set; }
    //    public Func<TDetector, TDetectorOutput> CreateOutput { get; set; }
    //    public Func<string, TDetectorInput> ReadInputFromFile { get; set; }
    //    public Func<string, string, TDetectorInput> ReadInputFromResources { get; set; }
    //    public Action<TDetectorInput, string> WriteInputToFile { get; set; }
    //    public Func<string, string, TDetectorOutput> ReadOutputFromFile { get; set; }
    //    public Action<TDetectorOutput, string> WriteOutputToFile { get; set; }

    //}

    //////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Properties and methods that all IDetectors must implement
    /// </summary>
    public interface IDetector2<out TInput> where TInput : IDetectorInput
    {
        TInput DetectorInput { get; }

        /// <summary>
        /// TallyType enum specification
        /// </summary>
        string TallyType { get; }

        /// <summary>
        /// Name string of IDetector.  Default = TallyType.ToString().
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Details of the tally - booleans that specify when they should be tallied
        /// </summary>
        TallyDetails TallyDetails { get; set; }

        /// <summary>
        /// Method to normalize the tally to get Mean and Second Moment estimates
        /// </summary>
        /// <param name="numPhotons">number of photons launched</param>
        void Normalize(long numPhotons);

        /// <summary>
        /// Method to tally to detector using information in Photon
        /// </summary>
        /// <param name="photon">photon data needed to tally</param>
        void Tally(Photon photon);

        BinaryArrayInfo[] GetAllBinaryArrayInfo();
    }

    public interface IDetectorInput<out TDetector> : IDetectorInput where TDetector : IDetector2<IDetectorInput<TDetector>>
    {
        bool TallySecondMoment { get; }
        TDetector CreateDetector();
    }

    public class BinaryArrayInfo
    {
        public Array DataArray { get; set; }
        public string Name { get; set; }
        public int[] Dimensions { get; set; }
    }

    // user code
    public class FancyDetectorInput : IDetectorInput<FancyDetector> // marks that FancyDetector is associated with FancyDetectorInput
    {
        public FancyDetectorInput()
        {
            // assign defaults for mandatory values
            TallyType = "Fancy";
            Name = "test";

            // assign defaults for optional values
            XRange = new DoubleRange(0, 1, 10);
            YRange = new DoubleRange(0, 1, 10);
            TallySecondMoment = true;
        }

        public string TallyType { get; set; } // required (IDetectorInput contract)
        public string Name { get; set; } // required (IDetectorInput contract)
        public DoubleRange XRange { get; set; }
        public DoubleRange YRange { get; set; }
        public bool TallySecondMoment { get; set; }

        public FancyDetector CreateDetector() // required (IDetectorInput contract)
        {
            return new FancyDetector(this);
        }
    }

    public interface IAbstractDetector2<out TDetector> where TDetector : IDetector2<IDetectorInput<TDetector>>
    {
    }

    public class AbstractDetector2<TDetector> : IAbstractDetector2<TDetector>where TDetector : IDetector2<IDetectorInput<TDetector>>
    {
        public AbstractDetector2(IDetectorInput<TDetector> detectorInput)
        {
            DetectorInput = detectorInput;
            TallyCount = 0;
        }

        /* ==== These public properties are required (IDetector contract), and will be saved in text (JSON) format ==== */
        public IDetectorInput<TDetector> DetectorInput { get; set; }
        public string TallyType { get { return DetectorInput.TallyType; } }
        public string Name { get { return DetectorInput.Name; } }
        public TallyDetails TallyDetails { get; set; }
        public long TallyCount { get; set; }
    }

    public class FancyDetector : AbstractDetector2<FancyDetector>, IDetector2<IDetectorInput<FancyDetector>> // marks that FancyDetectorInput is associated with FancyDetector
    {
        // put private variables here that you only want to use internally and don't want saved
        private Random _rng;

        // class constructor, where properties and fields get initialized
        public FancyDetector(FancyDetectorInput detectorInput) : base(detectorInput)
        {
            // assign mandatory values 
            TallyDetails = new TallyDetails()
            {
                IsReflectanceTally = false,
                IsTransmittanceTally = false,
                IsSpecularReflectanceTally = false,
                IsInternalSurfaceTally = false,
                IspMCReflectanceTally = true, // ours is a simple x-y reflectance tally
                IsDosimetryTally = false,
                IsVolumeTally = false,
                IsCylindricalTally = false,
                IsNotImplementedForCAW = false,
                IsNotImplementedYet = false
            };

            // assign any user-defined public properties (except arrays...we'll make those on-demand)
            Nx = detectorInput.XRange.Count;
            Ny = detectorInput.YRange.Count;

            // assign any additional private members that you'll need for the Tally/Normalize/GetAllBinaryArrayInfo methods
            _rng = new Random(); // for demo purposes only 
        }

        /* ==== The remaining public properties are user-defined, and by default will be saved in text (JSON) format ==== */
        /* ==== Prepend data arrays with "[IgnoreDataMember]" and implement to save separately in binary format ==== */
        public int Nx { get; set; }
        public int Ny { get; set; }
        [IgnoreDataMember] public double[,] Mean { get; set; }
        [IgnoreDataMember] public double[,] SecondMoment { get; set; }

        /* ==== These public methods are required, in order for the class to obey the IDetector contract ==== */
        public void Tally(Photon photon)
        {
            // if this is the first time calling this method, create the matrices
            if (Mean == null) {
                Mean = new double[Nx, Ny];
            }
            if (SecondMoment == null  && DetectorInput.TallySecondMoment) {
                SecondMoment = new double[Nx, Ny];
            }

            // for demo purposes, just place in random x-y bins
            var xIndex = _rng.Next(0, Nx - 1);
            var yIndex = _rng.Next(0, Ny - 1);

            Mean[xIndex, yIndex] += photon.DP.Weight;
            SecondMoment[xIndex, yIndex] += photon.DP.Weight*photon.DP.Weight;
            TallyCount++;
        }

        public void Normalize(long numPhotons)
        {
            if (Mean == null)
                return;

            for (int i = 0; i < Nx; i++) {
                for (int j = 0; j < Ny; j++) {
                    Mean[i, j] /= TallyCount;
                }
            }

            if (DetectorInput.TallySecondMoment){
                if (SecondMoment == null)
                    return;
                for (int i = 0; i < Nx; i++) {
                    for (int j = 0; j < Ny; j++) {
                        SecondMoment[i, j] /= TallyCount;
                    }
                }
            }
        }

        public BinaryArrayInfo[] GetAllBinaryArrayInfo()
        {
            return new[] {
                new BinaryArrayInfo {
                    DataArray = Mean,
                    Name = "Mean",
                    Dimensions = new[] {Nx, Ny}
                },
                new BinaryArrayInfo {
                    DataArray = SecondMoment,
                    Name = "SecondMoment",
                    Dimensions = new[] {Nx, Ny}
                },
            };
        }
    }

    ///// <summary>
    ///// Class to hold information necessary for creating detector
    ///// </summary>
    //public class SampleDetectorInput : IDetectorInput
    //{
    //    public SampleDetectorInput()
    //    {
    //        TallyType = "ROfFx";
    //        Name = "ROfFx";
    //        QRange = new DoubleRange(0, 1, 10);
    //    }

    //    public string TallyType { get; set; }
    //    public string Name { get; set; }
    //    public DoubleRange QRange { get; set; }
    //}

    ///// <summary>
    ///// Acutal detector class implementation
    ///// </summary>
    //public class SampleDetector : DetectorBase<double[]>
    //{
    //    private static int _tempIndex = -1;

    //    public SampleDetector()
    //    {
    //        QRange = new DoubleRange(0, 1, 20);
    //        TallySecondMoment = false;
    //        Name = "SampleDetector";

    //        TallyDetails.IsReflectanceTally = true;
    //    }

    //    public DoubleRange QRange { get; set; }

    //    public override void Tally(Photon photon)
    //    {
    //        Mean[(_tempIndex++) % Dimensions[0]] += photon.DP.Weight;
    //        TallyCount++;
    //    }

    //    public override void Normalize(long numPhotons)
    //    {
    //        for (int i = 0; i < Dimensions[0]; i++)
    //        {
    //            Mean[i] /= TallyCount;
    //        }
    //    }

    //    protected override int[] GetDimensions()
    //    {
    //        return new int[] { QRange.Count - 1 };
    //    }
    //}

    ///// <summary>
    ///// Class representing detector data to save/store
    ///// </summary>
    //public class DetectorOutput<T> : IDetectorOutput<T>
    //{
    //    [IgnoreDataMember]
    //    public T Mean { get; set; }

    //    [IgnoreDataMember]
    //    public T SecondMoment { get; set; }

    //    public int[] Dimensions { get; set; }
    //    public string Name { get; set; }
    //    public string TallyType { get; set; }
    //}

    //[Export(typeof(IDetectorOutput))]
    //[ExportMetadata("Target Class", "SampleDetector")]
    //public class SampleDetectorOutput : DetectorOutput<double[]>
    //{
    //    public DoubleRange QRange { get; set; }
    //}

    ///// <summary>
    ///// Class that glues all the pieces together. In most cases, there shouldn't be any extra work to do here
    ///// </summary>
    //public class SampleDetectorProvider
    //    : DetectorProvider<SampleDetectorInput, SampleDetector, SampleDetectorOutput>
    //{
    //}
}
