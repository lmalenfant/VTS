﻿using System;
using System.IO;
using Vts.MonteCarlo.PhotonData;

namespace Vts.MonteCarlo.Factories
{
    /// <summary>
    /// Factory methods for PhotonDatabase class
    /// </summary>
    public class PhotonDatabaseFactory
    {
        /// <summary>
        /// Method to read photon database from file
        /// </summary>
        /// <param name="virtualBoundaryType">VB type</param>
        /// <param name="filePath">path to database file</param>
        /// <returns>PhotonDatabase read</returns>
        public static PhotonDatabase GetPhotonDatabase(
            VirtualBoundaryType virtualBoundaryType, string filePath)
        {
            string dbFilename;
            switch (virtualBoundaryType)
            {
                case VirtualBoundaryType.DiffuseReflectance:
                    dbFilename = Path.Combine(filePath, "DiffuseReflectanceDatabase");
                    break;
                case VirtualBoundaryType.DiffuseTransmittance:
                    dbFilename = Path.Combine(filePath, "DiffuseTransmittanceDatabase");
                    break;
                case VirtualBoundaryType.SpecularReflectance:
                    dbFilename = Path.Combine(filePath, "SpecularReflectanceDatabase");
                    break;
                case VirtualBoundaryType.pMCDiffuseReflectance: //pMC uses same exit db as regular post-processing
                    dbFilename = Path.Combine(filePath, "DiffuseReflectanceDatabase");
                    break;
                case VirtualBoundaryType.pMCDiffuseTransmittance: //pMC uses same exit db as regular post-processing
                    dbFilename = Path.Combine(filePath, "DiffuseTransmittanceDatabase");
                    break;
                case VirtualBoundaryType.GenericVolumeBoundary:
                case VirtualBoundaryType.Dosimetry:
                case VirtualBoundaryType.BoundingCylinderVolume:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(
                        "Virtual boundary type not recognized: " + virtualBoundaryType);
            }
            if (!File.Exists(dbFilename))
            {
                throw new FileNotFoundException("\nThe database file could not be found: " + dbFilename);
            }
            return PhotonDatabase.FromFile(dbFilename);
        }
        /// <summary>
        /// Method to read perturbation Monte Carlo (pMC) photon database from file
        /// </summary>
        /// <param name="virtualBoundaryType">VB type</param>
        /// <param name="filePath">path to database file</param>
        /// <returns>PhotonDatabase read</returns>
        public static pMCDatabase GetpMCDatabase(
            VirtualBoundaryType virtualBoundaryType, string filePath)
        {
            switch (virtualBoundaryType)
            {
                case VirtualBoundaryType.pMCDiffuseReflectance:
                    return pMCDatabase.FromFile(Path.Combine(filePath, "DiffuseReflectanceDatabase"),
                        Path.Combine(filePath, "CollisionInfoDatabase"));
                case VirtualBoundaryType.pMCDiffuseTransmittance:
                    return pMCDatabase.FromFile(Path.Combine(filePath, "DiffuseTransmittanceDatabase"),
                        Path.Combine(filePath, "CollisionInfoTransmittanceDatabase"));
                case VirtualBoundaryType.DiffuseReflectance:
                case VirtualBoundaryType.DiffuseTransmittance:
                case VirtualBoundaryType.SpecularReflectance:
                case VirtualBoundaryType.GenericVolumeBoundary:
                case VirtualBoundaryType.Dosimetry:
                case VirtualBoundaryType.BoundingCylinderVolume:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(
                        "Virtual boundary type not recognized: " + virtualBoundaryType);

            }
        }

    }
} 
