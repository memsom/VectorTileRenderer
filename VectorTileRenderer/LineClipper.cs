﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace VectorTileRenderer
{
    static class LineClipper
    {
        static VTOutCode ComputeOutCode(double x, double y, VTRect r)
        {
            var code = VTOutCode.Inside;

            if (x < r.Left) code |= VTOutCode.Left;
            if (x > r.Right) code |= VTOutCode.Right;
            if (y < r.Top) code |= VTOutCode.Top;
            if (y > r.Bottom) code |= VTOutCode.Bottom;

            return code;
        }

        static VTOutCode ComputeOutCode(VTPoint p, VTRect r) { return ComputeOutCode(p.X, p.Y, r); }

        static VTPoint CalculateIntersection(VTRect r, VTPoint p1, VTPoint p2, VTOutCode clipTo)
        {
            var dx = (p2.X - p1.X);
            var dy = (p2.Y - p1.Y);

            var slopeY = dx / dy; // slope to use for possibly-vertical lines
            var slopeX = dy / dx; // slope to use for possibly-horizontal lines

            if (clipTo.HasFlag(VTOutCode.Top))
            {
                return new VTPoint(
                    p1.X + slopeY * (r.Top - p1.Y),
                    r.Top
                    );
            }
            if (clipTo.HasFlag(VTOutCode.Bottom))
            {
                return new VTPoint(
                    p1.X + slopeY * (r.Bottom - p1.Y),
                    r.Bottom
                    );
            }
            if (clipTo.HasFlag(VTOutCode.Right))
            {
                return new VTPoint(
                    r.Right,
                    p1.Y + slopeX * (r.Right - p1.X)
                    );
            }
            if (clipTo.HasFlag(VTOutCode.Left))
            {
                return new VTPoint(
                    r.Left,
                    p1.Y + slopeX * (r.Left - p1.X)
                    );
            }
            throw new ArgumentOutOfRangeException("clipTo = " + clipTo);
        }

        public static Tuple<VTPoint, VTPoint> ClipSegment(VTRect r, VTPoint p1, VTPoint p2)
        {
            // classify the endpoints of the line
            var outCodeP1 = ComputeOutCode(p1, r);
            var outCodeP2 = ComputeOutCode(p2, r);
            var accept = false;

            while (true)
            { // should only iterate twice, at most
              // Case 1:
              // both endpoints are within the clipping region
                if ((outCodeP1 | outCodeP2) == VTOutCode.Inside)
                {
                    accept = true;
                    break;
                }

                // Case 2:
                // both endpoints share an excluded region, impossible for a line between them to be within the clipping region
                if ((outCodeP1 & outCodeP2) != 0)
                {
                    break;
                }

                // Case 3:
                // The endpoints are in different regions, and the segment is partially within the clipping rectangle

                // Select one of the endpoints outside the clipping rectangle
                var outCode = outCodeP1 != VTOutCode.Inside ? outCodeP1 : outCodeP2;

                // calculate the intersection of the line with the clipping rectangle
                var p = CalculateIntersection(r, p1, p2, outCode);

                // update the point after clipping and recalculate outcode
                if (outCode == outCodeP1)
                {
                    p1 = p;
                    outCodeP1 = ComputeOutCode(p1, r);
                }
                else
                {
                    p2 = p;
                    outCodeP2 = ComputeOutCode(p2, r);
                }
            }
            // if clipping area contained a portion of the line
            if (accept)
            {
                return new Tuple<VTPoint, VTPoint>(p1, p2);
            }

            // the line did not intersect the clipping area
            return null;
        }

        static VTRect getLineRect(List<VTPoint> polyLine)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var point in polyLine)
            {
                if (point.X < minX)
                {
                    minX = point.X;
                }
                if (point.Y < minY)
                {
                    minY = point.Y;
                }

                if (point.X > maxX)
                {
                    maxX = point.X;
                }
                if (point.Y > maxY)
                {
                    maxY = point.Y;
                }
            }

            return new VTRect(minX, minY, maxX - minX, maxY - minY);
        }

        public static List<VTPoint> ClipPolyline(List<VTPoint> polyLine, VTRect bounds)
        {
            var lineRect = getLineRect(polyLine);

            if(!bounds.IntersectsWith(lineRect))
            {
                return null;
            }

            List<VTPoint> newLine = null;

            for (int i = 1; i < polyLine.Count; i++)
            {
                var p1 = polyLine[i - 1];
                var p2 = polyLine[i];

                var newSegment = ClipSegment(bounds, p1, p2);

                if(newSegment != null)
                {
                    if(newLine == null)
                    {
                        newLine = new List<VTPoint>();
                        newLine.Add(newSegment.Item1);
                        newLine.Add(newSegment.Item2);
                    }
                    else
                    {
                        if(newLine.Last() == newSegment.Item1)
                        {
                            newLine.Add(newSegment.Item2);
                        } else
                        {
                            newLine.Add(newSegment.Item1);
                            newLine.Add(newSegment.Item2);
                        }
                    }
                } else
                {

                }
            }

            return newLine;

        }


    }
}
