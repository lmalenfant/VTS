﻿using System;
using Vts.Common;
using Vts.MonteCarlo.PhotonData;
using Vts.MonteCarlo.Helpers;

namespace Vts.MonteCarlo.Sources
{
    /// <summary>
    /// 
    /// </summary>
    public class PointSourceIsotropic : ISource
    {
        private Position _translationFromOrigin;
        private PolarAzimuthalAngles _rotationFromInwardNormal;
        private SourceFlags _rotationAndTranslationFlags;

        /// <summary>
        /// Returns an instance of MultiDirectional PointSource
        /// </summary>
        /// <param name="translationFromOrigin"></param>
        /// <param name="rotationFromInwardNormal"></param>
        public PointSourceIsotropic(
            Position translationFromOrigin,
            PolarAzimuthalAngles rotationFromInwardNormal)
        {
            _translationFromOrigin = translationFromOrigin.Clone();
            _rotationFromInwardNormal = rotationFromInwardNormal.Clone();
            _rotationAndTranslationFlags = new SourceFlags(true, true, false); 
        }

        /// <summary>
        /// Returns an instance of Multidirectional/Isotropic PointSource with a specified translation, no rotation, pointing normally inward
        /// </summary>
        /// <param name="translationFromOrigin"></param>
        public PointSourceIsotropic(
            Position translationFromOrigin)
            : this(
                translationFromOrigin,
                new PolarAzimuthalAngles(0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(true, false, false); 
        }
                       
        /// <summary>
        /// Returns an instance of Multidirectional/Isotropic PointSource with no translation, but rotation from inward normal
        /// </summary>
        /// <param name="rotationFromInwardNormal"></param>
        public PointSourceIsotropic(
            PolarAzimuthalAngles rotationFromInwardNormal)
            : this(
                new Position(0, 0, 0),
                rotationFromInwardNormal)
        {
            _rotationAndTranslationFlags = new SourceFlags(false, true, false);
        }

        /// <summary>
        /// Returns an instance of Multidirectional/ Isotropic PointSource with no translation, no rotation, pointing normally inward
        /// </summary>
        public PointSourceIsotropic()
            : this(
                new Position(0, 0, 0),
                new PolarAzimuthalAngles(0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(false, false, false);
        }

        public Photon GetNextPhoton(ITissue tissue)
        {
            //Source starts at the origin
            Position finalPosition = new Position(0, 0, 0);

            //Source oriented along z-axis
            Direction finalDirection = SourceToolbox.GetRandomDirectionForIsotropicDistribution(Rng);

            //Rotation and translation
            SourceToolbox.DoRotationandTranslationForGivenFlags(
                ref finalPosition,
                ref finalDirection,               
                _translationFromOrigin,
                _rotationFromInwardNormal,
                _rotationAndTranslationFlags);


            // the handling of specular needs work
            var weight = 1.0 - Helpers.Optics.Specular(tissue.Regions[0].RegionOP.N, tissue.Regions[1].RegionOP.N);

            var dataPoint = new PhotonDataPoint(
                finalPosition,
                finalDirection,
                weight,
                0.0,
                PhotonStateType.NotSet);

            var photon = new Photon { DP = dataPoint };

            return photon;
        }

        #region Random number generator code (copy-paste into all sources)
        /// <summary>
        /// The random number generator used to create photons. If not assigned externally,
        /// a Mersenne Twister (MathNet.Numerics.Random.MersenneTwister) will be created with
        /// a seed of zero.
        /// </summary>
        public Random Rng
        {
            get
            {
                if (_rng == null)
                {
                    _rng = new MathNet.Numerics.Random.MersenneTwister(0);
                }
                return _rng;
            }
            set { _rng = value; }
        }
        private Random _rng;
        #endregion

    }
}
