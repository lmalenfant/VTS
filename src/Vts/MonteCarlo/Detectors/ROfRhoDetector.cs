using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Vts.Common;
using Vts.MonteCarlo.Helpers;
using Vts.MonteCarlo.PhotonData;

namespace Vts.MonteCarlo.Detectors
{
    [KnownType(typeof(ROfRhoDetector))]
    /// <summary>
    /// Implements ITerminationTally<double[]>.  Tally for reflectance as a function 
    /// of Rho.
    /// This implementation works for Analog, DAW and CAW processing.
    /// </summary>
    public class ROfRhoDetector : ITerminationDetector<double[]>
    {
        private bool _tallySecondMoment;
        /// <summary>
        /// Returns an instance of ROfRhoDetector
        /// </summary>
        /// <param name="rho"></param>
        public ROfRhoDetector(DoubleRange rho, bool tallySecondMoment, String name)
        {
            Rho = rho;
            _tallySecondMoment = tallySecondMoment;
            Mean = new double[Rho.Count - 1];
            if (_tallySecondMoment)
            {
                SecondMoment = new double[Rho.Count - 1];
            }
            else
            {
                SecondMoment = null;
            }
            TallyType = TallyType.ROfRho;
            Name = name;
            TallyCount = 0;
        }

        /// <summary>
        ///  Returns a default instance of ROfRhoDetector (for serialization purposes only)
        /// </summary>
        public ROfRhoDetector()
            : this(new DoubleRange(), true, TallyType.ROfRho.ToString())
        {
        }

        [IgnoreDataMember]
        public double[] Mean { get; set; }

        [IgnoreDataMember]
        public double[] SecondMoment { get; set; }

        public TallyType TallyType { get; set; }

        public String Name { get; set; }

        public long TallyCount { get; set; }

        public DoubleRange Rho { get; set; }

        public void Tally(PhotonDataPoint dp)
        {
            var ir = DetectorBinning.WhichBin(DetectorBinning.GetRho(dp.Position.X, dp.Position.Y), Rho.Count - 1, Rho.Delta, Rho.Start);

            Mean[ir] += dp.Weight;
            if (_tallySecondMoment)
            {
                SecondMoment[ir] += dp.Weight * dp.Weight;
            }
            TallyCount++;
        }

        public void Normalize(long numPhotons)
        {
            var normalizationFactor = 2.0 * Math.PI * Rho.Delta * Rho.Delta;
            for (int ir = 0; ir < Rho.Count - 1; ir++)
            {
                var areaNorm = (ir + 0.5) * normalizationFactor;
                Mean[ir] /= areaNorm * numPhotons;
                if (_tallySecondMoment)
                {
                    SecondMoment[ir] /= areaNorm * areaNorm * numPhotons;
                }
            }
        }

        public bool ContainsPoint(PhotonDataPoint dp)
        {
            return (dp.StateFlag == PhotonStateType.ExitedOutTop);
        }

    }
}