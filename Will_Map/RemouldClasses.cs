using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.Geometry.Curves; 
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using Arrowhead = Microsoft.Msagl.Core.Layout.Arrowhead;
namespace Will_Map
{
    class ArrowheadHelper
    {
     
        public static bool CalculateArrowheads(ref ICurve lineCurve,Arrowhead srcArrowhead,Arrowhead tarArrowhead)
        {
            //ValidateArg.IsNotNull(edgeGeometry, "edgeGeometry");
            if (srcArrowhead == null && tarArrowhead == null)
                return true;
            double parStart, parEnd;
            if (!FindTrimStartForArrowheadAtSource(lineCurve,srcArrowhead, out parStart))
                return false;
            if (!FindTrimEndForArrowheadAtTarget(lineCurve,tarArrowhead, out parEnd))
                return false;
            if (parStart > parEnd - ApproximateComparer.IntersectionEpsilon || ApproximateComparer.CloseIntersections(lineCurve[parStart], lineCurve[parEnd]))
                return false; //after the trim nothing would be left of the curve
            var c = lineCurve.Trim(parStart, parEnd);
            if (c == null)
                return false;
            if (srcArrowhead != null)
                srcArrowhead.TipPosition = PlaceTip(c.Start, lineCurve.Start, srcArrowhead.Offset);
            if (tarArrowhead != null)
                tarArrowhead.TipPosition = PlaceTip(c.End, lineCurve.End, tarArrowhead.Offset);
            lineCurve = c;
            return true;
        }
        static bool FindTrimEndForArrowheadAtTarget(ICurve lineCurve,Arrowhead tarArrowhead , out double p)
        {
            //Debug.Assert((edgeGeometry.Curve.End - edgeGeometry.Curve.Start).LengthSquared > eps);
            p = lineCurve.ParEnd;
            if (tarArrowhead == null || tarArrowhead.Length <= ApproximateComparer.DistanceEpsilon)
                return true;

            var eps = ApproximateComparer.DistanceEpsilon * ApproximateComparer.DistanceEpsilon;
            var curve = lineCurve;
            var arrowheadLength = tarArrowhead.Length;
            Point newCurveEnd;
            IList<IntersectionInfo> intersections;
            int reps = 10;
            do
            {
                reps--;
                if (reps == 0)
                    return false;
                intersections = GetIntersectionsWithArrowheadCircle(curve, arrowheadLength, curve.End);
                p = intersections.Count != 0 ? intersections.Max(x => x.Par1) : curve.ParEnd;
                newCurveEnd = lineCurve[p];
                arrowheadLength /= 2;
            } while (((newCurveEnd - curve.Start).LengthSquared < eps || intersections.Count == 0));
            //we would like to have at least something left from the curve
            return true;
        }

        static bool FindTrimStartForArrowheadAtSource(ICurve lineCurve, Arrowhead srcArrowhead, out double p)
        {
            p = 0; //does not matter
            if (srcArrowhead == null || srcArrowhead.Length <= ApproximateComparer.DistanceEpsilon)
                return true;
            var eps = ApproximateComparer.DistanceEpsilon * ApproximateComparer.DistanceEpsilon;
            Debug.Assert((lineCurve.End - lineCurve.Start).LengthSquared > eps);
            var arrowheadLength = srcArrowhead.Length;
            Point newStart;
            var curve = lineCurve;
            IList<IntersectionInfo> intersections;
            int reps = 10;
            do
            {
                reps--;
                if (reps == 0)
                    return false;
                intersections = GetIntersectionsWithArrowheadCircle(curve, arrowheadLength, curve.Start);
                p = intersections.Count != 0 ? intersections.Min(x => x.Par1) : curve.ParStart;
                newStart = curve[p];
                arrowheadLength /= 2;
            } while ((newStart - curve.End).LengthSquared < eps || intersections.Count == 0);
            //we are checkng that something will be left from the curve
            return true;
        }
        static IList<IntersectionInfo> GetIntersectionsWithArrowheadCircle(ICurve curve, double arrowheadLength, Point circleCenter)
        {
            Debug.Assert(arrowheadLength > 0);
            var e = new Ellipse(arrowheadLength, arrowheadLength, circleCenter);
            return Curve.GetAllIntersections(e, curve, true);
        }

        internal static Point PlaceTip(Point arrowBase, Point arrowTip, double offset)
        {
            if (Math.Abs(offset) < ApproximateComparer.Tolerance)
                return arrowTip;

            var d = arrowBase - arrowTip;
            var dLen = d.Length;
            if (dLen < ApproximateComparer.Tolerance)
                return arrowTip;
            return arrowTip + offset * (d / dLen);
        }
    }
    /// <summary>
    /// Class that provides methods for doing approximate comparisons.
    /// </summary>
    public static class ApproximateComparer
    {
        /// <summary>
        /// return true if the points are close enough
        /// </summary>
        public static bool Close(Point pointA, Point pointB, double tolerance)
        {
            return (pointA - pointB).Length <= tolerance;
        }

        /// <summary>
        /// return true if the points are close enough
        /// </summary>
        public static bool Close(Point pointA, Point pointB)
        {
            return Close(pointA, pointB, DistanceEpsilon);
        }

        /// <summary>
        /// return true if the numbers are close enough
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a"), SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
        public static bool Close(double a, double b)
        {
            return Math.Abs(a - b) <= DistanceEpsilon;
        }

        /// <summary>
        /// return true if the two rects are nearly identical (ignoring precision errors smaller than DistanceEpsilon)
        /// </summary>
        public static bool Close(Rectangle rect1, Rectangle rect2, double tolerance)
        {
            return Close(rect1.LeftBottom, rect2.LeftBottom, tolerance) && Close(rect1.RightTop, rect2.RightTop, tolerance);
        }

        /// <summary>
        /// return true if the two rects are nearly identical (ignoring precision errors smaller than DistanceEpsilon)
        /// </summary>
        /// <param name="rect1">first Rectangle</param>
        /// <param name="rect2">second Rectangle</param>
        /// <returns></returns>
        public static bool Close(Rectangle rect1, Rectangle rect2)
        {
            return Close(rect1, rect2, DistanceEpsilon);
        }

        /// <summary>
        /// The usual Compare operation, but within Curve.DistanceEpsilon.
        /// </summary>
        /// <param name="numberA"></param>
        /// <param name="numberB"></param>
        /// <returns></returns>
        public static int Compare(double numberA, double numberB)
        {
            double c = numberA - numberB;
            // The <= and >= here complement the < and > in Close(double, double).
            if (c <= -DistanceEpsilon)
                return -1;
            if (c >= DistanceEpsilon)
                return 1;
            return 0;
        }


        /// <summary>
        /// The usual > operation, but within Curve.DistanceEpsilon.
        /// </summary>
        /// <param name="numberA"></param>
        /// <param name="numberB"></param>
        /// <returns></returns>
        public static bool Greater(double numberA, double numberB)
        {
            return Compare(numberA, numberB) > 0;
        }

        /// <summary>
        /// The usual >= operation, but within Curve.DistanceEpsilon.
        /// </summary>
        /// <param name="numberA"></param>
        /// <param name="numberB"></param>
        /// <returns></returns>
        public static bool GreaterOrEqual(double numberA, double numberB)
        {
            return Compare(numberA, numberB) >= 0;
        }

        /// <summary>
        /// The usual less operation, but within Curve.DistanceEpsilon.
        /// </summary>
        /// <param name="numberA"></param>
        /// <param name="numberB"></param>
        /// <returns></returns>
        public static bool Less(double numberA, double numberB)
        {
            return Compare(numberA, numberB) < 0;
        }

        /// <summary>
        /// The usual less operation, but within Curve.DistanceEpsilon.
        /// </summary>
        /// <param name="numberA"></param>
        /// <param name="numberB"></param>
        /// <returns></returns>
        public static bool LessOrEqual(double numberA, double numberB)
        {
            return Compare(numberA, numberB) <= 0;
        }

        /// <summary>
        /// returns true if two intersections points are close enough
        /// </summary>
        /// <param name="intersectionPoint0"></param>
        /// <param name="intersectionPoint1"></param>
        /// <returns></returns>
        public static bool CloseIntersections(Point intersectionPoint0, Point intersectionPoint1)
        {
            Point c = intersectionPoint0 - intersectionPoint1;
            return c * c < IntersectionEpsilon * IntersectionEpsilon;
        }

        /// <summary>
        /// 0  iff value is close to zero;
        /// 1  iff value is strictly greater than zero;
        /// -1 iff value is strictly lower than zero;
        /// </summary>
        public static int Sign(double value)
        {
            if (value > DistanceEpsilon) return 1;
            if (value < -DistanceEpsilon) return -1;
            return 0;
        }

        static readonly double squareOfDistanceEpsilon = Math.Pow(10.0, -DistanceEpsilonPrecision * 2);

        /// <summary>
        /// The distance for two points considered to be the same
        /// </summary>
        internal static double DistanceEpsilon = Math.Pow(10.0, -DistanceEpsilonPrecision);

        /// <summary>
        /// The square of the distance epsilon for two points considered to be the same
        /// </summary>
        internal static double SquareOfDistanceEpsilon
        {
            get { return squareOfDistanceEpsilon; }
        }

        /// <summary>
        /// The digits of precision for the distance for two points considered to be the same
        /// </summary>
        internal static int DistanceEpsilonPrecision
        {
            get { return 6; }
        }

        static double distXEps = 0.0001;

        /// <summary>
        /// The distance for two intersection points considered to be the same
        /// </summary>
        public static double IntersectionEpsilon
        {
            get { return distXEps; }
            set { distXEps = value; }
        }

        static double tolerance = 1.0E-8;

        /// <summary>
        /// The distance where to real numbers are considered the same
        /// </summary>
        public static double Tolerance
        {
            get { return tolerance; }
            set { tolerance = value; }
        }

        static double userDefinedTolerance = tolerance;

        /// <summary>
        /// A tolerance that is settable by users of MSAGL to adjust performance.
        /// </summary>
        public static double UserDefinedTolerance
        {
            get { return userDefinedTolerance; }
            set { userDefinedTolerance = value; }
        }

        /// <summary>
        /// point coordinates will be rounded to NumberOfDigitsToRound for comparison
        /// </summary>
        static internal int NumberOfDigitsToRound
        {
            get { return DistanceEpsilonPrecision; }
        }

        static internal Point Round(Point point)
        {
            return new Point(Round(point.X), Round(point.Y));
        }

        static internal Point Round(Point point, int numberDigitsToRound)
        {
            return new Point(Math.Round(point.X, numberDigitsToRound), Math.Round(point.Y, numberDigitsToRound));
        }

        static internal double Round(double value)
        {
            return Math.Round(value, NumberOfDigitsToRound);
        }
    }
}
