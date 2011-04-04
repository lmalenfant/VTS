﻿using System;
using Vts.Common;
using Vts.MonteCarlo.PhotonData;
using Vts.MonteCarlo.Helpers;

namespace Vts.MonteCarlo.Sources
{
    /// <summary>
    /// 
    /// </summary>
    public class EllipticalSourceCollimatedFlat: ISource
    {        
        private Position _translationFromOrigin;
        private PolarAzimuthalAngles _rotationFromInwardNormal;
        private ThreeAxisRotation _rotationOfPrincipalSourceAxis;
        private double _aParameter = 1.0;
        private double _bParameter = 1.0;
        private SourceFlags _rotationAndTranslationFlags;

        /// <summary>
        /// Returns an instance of Collimated Flat Elliptical Source with a specified translation, inward normal rotation, and source axis rotation
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="translationFromOrigin"></param>
        /// <param name="rotationFromInwardNormal"></param>
        /// <param name="rotationOfPrincipalSourceAxis"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,            
            Position translationFromOrigin,
            PolarAzimuthalAngles rotationFromInwardNormal,
            ThreeAxisRotation rotationOfPrincipalSourceAxis)
        {           
            _translationFromOrigin = translationFromOrigin.Clone();
            _rotationFromInwardNormal = rotationFromInwardNormal.Clone();
            _rotationOfPrincipalSourceAxis = rotationOfPrincipalSourceAxis.Clone();
            _rotationAndTranslationFlags = new SourceFlags(true, true, true);
        }

        /// <summary>
        /// Returns an instance of Collimated Flat Elliptical Source with a specified translation and inward normal rotation, but without source axis rotation
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="translationFromOrigin"></param>
        /// <param name="rotationFromInwardnormal"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,           
            Position translationFromOrigin,
            PolarAzimuthalAngles rotationFromInwardnormal)
            : this(
                aParameter,  
                bParameter,                
                translationFromOrigin,
                rotationFromInwardnormal,
                new ThreeAxisRotation(0, 0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(true, true, false);
        }

        /// <summary>
        /// Returns an instance of Collimated Flat Elliptical Source with a specified translation and source axis rotation, but without inward normal rotation 
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="translationFromOrigin"></param>
        /// <param name="rotationOfPrincipalSourceAxis"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,            
            Position translationFromOrigin,
            ThreeAxisRotation rotationOfPrincipalSourceAxis
            )
            : this(
                aParameter,  
                bParameter,                
                translationFromOrigin,
                new PolarAzimuthalAngles(0, 0),
                rotationOfPrincipalSourceAxis)
        {
            _rotationAndTranslationFlags = new SourceFlags(true, false, true);
        }

        /// <summary>
        /// Returns an instance of Collimated flat Elliptical Source with a specified translation but without inward normal rotation or source axis rotation 
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="translationFromOrigin"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,            
            Position translationFromOrigin)
            : this(
                aParameter,  
                bParameter,                
                translationFromOrigin,
                new PolarAzimuthalAngles(0, 0),
                new ThreeAxisRotation(0, 0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(true, false, false);
        }

        /// <summary>
        /// Returns an instance of Collimated flat Elliptical Source with an inward normal rotation and source axis rotation
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="rotationFromInwardnormal"></param>
        /// <param name="rotationOfPrincipalSourceAxis"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,
            PolarAzimuthalAngles rotationFromInwardnormal,
            ThreeAxisRotation rotationOfPrincipalSourceAxis)
            : this(
                aParameter,  
                bParameter,                
                new Position(0, 0, 0),
                rotationFromInwardnormal,
                rotationOfPrincipalSourceAxis)
        {
            _rotationAndTranslationFlags = new SourceFlags(false, true, false);
        }


        /// <summary>
        /// Returns an instance of Collimated flat Elliptical Source with an inward normal rotation, but without source axis rotation
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="rotationFromInwardnormal"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,
            PolarAzimuthalAngles rotationFromInwardnormal)
            : this(
                aParameter,  
                bParameter,
                new Position(0, 0, 0),
                rotationFromInwardnormal,
                new ThreeAxisRotation(0, 0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(false, true, false);
        }

        /// <summary>
        /// Returns an instance of Collimated flat Elliptical Source with a source axis rotation, but without inward normal rotation
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        /// <param name="rotationOfPrincipalSourceAxis"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,  
            double bParameter,
            ThreeAxisRotation rotationOfPrincipalSourceAxis)
            : this(
                aParameter,  
                bParameter,
                new Position(0, 0, 0),
                new PolarAzimuthalAngles(0, 0),
                rotationOfPrincipalSourceAxis)
        {
            _rotationAndTranslationFlags = new SourceFlags(false, false, true);
        }

        /// <summary>
        /// Returns an instance of Collimated flat Elliptical Source with no inward normal rotation or source axis rotation 
        /// </summary>
        /// <param name="aParameter"></param>
        /// <param name="bParameter"></param>
        public EllipticalSourceCollimatedFlat(
            double aParameter,
            double bParameter)
            : this(
                aParameter,  
                bParameter,
                new Position(0, 0, 0),
                new PolarAzimuthalAngles(0, 0),
                new ThreeAxisRotation(0, 0, 0))
        {
            _rotationAndTranslationFlags = new SourceFlags(false, false, false);
        }


        public Photon GetNextPhoton(ITissue tissue)
        {
            Position finalPosition;

            if (_aParameter == _bParameter)
                //Source starts from anywhere in the circle
                finalPosition = SourceToolbox.GetRandomFlatCircularPosition(new Position(0, 0, 0), 
                    _aParameter, 
                    Rng);
            else
                //Source starts from anywhere in the ellipse
                finalPosition = SourceToolbox.GetRandomFlatEllipsePosition(new Position(0, 0, 0),                 
                    _aParameter, 
                    _bParameter, 
                    Rng);

            // sample angular distribution
            Direction finalDirection = new Direction(0, 0, 1);

            //Rotation and translation
            SourceToolbox.DoRotationandTranslationForGivenFlags(
                ref finalPosition,
                ref finalDirection,
                _translationFromOrigin,
                _rotationFromInwardNormal,
                _rotationOfPrincipalSourceAxis,
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
