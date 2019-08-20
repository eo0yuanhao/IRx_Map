using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPoint = Microsoft.Msagl.Core.Geometry.Point;
using Point = System.Windows.Point;

using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Diagnostics.CodeAnalysis;
using SmoothedPolyline = Microsoft.Msagl.Core.Geometry.SmoothedPolyline;
using Site = Microsoft.Msagl.Core.Geometry.Site;
//using Microsoft.Msagl.Core.Geometry;

namespace Will_Map
{
    public enum PolylineCornerType
    {
        /// <summary>
        /// a corner to insert
        /// </summary>
        PreviousCornerForInsertion,
        /// <summary>
        /// a corner to delete
        /// </summary>
        CornerToDelete
    }
    public partial class NativeMethods
    {
        /// Return Type: BOOL->int  
        ///X: int  
        ///Y: int  
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);
    }
    class Common
    {

        public static void setFE_center(FrameworkElement fe, Point pos)
        {
            var w = fe.Width;
            var h = fe.Height;
            if (w == double.NaN || h == double.NaN)
                return;
            fe.SetValue(Canvas.LeftProperty, pos.X - w / 2);
            fe.SetValue(Canvas.TopProperty, pos.Y - h / 2);
        }

        // [Obsolete]
        public static Size MeasureText(string text, FontFamily family, double fontSize)
        {
            // PixelsPerDip
            FormattedText formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, new System.Windows.FontStyle(), FontWeights.Regular, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                125);

            return new Size(formattedText.Width, formattedText.Height);
        }
        public static Size MeasureNodeLabel(Node n)
        {
            return MeasureText(n.Label, new FontFamily(n.FontName), n.FontSize);
        }
        public static GPoint AglPoint(Point p)
        {
            return new GPoint(p.X, p.Y);
        }
        public static Point WpfPoint(GPoint p)
        {
            return new Point(p.X, p.Y);
        }
        public static void addCordinate(Canvas gv)
        {
            System.Windows.Shapes.Line a1 = new System.Windows.Shapes.Line();
            a1.X1 = -100;
            a1.Y1 = 0;
            a1.X2 = 100;
            a1.Y2 = 0;
            a1.Stroke = Brushes.Black;
            gv.Children.Add(a1);

            System.Windows.Shapes.Line a2 = new System.Windows.Shapes.Line();
            a2.X1 = 00;
            a2.Y1 = 100;
            a2.X2 = 0;
            a2.Y2 = -100;
            a2.Stroke = Brushes.Black;
            gv.Children.Add(a2);
        }

        public static Geometry CreateGeometryFromMsaglCurve(ICurve iCurve)
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                IsClosed = true,
                IsFilled = true,
                StartPoint = Common.WpfPoint(iCurve.Start)
            };

            var curve = iCurve as Curve;
            if (curve != null)
            {
                AddCurve(pathFigure, curve);
            }
            else
            {
                var rect = iCurve as RoundedRect;
                if (rect != null)
                    AddCurve(pathFigure, rect.Curve);
                else
                {
                    var ellipse = iCurve as Ellipse;
                    if (ellipse != null)
                    {
                        return new EllipseGeometry(Common.WpfPoint(ellipse.Center), ellipse.AxisA.Length,
                            ellipse.AxisB.Length);
                    }
                    var poly = iCurve as Polyline;
                    if (poly != null)
                    {
                        var p = poly.StartPoint.Next;
                        do
                        {
                            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(p.Point),
                                true));

                            p = p.NextOnPolyline;
                        } while (p != poly.StartPoint);
                    }
                }
            }


            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }
        static void AddCurve(PathFigure pathFigure, Curve curve)
        {
            foreach (ICurve seg in curve.Segments)
            {
                var ls = seg as LineSegment;
                if (ls != null)
                    pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(ls.End), true));
                else
                {
                    var ellipse = seg as Ellipse;
                    if (ellipse != null)
                        pathFigure.Segments.Add(new ArcSegment(Common.WpfPoint(ellipse.End),
                            new Size(ellipse.AxisA.Length, ellipse.AxisB.Length),
                                (double)GPoint.Angle(new GPoint(1, 0), ellipse.AxisA),
                            ellipse.ParEnd - ellipse.ParEnd >= Math.PI,
                            !ellipse.OrientedCounterclockwise()
                                ? SweepDirection.Counterclockwise
                                : SweepDirection.Clockwise, true));
                }
            }
        }



        public static Geometry GetICurveWpfGeometry(ICurve curve)
        {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                if (curve != null)
                {
                    FillContextForICurve(context, curve);
                }
                return streamGeometry;
            }
        }
        static internal void FillContextForICurve(StreamGeometryContext context, ICurve iCurve)
        {

            context.BeginFigure(Common.WpfPoint(iCurve.Start), false, false);

            var c = iCurve as Curve;
            if (c != null)
                FillContexForCurve(context, c);
            else
            {
                var cubicBezierSeg = iCurve as CubicBezierSegment;
                if (cubicBezierSeg != null)
                    context.BezierTo(Common.WpfPoint(cubicBezierSeg.B(1)), Common.WpfPoint(cubicBezierSeg.B(2)),
                                     Common.WpfPoint(cubicBezierSeg.B(3)), true, false);
                else
                {
                    var ls = iCurve as LineSegment;
                    if (ls != null)
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    else
                    {
                        var rr = iCurve as RoundedRect;
                        if (rr != null)
                            FillContexForCurve(context, rr.Curve);
                        else
                        {
                            var poly = iCurve as Polyline;
                            if (poly != null)
                                FillContexForPolyline(context, poly);
                            else
                            {
                                var ellipse = iCurve as Ellipse;
                                if (ellipse != null)
                                {
                                    //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                                    double sweepAngle = EllipseSweepAngle(ellipse);
                                    bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                                    Rectangle box = ellipse.FullBox();
                                    context.ArcTo(Common.WpfPoint(ellipse.End),
                                                  new Size(box.Width / 2, box.Height / 2),
                                                  sweepAngle,
                                                  largeArc,
                                                  sweepAngle < 0
                                                      ? SweepDirection.Counterclockwise
                                                      : SweepDirection.Clockwise,
                                                  true, true);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        static void FillContexForPolyline(StreamGeometryContext context, Polyline poly)
        {
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next)
                context.LineTo(Common.WpfPoint(pp.Point), true, false);
        }

        static void FillContexForCurve(StreamGeometryContext context, Curve c)
        {
            foreach (ICurve seg in c.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                {
                    context.BezierTo(Common.WpfPoint(bezSeg.B(1)),
                                     Common.WpfPoint(bezSeg.B(2)), Common.WpfPoint(bezSeg.B(3)), true, false);
                }
                else
                {
                    var ls = seg as LineSegment;
                    if (ls != null)
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    else
                    {
                        var ellipse = seg as Ellipse;
                        if (ellipse != null)
                        {
                            //       context.LineTo(Common.WpfPoint(ellipse.End),true,false);
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            Rectangle box = ellipse.FullBox();
                            context.ArcTo(Common.WpfPoint(ellipse.End),
                                          new Size(box.Width / 2, box.Height / 2),
                                          sweepAngle,
                                          largeArc,
                                          sweepAngle < 0
                                              ? SweepDirection.Counterclockwise
                                              : SweepDirection.Clockwise,
                                          true, true);
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }
        public static double EllipseSweepAngle(Ellipse ellipse)
        {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }

        public static void CreateSimpleEdgeCurveWithUnderlyingPolyline(Edge edge)
        {
            //ValidateArg.IsNotNull(edge, "edge");
            var a = edge.srcNode.Center;
            var b = edge.tarNode.Center;
            if (edge.srcNode == edge.tarNode)
            {
                var dx = 2.0 / 3 * edge.srcNode.borderCurve.BoundingBox.Width;
                var dy = edge.srcNode.borderCurve.BoundingBox.Height / 4;
                edge.underlying =  CreateUnderlyingPolylineForSelfEdge(new GPoint(a.X,a.Y), dx, dy);
                edge.lineCurve = edge.underlying.CreateCurve();
            }
            else
            {
                edge.underlying = SmoothedPolyline.FromPoints(new[] { new GPoint(a.X,a.Y),new GPoint(b.X,b.Y) });
                edge.lineCurve = edge.underlying.CreateCurve();
            }
            Edge.TrimSplineByBoundaryAndArrowhead(edge, false);  
                //.TrimSplineAndCalculateArrowheads(edge.EdgeGeometry,
                //                                                 edge.Source.BoundaryCurve,
                //                                                 edge.Target.BoundaryCurve,
                //                                                 edge.Curve, false, false);
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Msagl.Core.Geometry.Site")]
        internal static SmoothedPolyline CreateUnderlyingPolylineForSelfEdge(GPoint p0, double dx, double dy)
        {
            var p1 = p0 + new GPoint(0, dy);
            var p2 = p0 + new GPoint(dx, dy);
            var p3 = p0 + new GPoint(dx, -dy);
            var p4 = p0 + new GPoint(0, -dy);

            var site = new Site(p0);
            var polyline = new SmoothedPolyline(site);
            site = new Site(site, p1);
            site = new Site(site, p2);
            site = new Site(site, p3);
            site = new Site(site, p4);
            new Site(site, p0);
            return polyline;
        }
        



    }
    class SmoothedPolylineHelper
    {
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline"),
     SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static Tuple<Site, PolylineCornerType> AnalyzeInsertOrDeletePolylineCorner(SmoothedPolyline sm, GPoint point, double tolerance)
        {
            //tolerance += SelectedEdge.Edge.Attr.LineWidth;
            Site corner = FindCornerForEdit(sm, point, tolerance);
            if (corner != null)
                return new Tuple<Site, PolylineCornerType>(corner, PolylineCornerType.CornerToDelete);

            corner = GetPreviousSite(sm, point);
            if (corner != null)
                return new Tuple<Site, PolylineCornerType>(corner, PolylineCornerType.PreviousCornerForInsertion);

            return null;
        }

        public static SmoothedPolyline SmoothedPolylineTranslate(SmoothedPolyline smoothedPolyline, Vector deltaVec)
        {
            GPoint delta = new GPoint(deltaVec.X, deltaVec.Y);
            SmoothedPolyline sm = smoothedPolyline.Clone();
            for (Microsoft.Msagl.Core.Geometry.Site s = sm.HeadSite, s0 = sm.HeadSite;
                    s != null;
                    s = s.Next, s0 = s0.Next)
            {
                s.Point = s0.Point + delta;
            }
            return sm;
        }
        public static SmoothedPolyline SmoothedPolylineTransform(SmoothedPolyline smoothedPolyline, Matrix m)
        {
            //GPoint delta = new GPoint(deltaVec.X, deltaVec.Y);
            SmoothedPolyline sm = smoothedPolyline.Clone();
            PlaneTransformation pt = new PlaneTransformation(m.M11, m.M12,m.OffsetX,m.M21, m.M22, m.OffsetY);
            for (Microsoft.Msagl.Core.Geometry.Site s = sm.HeadSite, s0 = sm.HeadSite;
                    s != null;
                    s = s.Next, s0 = s0.Next)
            {
                s.Point = Common.AglPoint(m.Transform(Common.WpfPoint( s0.Point)));
            }
            return sm;
          
        }
        public static SmoothedPolyline SmoothedPolylineTransform(SmoothedPolyline smoothedPolyline, PlaneTransformation m)
        {
            //GPoint delta = new GPoint(deltaVec.X, deltaVec.Y);
            SmoothedPolyline sm = smoothedPolyline.Clone();
            //PlaneTransformation pt = new PlaneTransformation(m.M11, m.M12,m.OffsetX,m.M21, m.M22, m.OffsetY);
            for (Microsoft.Msagl.Core.Geometry.Site s = sm.HeadSite, s0 = sm.HeadSite;
                    s != null;
                    s = s.Next, s0 = s0.Next)
            {
                s.Point = m * s0.Point;
            }
            return sm;
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public static Site FindCornerForEdit(SmoothedPolyline underlyingPolyline, GPoint mousePoint, double tolerance)
        {
            Site site = underlyingPolyline.HeadSite.Next;
            tolerance *= tolerance; //square the tolerance

            do
            {
                if (site.Previous == null || site.Next == null)
                    continue; //don't return the first and the last corners
                GPoint diff = mousePoint - site.Point;
                if (diff * diff <= tolerance)
                    return site;

                site = site.Next;
            } while (site.Next != null);
            return null;
        }
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public static Site GetPreviousSite(SmoothedPolyline sm, GPoint point)
        {
            Site prevSite = sm.HeadSite;
            Site nextSite = prevSite.Next;
            do
            {
                if (BetweenSites(prevSite, nextSite, point))
                    return prevSite;
                prevSite = nextSite;
                nextSite = nextSite.Next;
            } while (nextSite != null);
            return null;
        }
        static bool BetweenSites(Site prevSite, Site nextSite, GPoint point)
        {
            double par = GPoint.ClosestParameterOnLineSegment(point, prevSite.Point, nextSite.Point);
            return par > 0.1 && par < 0.9;
        }
        public static void InsertSite(Site siteBeforeInsertion, GPoint point)
        {
            //creating the new site
            Site first = siteBeforeInsertion;
            Site second = first.Next;
            var s = new Site(first, point, second);
        }
        public static void DeleteSite(Site site)
        {
            site.Previous.Next = site.Next; //removing the site from the list
            site.Next.Previous = site.Previous;

        }
    }
  
}
