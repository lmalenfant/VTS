using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Vts.Common;
using Vts.MonteCarlo.PhotonData;
using Vts.MonteCarlo.Helpers;
using Vts.MonteCarlo.Tissues;

namespace Vts.MonteCarlo.Detectors
{
    [KnownType(typeof(RadianceOfRhoAndZAndAngleDetector))]
    /// <summary>
    /// Implements IVolumeDetector<double[,,]>.  Tally for Radiance(rho,z,angle).
    /// Note: this tally currently only works with discrete absorption weighting and analog
    /// </summary>
    public class RadianceOfRhoAndZAndAngleDetector : IVolumeDetector<double[, ,]>
    {
        private Func<double, double, double, double, PhotonStateType, double> _absorbAction;

        private ITissue _tissue;
        private bool _tallySecondMoment;
        private IList<OpticalProperties> _ops;
        ///<summary>
        /// Returns an instance of RadianceOfRhoAndZAndAngleDetector
        ///</summary>
        ///<param name="rho"></param>
        ///<param name="z"></param>
        ///<param name="angle"></param>
        ///<param name="tissue"></param>
        public RadianceOfRhoAndZAndAngleDetector(
            DoubleRange rho, 
            DoubleRange z, 
            DoubleRange angle, // this is binned with respect to theta 
            ITissue tissue,
            bool tallySecondMoment,
            String name
            )
        {
            Rho = rho;
            Z = z;
            Angle = angle;
            Mean = new double[Rho.Count - 1, Z.Count - 1, Angle.Count - 1];
            _tallySecondMoment = tallySecondMoment;
            SecondMoment = null;
            if (_tallySecondMoment)
            {
                SecondMoment = new double[Rho.Count - 1, Z.Count - 1, Angle.Count - 1];
            }
            TallyType = TallyType.FluenceOfRhoAndZAndTime;
            Name = name;
            TallyCount = 0;
            _tissue = tissue;
            SetAbsorbAction(_tissue.AbsorptionWeightingType);
            _ops = tissue.Regions.Select(r => r.RegionOP).ToArray();
        }

        /// <summary>
        /// Returns an instance of RadianceOfRhoAndZAndAngleDetector (for serialization purposes only)
        /// </summary>
        public RadianceOfRhoAndZAndAngleDetector()
            : this(
            new DoubleRange(), 
            new DoubleRange(), 
            new DoubleRange(), 
            new MultiLayerTissue(), 
            true,
            TallyType.RadianceOfRhoAndZAndAngle.ToString())
        {
        }

        [IgnoreDataMember]
        public double[, ,] Mean { get; set; }

        [IgnoreDataMember]
        public double[, ,] SecondMoment { get; set; }

        public TallyType TallyType { get; set; }

        public String Name { get; set; }

        public long TallyCount { get; set; }

        public DoubleRange Rho { get; set; }

        public DoubleRange Z { get; set; }

        public DoubleRange Angle { get; set; }

        private void SetAbsorbAction(AbsorptionWeightingType awt)
        {
            switch (awt)
            {
                case AbsorptionWeightingType.Analog:
                    _absorbAction = AbsorbAnalog;
                    break;
                case AbsorptionWeightingType.Continuous:
                    _absorbAction = AbsorbContinuous;
                    break;
                case AbsorptionWeightingType.Discrete:
                    _absorbAction = AbsorbDiscrete;
                    break;
                default:
                    throw new ArgumentException("AbsorptionWeightingType not set");
            }
        }

        public void Tally(PhotonDataPoint previousDP, PhotonDataPoint dp)
        {
            var ir = DetectorBinning.WhichBin(DetectorBinning.GetRho(dp.Position.X, dp.Position.Y), Rho.Count - 1, Rho.Delta, Rho.Start);
            var iz = DetectorBinning.WhichBin(dp.Position.Z, Z.Count - 1, Z.Delta, Z.Start);
            var ia = DetectorBinning.WhichBin(Math.Acos(dp.Direction.Uz), Angle.Count - 1, Angle.Delta, Angle.Start);

            var weight = _absorbAction(
                _ops[_tissue.GetRegionIndex(dp.Position)].Mua,
                _ops[_tissue.GetRegionIndex(dp.Position)].Mus,
                previousDP.Weight,
                dp.Weight,
                dp.StateFlag);

            var regionIndex = _tissue.GetRegionIndex(dp.Position);

            Mean[ir, iz, ia] += weight / _ops[regionIndex].Mua;
            if (_tallySecondMoment)
            {
                SecondMoment[ir, iz, ia] += (weight / _ops[regionIndex].Mua) * (weight / _ops[regionIndex].Mua);
            }
            TallyCount++;
        }

        private double AbsorbAnalog(double mua, double mus, double previousWeight, double weight, PhotonStateType photonStateType)
        {
            if (photonStateType.Has(PhotonStateType.Absorbed))
            {
                weight = previousWeight * mua / (mua + mus); 
            }
            else
            {
                weight = 0.0;
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

        private double AbsorbContinuous(double mua, double mus, double previousWeight, double weight, PhotonStateType photonStateType)
        {
            throw new NotImplementedException();
        }

        public void Normalize(long numPhotons)
        {
            var normalizationFactor = 2.0 * Math.PI * Rho.Delta * Rho.Delta * Z.Delta * 2.0 * Math.PI * Angle.Delta;
            for (int ir = 0; ir < Rho.Count - 1; ir++)
            {
                for (int iz = 0; iz < Z.Count - 1; iz++)
                {
                    for (int ia = 0; ia < Angle.Count - 1; ia++)
                    {
                        var areaNorm = (ir + 0.5) * Math.Sin((ia + 0.5) * Angle.Delta) * normalizationFactor;
                        Mean[ir, iz, ia] /= areaNorm * numPhotons;
                        if (_tallySecondMoment)
                        {
                            SecondMoment[ir, iz, ia] /= areaNorm * areaNorm * numPhotons;
                        }
                    }
                }
            }

        }

        public bool ContainsPoint(PhotonDataPoint dp)
        {
            return true;
        }

    }
}