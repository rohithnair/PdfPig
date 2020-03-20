﻿namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Core.Graphics.Colors;

    /// <summary>
    /// A grayscale color with a single gray component.
    /// </summary>
    public class GrayColor : IColor, IEquatable<GrayColor>
    {
        /// <summary>
        /// Gray Black value (0).
        /// </summary>
        public static GrayColor Black { get; } = new GrayColor(0);

        /// <summary>
        /// Gray White value (1).
        /// </summary>
        public static GrayColor White { get; } = new GrayColor(1);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceGray;

        /// <summary>
        /// The gray value between 0 and 1.
        /// </summary>
        public decimal Gray { get; }

        /// <summary>
        /// Create a new <see cref="GrayColor"/>.
        /// </summary>
        public GrayColor(decimal gray)
        {
            Gray = gray;
        }

        /// <inheritdoc/>
        public (decimal r, decimal g, decimal b) ToRGBValues()
        {
            return (Gray, Gray, Gray);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as GrayColor);
        }

        /// <inheritdoc />
        public bool Equals(GrayColor other)
        {
            return other != null &&
                   Gray == other.Gray;
        }

        /// <inheritdoc />
        public override int GetHashCode() => Gray.GetHashCode();
        
        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(GrayColor color1, GrayColor color2) => EqualityComparer<GrayColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(GrayColor color1, GrayColor color2) => !(color1 == color2);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Gray: {Gray}";
        }
    }
}