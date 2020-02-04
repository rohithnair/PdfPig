﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using static UglyToad.PdfPig.Core.PdfPath;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// 
    /// </summary>
    public static class TableExtractor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IEnumerable<List<PdfRectangle>> GetCandidates(Page page)
        {
            var processedLines = GetProcessLines(page);
            var intersectionPoints = GetIntersections(processedLines);
            var foundRectangles = GetRectangularAreas(intersectionPoints);
            return GroupRectanglesInTable(foundRectangles).ToList();
        }

        private static IReadOnlyList<PdfLine> GetProcessLines(Page page)
        {
            var modeWidth = page.Letters.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.GlyphRectangle.Width).Mode();
            var modeHeight = page.Letters.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.GlyphRectangle.Height).Mode();

            // See 'Configurable Table Structure Recognition in Untagged PDF Documents' by Alexey Shigarov
            // 2.1 Preprocessing
            // 1. We also split each rectangle into four rulings corresponding to its boundaries
            var processedLinesSet = new HashSet<PdfLine>();
            foreach (var pdfPath in page.ExperimentalAccess.Paths)
            {
                if (pdfPath.IsClipping) continue;

                // handle filled rectale to check if they are in fact lines
                if (pdfPath.IsDrawnAsRectangle) // also isFilled + force closure of filled
                {
                    var rect = pdfPath.GetBoundingRectangle();
                    if (!rect.HasValue) continue;

                    if (rect.Equals(page.CropBox.Bounds)) continue;

                    if (rect.Value.Width < modeWidth * 0.7)
                    {
                        if (rect.Value.Height < modeHeight * 0.7)
                        {
                            var centroid = rect.Value.Centroid;
                            processedLinesSet.Add(ExtendLine(new PdfLine(centroid.X, rect.Value.Bottom, centroid.X, rect.Value.Top), 2));
                            processedLinesSet.Add(ExtendLine(new PdfLine(rect.Value.Left, centroid.Y, rect.Value.Right, centroid.Y), 2));
                        }
                        else
                        {
                            var x = rect.Value.Centroid.X;
                            processedLinesSet.Add(ExtendLine(new PdfLine(x, rect.Value.Bottom, x, rect.Value.Top), 2));
                        }
                        continue;
                    }
                    else if (rect.Value.Height < modeHeight * 0.7)
                    {
                        if (rect.Value.Width < modeWidth * 0.7)
                        {
                            var centroid = rect.Value.Centroid;
                            processedLinesSet.Add(ExtendLine(new PdfLine(rect.Value.Left, centroid.Y, rect.Value.Right, centroid.Y), 2));
                            processedLinesSet.Add(ExtendLine(new PdfLine(centroid.X, rect.Value.Bottom, centroid.X, rect.Value.Top), 2));
                        }
                        else
                        {
                            var y = rect.Value.Centroid.Y;
                            processedLinesSet.Add(ExtendLine(new PdfLine(rect.Value.Left, y, rect.Value.Right, y), 2));
                        }
                        continue;
                    }
                }

                if (pdfPath.Commands.Any(c => c is BezierCurve)) continue; // filter out any path containing a bezier curve
                foreach (var command in pdfPath.Commands)
                {
                    if (command is Line line)
                    {
                        line = Normalise(line);
                        // vertical and horizontal lines only  
                        if (line.From.X != line.To.X && line.From.Y != line.To.Y) continue;

                        PdfLine pdfLine = ExtendLine(new PdfLine(line.From, line.To), 2);
                        if (!processedLinesSet.Contains(pdfLine)) processedLinesSet.Add(pdfLine);
                    }
                }
            }

            // 2. We merge all segments of one visual line into one ruling.
            var processedLines = processedLinesSet.ToList();
            int[] shouldMerge = Enumerable.Repeat(-1, processedLinesSet.Count).ToArray();
            for (var b = 0; b < processedLinesSet.Count; b++)
            {
                var current1 = processedLines[b];

                for (var c = 0; c < processedLinesSet.Count; c++)
                {
                    if (b == c) continue;
                    var current2 = processedLines[c];

                    if (ShouldMerge(current1, current2))
                    {
                        if (shouldMerge[c] != b)
                        {
                            shouldMerge[b] = c;
                            break;
                        }
                    }
                }
            }

            var merged = ClusteringAlgorithms.GroupIndexes(shouldMerge);

            var valid = new List<PdfLine>();
            for (int a = 0; a < merged.Count(); a++)
            {
                var group = merged[a].Select(i => processedLines[i]).ToList();
                if (group.Count == 1)
                {
                    valid.Add(group.First());
                    continue;
                }

                var first = group.First();
                if (first.Point1.X == first.Point2.X) // vertical lines
                {
                    var mergedLine = new PdfLine(first.Point1.X, Math.Min(group.Min(x => x.Point1.Y), group.Min(x => x.Point2.Y)),
                                          first.Point1.X, Math.Max(group.Max(x => x.Point1.Y), group.Max(x => x.Point2.Y)));
                    if (!group.All(l => l.Length <= mergedLine.Length))
                    {
                        throw new Exception();
                    }
                    valid.Add(mergedLine);
                }
                else // horizontal lines
                {
                    var mergedLine = new PdfLine(Math.Min(group.Min(x => x.Point1.X), group.Min(x => x.Point2.X)), first.Point1.Y,
                                          Math.Max(group.Max(x => x.Point1.X), group.Max(x => x.Point2.X)), first.Point1.Y);
                    if (!group.All(l => l.Length <= mergedLine.Length))
                    {
                        throw new Exception();
                    }
                    valid.Add(mergedLine);
                }
            }

            //var valid = processedLines.Where(l => l.HasValue).Select(l => l.Value).ToList();
            //.Where(l => l.Length > Math.Max(modeWidth * 2, modeHeight * 2)).ToList();
            return valid;
        }

        private static Line Normalise(Line line)
        {
            return line;
            //return new Line(new PdfPoint(Math.Round(line.From.X), Math.Round(line.From.Y)),
            //                new PdfPoint(Math.Round(line.To.X), Math.Round(line.To.Y)));
        }


        private static bool ShouldMerge(PdfLine line1, PdfLine line2)
        {
            // might add aditional checks of same color, stroke, etc...
            bool line1Vert = line1.Point1.X == line1.Point2.X; // if false, horizontal
            bool line2Vert = line2.Point1.X == line2.Point2.X; // if false, horizontal

            if ((line1Vert && !line2Vert) || (!line1Vert && line2Vert)) return false;

            if (line1Vert) // line 1 is vertical
            {
                if (line1.Point1.X != line2.Point1.X) return false; // both lines do not share same X coord
            }
            else // line 1 is horizontal
            {
                if (line1.Point1.Y != line2.Point1.Y) return false;  // both lines do not share same Y coord
            }

            if (line2.Contains(line1.Point1) ||
                line2.Contains(line1.Point2) ||
                line1.Contains(line2.Point1) ||
                line1.Contains(line2.Point2)) return true;
            return false;
        }


        private static PdfLine ExtendLine(PdfLine line, double pixel)
        {
            // vertical and horizontal lines only
            PdfPoint start;
            PdfPoint end;
            if (line.Point1.X == line.Point2.X)
            {
                start = (line.Point1.Y < line.Point2.Y) ? line.Point1 : line.Point2;
                end = (line.Point1.Y >= line.Point2.Y) ? line.Point1 : line.Point2;
                start = start.MoveY(-pixel);
                end = end.MoveY(pixel);
            }
            else if (line.Point1.Y == line.Point2.Y)
            {
                start = (line.Point1.X < line.Point2.X) ? line.Point1 : line.Point2;
                end = (line.Point1.X >= line.Point2.X) ? line.Point1 : line.Point2;
                start = start.MoveX(-pixel);
                end = end.MoveX(pixel);
            }
            else
            {
                throw new Exception();
            }

            return new PdfLine(start, end);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private static Dictionary<PdfPoint, (PdfLine Hor, PdfLine Vert)> GetIntersections(IReadOnlyList<PdfLine> lines)
        {
            var intersectionPoints = new Dictionary<PdfPoint, (PdfLine Hor, PdfLine Vert)>();
            for (var b = 0; b < lines.Count; b++)
            {
                var current1 = lines[b];

                for (var c = 0; c < lines.Count; c++)
                {
                    var current2 = lines[c];
                    if (b == c) continue;

                    var intersection = current1.Intersect(current2);
                    if (intersection.HasValue)
                    {
                        if (current1.Point1.X == current1.Point2.X)
                        {
                            intersectionPoints[intersection.Value] = (current2, current1);
                        }
                        else
                        {
                            intersectionPoints[intersection.Value] = (current1, current2);
                        }
                    }
                }
            }
            return intersectionPoints;
        }

        /// <summary>
        /// Identify closed rectangular spaces within the vertical and horizontal separator lines
        /// see pseudo code in 'ANSSI NURMINEN ALGORITHMIC EXTRACTION OF DATA IN TABLES IN PDF DOCUMENTS'
        /// 4.2.4 Finding rectangular areas
        /// </summary>
        private static IReadOnlyList<PdfRectangle> GetRectangularAreas(Dictionary<PdfPoint, (PdfLine Hor, PdfLine Vert)> intersectionPoints)
        {
            List<PdfRectangle> foundRectangles = new List<PdfRectangle>();

            // All crossing-points have been sorted from up to down, and left to right in ascending order
            var crossingPointsStack = new Stack<KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)>>(intersectionPoints
                .OrderByDescending(p => p.Key.X).OrderByDescending(p => p.Key.Y)); // stack inverses the order

            while (crossingPointsStack.Any())
            {
                var currentCrossingPoint = crossingPointsStack.Pop();

                // Fetch all points on the same vertical and horizontal line with current crossing point
                var x_points = CrossingPointsDirectlyBelow(currentCrossingPoint, crossingPointsStack);
                var y_points = CrossingPointsDirectlyToTheRight(currentCrossingPoint, crossingPointsStack);

                foreach (var x_point in x_points)
                {
                    var verticalCandidate = intersectionPoints[x_point.Key];

                    if (!EdgeExistsBetween(currentCrossingPoint.Value, verticalCandidate, false)) goto NextCrossingPoint;

                    foreach (var y_point in y_points)
                    {
                        var horizontalCandidate = intersectionPoints[y_point.Key];

                        if (!EdgeExistsBetween(currentCrossingPoint.Value, horizontalCandidate, true)) goto NextCrossingPoint;

                        // Hypothetical bottom right point of rectangle
                        var oppositeIntersection = new PdfPoint(y_point.Key.X, x_point.Key.Y);

                        if (!intersectionPoints.ContainsKey(oppositeIntersection)) continue;
                        var oppositeCandidate = intersectionPoints[oppositeIntersection];
                        if (EdgeExistsBetween(oppositeCandidate, verticalCandidate, true) &&
                            EdgeExistsBetween(oppositeCandidate, horizontalCandidate, false))
                        {
                            // Rectangle is confirmed to have 4 sides
                            foundRectangles.Add(new PdfRectangle(currentCrossingPoint.Key.X, currentCrossingPoint.Key.Y, oppositeIntersection.X, oppositeIntersection.Y));

                            // Each crossing point can be the top left corner of only a single rectangle
                            goto NextCrossingPoint;
                        }
                    }
                }
                NextCrossingPoint:;
            }
            return foundRectangles;
        }

        private static bool EdgeExistsBetween((PdfLine Hor, PdfLine Vert) candidate1, (PdfLine Hor, PdfLine Vert) candidate2, bool horizontal)
        {
            if (horizontal)
            {
                return candidate1.Hor.Equals(candidate2.Hor);
            }
            else
            {
                return candidate1.Vert.Equals(candidate2.Vert);
            }
        }

        private static IReadOnlyList<KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)>> CrossingPointsDirectlyBelow(KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)> currentCrossingPoint, Stack<KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)>> crossingPoints)
        {
            return crossingPoints.Where(p => p.Key.X == currentCrossingPoint.Key.X)
                                 .Where(p => p.Key.Y > currentCrossingPoint.Key.Y).ToList();
        }

        private static IReadOnlyList<KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)>> CrossingPointsDirectlyToTheRight(KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)> currentCrossingPoint, Stack<KeyValuePair<PdfPoint, (PdfLine Hor, PdfLine Vert)>> crossingPoints)
        {
            return crossingPoints.Where(p => p.Key.Y == currentCrossingPoint.Key.Y)
                                 .Where(p => p.Key.X > currentCrossingPoint.Key.X).ToList();
        }

        private static IEnumerable<List<PdfRectangle>> GroupRectanglesInTable(IReadOnlyList<PdfRectangle> rectangles)
        {
            if (rectangles == null || rectangles.Count == 0) yield break;

            var ordered = rectangles.OrderByDescending(x => x.Bottom).ThenByDescending(x => x.Left).ToList();

            int[][] indexGrouped = new int[rectangles.Count][];
            double threshold = 0.5;
            for (int i = 0; i < ordered.Count; i++)
            {
                // TODO: Or use a list? then convert to array
                List<int> group = new List<int>();
                //indexGrouped[i] = Enumerable.Repeat(-1, 10).ToArray(); // theor max neigh is 8
                //int f = 0;
                for (int j = 0; j < ordered.Count; j++)
                {
                    if (i == j) continue;
                    if (ShareCorner(ordered[i], ordered[j], threshold))
                    {
                        //indexGrouped[i][f++] = j;
                        group.Add(j);
                    }
                    //if (f > 9) break; // theor max neigh is 8
                }
                indexGrouped[i] = group.ToArray(); //indexGrouped[i].Where(x => x != -1).ToArray();
            }

            var groupedIndexes = ClusteringAlgorithms.GroupIndexes(indexGrouped);

            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                yield return groupedIndexes[a].Select(i => ordered[i]).ToList();
            }
        }

        private static bool ShareCorner(PdfRectangle pivot, PdfRectangle candidate, double distanceThreshold = 1.0)
        {
            return (Distances.Euclidean(pivot.BottomLeft, candidate.BottomRight) <= distanceThreshold ||
                    Distances.Euclidean(pivot.BottomLeft, candidate.TopRight) <= distanceThreshold ||
                    Distances.Euclidean(pivot.BottomLeft, candidate.TopLeft) <= distanceThreshold ||
                    
                    Distances.Euclidean(pivot.BottomRight, candidate.BottomLeft) <= distanceThreshold ||
                    Distances.Euclidean(pivot.BottomRight, candidate.TopRight) <= distanceThreshold ||
                    Distances.Euclidean(pivot.BottomRight, candidate.TopLeft) <= distanceThreshold ||
                    
                    Distances.Euclidean(pivot.TopLeft, candidate.BottomLeft) <= distanceThreshold ||
                    Distances.Euclidean(pivot.TopLeft, candidate.BottomRight) <= distanceThreshold ||
                    Distances.Euclidean(pivot.TopLeft, candidate.TopRight) <= distanceThreshold ||
                    
                    Distances.Euclidean(pivot.TopRight, candidate.BottomRight) <= distanceThreshold ||
                    Distances.Euclidean(pivot.TopRight, candidate.BottomLeft) <= distanceThreshold ||
                    Distances.Euclidean(pivot.TopRight, candidate.TopLeft) <= distanceThreshold);
        }
    }
}
