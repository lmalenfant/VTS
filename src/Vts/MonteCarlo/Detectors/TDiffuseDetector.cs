using System.Collections.Generic;
using System.Runtime.Serialization;
using Vts.MonteCarlo.PhotonData;

namespace Vts.MonteCarlo.Detectors
{
    [KnownType(typeof(TDiffuseDetector))]
    /// <summary>
    /// Implements ITerminationTally<double>.  Tally for diffuse transmittance.
    /// This implemenation works for Analog, DAW and CAW processing.
    /// </summary>
    public class TDiffuseDetector : ITerminationDetector<double>
    {
        /// <summary>
        /// Returns an instance of TDiffuseDetector
        /// </summary>
        public TDiffuseDetector()
        {
            Mean = 0.0;
            SecondMoment = 0.0;
            TallyType = TallyType.TDiffuse;
        }

        public double Mean { get; set; }

        public double SecondMoment { get; set; }

        public TallyType TallyType { get; set; }

        public long TallyCount { get; set; }

        public void Tally(PhotonDataPoint dp)
        {
            Mean += dp.Weight;
            SecondMoment += dp.Weight * dp.Weight;
            TallyCount++;
        }

        public void Normalize(long numPhotons)
        {
            Mean /= numPhotons;
        }

        public bool ContainsPoint(PhotonDataPoint dp)
        {
            return (dp.StateFlag == PhotonStateType.ExitedOutBottom);
        }
    }
}
