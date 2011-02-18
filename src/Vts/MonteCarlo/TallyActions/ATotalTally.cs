using System;
using System.Collections.Generic;
using Vts.Common;
using Vts.MonteCarlo.PhotonData;
using Vts.MonteCarlo.Helpers;

namespace Vts.MonteCarlo.TallyActions
{
    /// <summary>
    /// Implements IHistoryTally<double[,]>.  Tally for Absorption(rho,z).
    /// </summary>
    public class ATotalTally : HistoryTallyBase, IHistoryTally<double>
    {
        private Func<double, double, double, double, PhotonStateType, double> _absorbAction;

        public ATotalTally(ITissue tissue)
           : base(tissue)
        {
        }

        public double Mean { get; set; }
        public double SecondMoment { get; set; }

        protected override void SetAbsorbAction(AbsorptionWeightingType awt)
        {
            switch (awt)
            {
                case AbsorptionWeightingType.Analog:
                    _absorbAction = AbsorbAnalog;
                    break;
                //case AbsorptionWeightingType.Continuous:
                //    AbsorbAction = AbsorbContinuous;
                //    break;
                case AbsorptionWeightingType.Discrete:
                default:
                    _absorbAction = AbsorbDiscrete;
                    break;
            }
        }
 
        public void Tally(PhotonDataPoint previousDP, PhotonDataPoint dp)
        {
            var weight = _absorbAction(
                _ops[_tissue.GetRegionIndex(dp.Position)].Mua, 
                _ops[_tissue.GetRegionIndex(dp.Position)].Mus,
                previousDP.Weight,
                dp.Weight,
                dp.StateFlag);
            Mean += weight; 
            SecondMoment += weight * weight;
        }
        private double AbsorbAnalog(double mua, double mus, double previousWeight, double weight, PhotonStateType photonStateType)
        {
            if (photonStateType != PhotonStateType.Absorbed)
            {
                weight = 0.0;
            }
            else
            {
                weight = previousWeight * mua / (mua + mus);
            }
            return weight;
        }
        private double AbsorbDiscrete(double mua, double mus, double previousWeight, double weight, PhotonStateType photonStateType)
        {
            if (previousWeight == weight) // pseudo collision, so no tally
            {
                weight = 0.0;
            }
            else
            {
                weight = previousWeight * mua / (mua + mus);
            }
            return weight;
        }

        public void Normalize(long numPhotons)
        {
            Mean /=  numPhotons;

        }
        public bool ContainsPoint(PhotonDataPoint dp)
        {
            return true;
        }

    }
}