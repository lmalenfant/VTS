﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vts.MonteCarlo.Interfaces;

namespace Vts.MonteCarlo.Sources.SourceProfiles
{
    /// <summary>
    /// The <see cref="SourceProfiles"/> namespace contains the Monte Carlo input source profiles
    /// </summary>

    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Defines Arbitrary Source Profile
    /// </summary>
    public class ArbitrarySourceProfile : ISourceProfile
    {
        /// <summary>
        /// Initializes a new instance of the ArbitrarySourceProfile class
        /// </summary>
        /// <param name="image">1D array containing 2D image of values</param>
        /// <param name="pixelWidth">pixel width</param>
        /// <param name="pixelHeight">pixel height</param>
        /// <remarks>Currently, only binary maps are implemented. Non-zero (1e-10D) values will be 1, 0 otherwise</remarks>
        public ArbitrarySourceProfile(double[] image, int pixelWidth, int pixelHeight)
        {
            Image = image;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            var binaryPixelMap = new List<int>(image.Length);
            for (var i = 0; i < image.Length; i++)
            {
                var pixelValue = image[i];
                if (Math.Abs(pixelValue) > 1e-10D)
                {
                    binaryPixelMap.Add(i);
                }
            }
            binaryPixelMap.ToArray();
        }

        /// <summary>
        /// Initializes the default constructor of ArbitrarySourceProfile class (intensity = 1.0)
        /// </summary>
        public ArbitrarySourceProfile()
            : this(new double[] { 255 }, 1, 1)
        {
        }

        /// <summary>
        /// Returns Arbitrary profile type
        /// </summary>
        //[IgnoreDataMember]
        public SourceProfileType SourceProfileType => SourceProfileType.Arbitrary;

        /// <summary>
        /// 1D array containing 2D image of values
        /// </summary>
        public double[] Image { get; set; }
        /// <summary>
        /// pixel width
        /// </summary>
        public int PixelWidth { get; set; }
        /// <summary>
        /// pixel height
        /// </summary>
        public int PixelHeight { get; set; }
        /// <summary>
        /// flag to determine if binary, currently only binary is implemented
        /// </summary>
        public bool IsBinary { get; set; }
    }
}
