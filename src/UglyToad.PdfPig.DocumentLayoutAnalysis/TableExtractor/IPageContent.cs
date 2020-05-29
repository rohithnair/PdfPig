﻿namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableExtractor
{
    using System;
    using Core;

    /// <summary>
    /// A single page content
    /// </summary>
    public interface IPageContent
    {
        /// <summary>
        /// Adds the text at the specified position
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="content">The content.</param>
        void AddText(PdfPoint point, float tolerance, string content);

        /// <summary>
        /// Determines whether the item content contains the point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        ///   <c>true</c> if the item contains the specified point; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(PdfPoint point, float tolerance);

        /// <summary>
        /// Determines whether this item contains the y coordinate.
        /// </summary>
        /// <param name="y">The y coordinate.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        ///   <c>true</c> if this item contains the specified y coordinate; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(double y, float tolerance);

        /// <summary>
        /// Gets the y coordinate of this item.
        /// </summary>
        /// <value>
        /// The y coordinate.
        /// </value>
        double Y { get; }
    }
}