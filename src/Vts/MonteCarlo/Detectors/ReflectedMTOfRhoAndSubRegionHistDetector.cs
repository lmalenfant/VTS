using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Runtime.Serialization;
using System.Linq;
using Vts.Common;
using Vts.MonteCarlo.Helpers;
using Vts.MonteCarlo.PhotonData;

namespace Vts.MonteCarlo.Detectors
{
    /// <summary>
    /// Tally for ReflectedMT as a function of Rho and Z.
    /// This works for Analog and DAW processing.
    /// </summary>
    public class ReflectedMTOfRhoAndSubregionHistDetectorInput : DetectorInput, IDetectorInput
    {
        /// <summary>
        /// constructor for ReflectedMT as a function of rho and tissue subregion detector input
        /// </summary>
        public ReflectedMTOfRhoAndSubregionHistDetectorInput()
        {
            TallyType = "ReflectedMTOfRhoAndSubregionHist";
            Name = "ReflectedMTOfRhoAndSubregionHist";
            Rho = new DoubleRange(0.0, 10, 101);
            // NEED TO ASK DC: how do I get tissue in this default constructor?
            //SubregionIndices = new IntRange(0, _tissue.Regions.Count - 1, _tissue.Regions.Count); // needed for DetectorIO
            //NumSubregions = 3;
            MTBins = new DoubleRange(0.0, 500.0, 51);

            // modify base class TallyDetails to take advantage of built-in validation capabilities (error-checking)
            TallyDetails.IsReflectanceTally = true;
            TallyDetails.IsCylindricalTally = true;
            TallyDetails.IsNotImplementedForCAW = true;
        }

        /// <summary>
        /// rho binning
        /// </summary>
        public DoubleRange Rho { get; set; }
        ///// <summary>
        ///// subregion index binning, needed by DetectorIO
        ///// </summary>
        //public int NumSubregions { get; set; }
        /// <summary>
        /// momentum transfer binning
        /// </summary>
        public DoubleRange MTBins { get; set; }

        /// <summary>
        /// fractional momentum transfer binning
        /// </summary>
        public DoubleRange FractionalMTBins { get; set; }
        
        public IDetector CreateDetector()
        {
            return new ReflectedMTOfRhoAndSubregionHistDetector
            {
                // required properties (part of DetectorInput/Detector base classes)
                TallyType = this.TallyType,
                Name = this.Name,
                TallySecondMoment = this.TallySecondMoment,
                TallyDetails = this.TallyDetails,

                // optional/custom detector-specific properties
                Rho = this.Rho,
                //NumSubregions = this.NumSubregions,
                MTBins = this.MTBins,
                FractionalMTBins = this.FractionalMTBins
            };
        }
    }

    /// <summary>
    /// Implements IDetector.  Tally for momentum transfer as a function  of Rho and tissue subregion.
    /// This implementation works for Analog, DAW and CAW processing.
    /// </summary>
    public class ReflectedMTOfRhoAndSubregionHistDetector : Detector, IDetector
    {
        private Func<PhotonDataPoint, PhotonDataPoint, int, double> _absorptionWeightingMethod;
        private ITissue _tissue;
        private IList<OpticalProperties> _ops;

        /* ==== Place optional/user-defined input properties here. They will be saved in text (JSON) format ==== */
        /* ==== Note: make sure to copy over all optional/user-defined inputs from corresponding input class ==== */
        /// <summary>
        /// rho binning
        /// </summary>
        public DoubleRange Rho { get; set; }
        /// <summary>
        /// momentum transfer binning
        /// </summary>
        public DoubleRange MTBins { get; set; }
        /// <summary>
        /// fractional momentum transfer binning
        /// </summary>
        public DoubleRange FractionalMTBins { get; set; }

        /* ==== Place user-defined output arrays here. They should be prepended with "[IgnoreDataMember]" attribute ==== */
        /* ==== Then, GetBinaryArrays() should be implemented to save them separately in binary format ==== */
        /// <summary>
        /// detector mean
        /// </summary>
        [IgnoreDataMember] 
        public double[,] Mean { get; set; }

        /// <summary>
        /// detector second moment
        /// </summary>
        [IgnoreDataMember]
        public double[,] SecondMoment { get; set; }

        /// <summary>
        /// fraction of MT spent in each subregion
        /// </summary>
        [IgnoreDataMember]
        public double[,,,] FractionalMT { get; set; }

        /* ==== Place optional/user-defined output properties here. They will be saved in text (JSON) format ==== */
        /// <summary>
        /// number of Zs detector gets tallied to
        /// </summary>
        public long TallyCount { get; set; }
        /// <summary>
        /// Z binning
        /// </summary>
        public int NumSubregions { get; set; }

        public void Initialize(ITissue tissue)
        {
            // intialize any necessary class fields here
            _absorptionWeightingMethod = AbsorptionWeightingMethods.GetVolumeAbsorptionWeightingMethod(tissue, this);
            _tissue = tissue;
            _ops = _tissue.Regions.Select(r => r.RegionOP).ToArray();

            // assign any user-defined outputs (except arrays...we'll make those on-demand)
            TallyCount = 0;
            NumSubregions = _tissue.Regions.Count;

            // if the data arrays are null, create them (only create second moment if TallySecondMoment is true)
            Mean = Mean ?? new double[Rho.Count - 1, MTBins.Count - 1];
            SecondMoment = SecondMoment ?? (TallySecondMoment ? new double[Rho.Count - 1, MTBins.Count - 1] : null);
            FractionalMT = FractionalMT ?? new double[Rho.Count - 1, MTBins.Count - 1, NumSubregions, FractionalMTBins.Count - 1];
        }

        /// <summary>
        /// method to tally to detector
        /// </summary>
        /// <param name="photon">photon data needed to tally</param>
        public void Tally(Photon photon)
        {
            // calculate the radial bin to attribute the deposition
            var ir = DetectorBinning.WhichBin(DetectorBinning.GetRho(photon.DP.Position.X, photon.DP.Position.Y), Rho.Count - 1, Rho.Delta, Rho.Start);
            var subregionMT = new double[NumSubregions];
            bool talliedMT = false;

            // go through photon history and claculate momentum transfer
            // assumes that no MT tallied at pseudo-collisions (reflections and refractions)
            // this algorithm needs to look ahead to angle of next DP, but needs info from previous to determine whether real or pseudo-collision
            PhotonDataPoint previousDP = photon.History.HistoryData.First();
            PhotonDataPoint currentDP = photon.History.HistoryData.Skip(1).Take(1).First();
            foreach (PhotonDataPoint nextDP in photon.History.HistoryData.Skip(2))
            {
                if (previousDP.Weight != currentDP.Weight) // only for true collision points
                {
                    var isr = _tissue.GetRegionIndex(currentDP.Position); // get current region index
                    // get angle between current and next
                    double cosineBetweenTrajectories = Direction.GetDotProduct(currentDP.Direction, nextDP.Direction);
                    var momentumTransfer = 1 - cosineBetweenTrajectories;
                    subregionMT[isr] += momentumTransfer;
                    talliedMT = true;
                }
                previousDP = currentDP;
                currentDP = nextDP;
            }
            // tally total MT
            double totalMT = subregionMT.Sum();
            if (totalMT > 0.0)  // only tally if momentum transfer accumulated
            {
                var imt = DetectorBinning.WhichBin(totalMT, MTBins.Count - 1, MTBins.Delta, MTBins.Start);
                Mean[ir, imt] += photon.DP.Weight;
                if (TallySecondMoment)
                {
                    SecondMoment[ir, imt] += photon.DP.Weight * photon.DP.Weight;                    
                }

                if (talliedMT) TallyCount++;

                // tally fractional MT in each subregion
                for (int isr = 0; isr < NumSubregions; isr++)
                {
                    var ifrac = DetectorBinning.WhichBin(subregionMT[isr] / totalMT,
                                                         FractionalMTBins.Count - 1, FractionalMTBins.Delta, FractionalMTBins.Start);
                    FractionalMT[ir, imt, isr, ifrac] += photon.DP.Weight;
                }
            }
        }

        /// <summary>
        /// method to normalize detector results after all photons launched
        /// </summary>
        /// <param name="numPhotons">number of photons launched</param>
        public void Normalize(long numPhotons)
        {
            var normalizationFactor = 2.0 * Math.PI * Rho.Delta;
            for (int ir = 0; ir < Rho.Count - 1; ir++)
            {
                for (int imt = 0; imt < MTBins.Count - 1; imt++)
                {
                    // normalize by area of surface area ring and N
                    var areaNorm = (Rho.Start + (ir + 0.5) * Rho.Delta) * normalizationFactor;
                    Mean[ir, imt] /= areaNorm * numPhotons;
                    if (TallySecondMoment)
                    {
                        SecondMoment[ir, imt] /= areaNorm * areaNorm * numPhotons;
                    }
                    for (int isr = 0; isr < NumSubregions; isr++)
                    {
                        for (int ifrac = 0; ifrac < FractionalMTBins.Count - 1; ifrac++)
                        {
                            FractionalMT[ir, imt, isr, ifrac] /= areaNorm*numPhotons;
                        }
                    }
                }
            }
        }
        // this is to allow saving of large arrays separately as a binary file
        public BinaryArraySerializer[] GetBinarySerializers()
        {
            return new[] {
                new BinaryArraySerializer {
                    DataArray = Mean,
                    Name = "Mean",
                    FileTag = "",
                    WriteData = binaryWriter => {
                        for (int i = 0; i < Rho.Count - 1; i++) {
                            for (int j = 0; j < FractionalMTBins.Count - 1; j++)
                            {                                
                                binaryWriter.Write(Mean[i, j]);
                            }
                        }
                    },
                    ReadData = binaryReader => {
                        Mean = Mean ?? new double[ Rho.Count - 1, MTBins.Count - 1];
                        for (int i = 0; i <  Rho.Count - 1; i++) {
                            for (int j = 0; j < MTBins.Count - 1; j++)
                            {
                               Mean[i, j] = binaryReader.ReadDouble(); 
                            }
                        }
                    }
                },
                new BinaryArraySerializer {
                    DataArray = SecondMoment,
                    Name = "SecondMoment",
                    FileTag = "_2",
                    WriteData = binaryWriter => {
                        if(!TallySecondMoment) return;
                        for (int i = 0; i < Rho.Count - 1; i++) {
                            for (int j = 0; j < MTBins.Count - 1; j++)
                            {
                                binaryWriter.Write(SecondMoment[i, j]);
                            }                            
                        }
                    },
                    ReadData = binaryReader => {
                        if(!TallySecondMoment) return;
                        SecondMoment = SecondMoment ?? new double[ Rho.Count - 1, MTBins.Count - 1];
                        for (int i = 0; i < Rho.Count - 1; i++) {
                            for (int j = 0; j < MTBins.Count - 1; j++)
                            {
                                SecondMoment[i, j] = binaryReader.ReadDouble();
                            }                       
			            }
                    },
                },
            };
        }
        /// <summary>
        /// Method to determine if photon is within detector
        /// </summary>
        /// <param name="dp">photon data point</param>
        /// <returns>method always returns true</returns>
        public bool ContainsPoint(PhotonDataPoint dp)
        {
            return true; // or, possibly test for NA or confined position, etc
            //return (dp.StateFlag.Has(PhotonStateType.PseudoTransmissionDomainTopBoundary));
        }

    }
}
