﻿namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains helpful tools for distance measures.
    /// </summary>
    public static class Distances
    {
        /// <summary>
        /// The Euclidean distance is the "ordinary" straight-line distance between two points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double Euclidean(PdfPoint point1, PdfPoint point2)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// The weighted Euclidean distance.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <param name="wX">The weight of the X coordinates. Default is 1.</param>
        /// <param name="wY">The weight of the Y coordinates. Default is 1.</param>
        public static double WeightedEuclidean(PdfPoint point1, PdfPoint point2, double wX = 1.0, double wY = 1.0)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            return Math.Sqrt(wX * dx * dx + wY * dy * dy);
        }

        /// <summary>
        /// The Manhattan distance between two points is the sum of the absolute differences of their Cartesian coordinates.
        /// <para>Also known as rectilinear distance, L1 distance, L1 norm, snake distance, city block distance, taxicab metric.</para>
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double Manhattan(PdfPoint point1, PdfPoint point2)
        {
            return Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y);
        }

        /// <summary>
        /// The angle in degrees between the horizontal axis and the line between two points.
        /// <para>-180 ≤ θ ≤ 180</para>
        /// </summary>
        /// <param name="startPoint">The first point.</param>
        /// <param name="endPoint">The second point.</param>
        public static double Angle(PdfPoint startPoint, PdfPoint endPoint)
        {
            return Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X) * 180 / Math.PI;
        }

        /// <summary>
        /// The absolute distance between the Y coordinates of two points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double Vertical(PdfPoint point1, PdfPoint point2)
        {
            return Math.Abs(point2.Y - point1.Y);
        }

        /// <summary>
        /// The absolute distance between the X coordinates of two points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double Horizontal(PdfPoint point1, PdfPoint point2)
        {
            return Math.Abs(point2.X - point1.X);
        }

        /// <summary>
        /// Bound angle so that -180 ≤ θ ≤ 180.
        /// </summary>
        /// <param name="angle">The angle to bound.</param>
        public static double BoundAngle180(double angle)
        {
            angle = (angle + 180) % 360;
            if (angle < 0) angle += 360;
            return angle - 180;
        }

        /// <summary>
        /// Bound angle so that 0 ≤ θ ≤ 360.
        /// </summary>
        /// <param name="angle">The angle to bound.</param>
        public static double BoundAngle0to360(double angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;
            return angle;
        }

        /// <summary>
        /// Get the minimum edit distance between two strings.
        /// </summary>
        /// <param name="string1">The first string.</param>
        /// <param name="string2">The second string.</param>
        public static int MinimumEditDistance(string string1, string string2)
        {
            ushort[,] d = new ushort[string1.Length + 1, string2.Length + 1];

            for (int i = 1; i <= string1.Length; i++)
            {
                d[i, 0] = (ushort)i;
            }

            for (int j = 1; j <= string2.Length; j++)
            {
                d[0, j] = (ushort)j;
            }

            for (int j = 1; j <= string2.Length; j++)
            {
                for (int i = 1; i <= string1.Length; i++)
                {
                    d[i, j] = Math.Min(Math.Min(
                        (ushort)(d[i - 1, j] + 1),
                        (ushort)(d[i, j - 1] + 1)),
                        (ushort)(d[i - 1, j - 1] + (string1[i - 1] == string2[j - 1] ? 0 : 1))); // substitution, set cost to 1
                }
            }
            return d[string1.Length, string2.Length];
        }

        /// <summary>
        /// Get the minimum edit distance between two strings.
        /// <para>Returned values are between 0 and 1 included. A value of 0 means that the two strings are indentical.</para>
        /// </summary>
        /// <param name="string1">The first string.</param>
        /// <param name="string2">The second string.</param>
        public static double MinimumEditDistanceNormalised(string string1, string string2)
        {
            return MinimumEditDistance(string1, string2) / (double)Math.Max(string1.Length, string2.Length);
        }

        /// <summary>
        /// Find the index of the nearest point, excluding itself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element">The reference point, for which to find the nearest neighbour.</param>
        /// <param name="candidates">The list of neighbours candidates.</param>
        /// <param name="pivotPoint"></param>
        /// <param name="candidatePoint"></param>
        /// <param name="distanceMeasure">The distance measure to use.</param>
        /// <param name="distance">The distance between the reference element and its nearest neighbour.</param>
        public static int FindIndexNearest<T>(T element, IReadOnlyList<T> candidates,
            Func<T, PdfPoint> pivotPoint, Func<T, PdfPoint> candidatePoint,
            Func<PdfPoint, PdfPoint, double> distanceMeasure, out double distance)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The list of neighbours candidates is either null or empty.", nameof(candidates));
            }

            if (distanceMeasure == null)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The distance measure must not be null.", nameof(distanceMeasure));
            }

            distance = double.MaxValue;
            int closestPointIndex = -1;
            var candidatesPoints = candidates.Select(candidatePoint).ToList();
            var pivot = pivotPoint(element);

            for (var i = 0; i < candidates.Count; i++)
            {
                double currentDistance = distanceMeasure(pivot, candidatesPoints[i]);
                if (currentDistance < distance && !candidates[i].Equals(element))
                {
                    distance = currentDistance;
                    closestPointIndex = i;
                }
            }

            return closestPointIndex;
        }

        /// <summary>
        /// Find the index of the nearest line, excluding itself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element">The reference line, for which to find the nearest neighbour.</param>
        /// <param name="candidates">The list of neighbours candidates.</param>
        /// <param name="pivotLine"></param>
        /// <param name="candidateLine"></param>
        /// <param name="distanceMeasure">The distance measure between two lines to use.</param>
        /// <param name="distance">The distance between the reference element and its nearest neighbour.</param>
        public static int FindIndexNearest<T>(T element, IReadOnlyList<T> candidates,
            Func<T, PdfLine> pivotLine, Func<T, PdfLine> candidateLine,
            Func<PdfLine, PdfLine, double> distanceMeasure, out double distance)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The list of neighbours candidates is either null or empty.", nameof(candidates));
            }

            if (distanceMeasure == null)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The distance measure must not be null.", nameof(distanceMeasure));
            }

            distance = double.MaxValue;
            int closestLineIndex = -1;
            var candidatesLines = candidates.Select(candidateLine).ToList();
            var pivot = pivotLine(element);

            for (var i = 0; i < candidates.Count; i++)
            {
                double currentDistance = distanceMeasure(pivot, candidatesLines[i]);
                if (currentDistance < distance && !candidates[i].Equals(element))
                {
                    distance = currentDistance;
                    closestLineIndex = i;
                }
            }

            return closestLineIndex;
        }
    }
}
