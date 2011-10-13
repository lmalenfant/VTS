using System;
using System.Collections.Generic;
using Vts.Common;

namespace Vts.MonteCarlo
{
    /// <summary>
    /// DetectorInput for R(fx)
    /// </summary>
    public class ROfFxDetectorInput : IDetectorInput
    {
        /// <summary>
        /// Returns an instance of ROfFxDetectorInput
        /// </summary>
        /// <param name="fx"></param>
        /// <param name="name"></param>
        public ROfFxDetectorInput(DoubleRange fx, String name)
        {
            TallyType = TallyType.ROfFx;
            Name = name;
            Fx = fx;
        }
        /// <summary>
        /// constructor uses TallyType as name
        /// </summary>
        /// <param name="fx"></param>
        public ROfFxDetectorInput(DoubleRange fx)
            : this(fx, TallyType.ROfFx.ToString()) { }

        /// <summary>
        /// Default constructor uses default rho bins
        /// </summary>
        public ROfFxDetectorInput()
            : this(new DoubleRange(0.0, 0.2, 21), TallyType.ROfFx.ToString()) { }

        public TallyType TallyType { get; set; }
        public String Name { get; set; }
        public DoubleRange Fx { get; set; }
    }
}
