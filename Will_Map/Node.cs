using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
//using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using GPoint = Microsoft.Msagl.Core.Geometry.Point;
using Point = System.Windows.Point;
using SmoothedPolyline = Microsoft.Msagl.Core.Geometry.SmoothedPolyline;
using Site = Microsoft.Msagl.Core.Geometry.Site;
namespace Will_Map
{

    public enum NodeShape
    {
        PlainText,
        Box,
        Rect,
        Circle
    }
    public enum IRxObjectType
    {
        Node,
        Edge,
        NodeBox
    }

    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#k:arrowType
    /// </summary>
    public enum ArrowStyle
    {
        ///<summary>
        ///The default.
        ///</summary>
        NonSpecified,
        /// <summary>
        /// The default.
        /// </summary>
        None,
        /// <summary>
        /// The default
        /// </summary>
        Normal,
        /// <summary>
        /// straight line only
        /// </summary>
        StraightLine,
        /// <summary>
        /// Looks like a tee
        /// </summary>
        Tee,
        /// <summary>
        /// Diamond (UML symbol for a Containment)
        /// </summary>
        Diamond,
        /// <summary>
        /// ODiamond (UML symbol for an Aggregation)
        /// </summary>
        ODiamond,
        /// <summary>
        /// Generalization (UML symbol for a Generalization)
        /// </summary>
        Generalization,
        /// <summary>
        /// Circle with empty fill
        /// </summary>
        Circle,
        /// <summary>
        /// Rectangle with empty fill
        /// </summary>
        Rectangle,
        /// <summary>
        /// Vee Arrow
        /// </summary>
        Vee,
        /// <summary>
        /// triangle
        /// </summary>
        Triangle
    }
    public enum LineStyle
    {
        Solid,
        Dashed,
        Dotted
    }
    public abstract class IRxObject
    {
        //public abstract uint Id();

        internal uint objId;
        public uint Id()
        {
            return objId;
        }
        public abstract IRxObjectType IRxType();
        public abstract void invalidate();
        public bool Marked;
        public FrameworkElement FE;
        protected bool _needUpdate=true;
        public bool NeedUpdate { get => _needUpdate; }
    }
    public abstract class NodeBase : IRxObject
    {
        protected string _label;
        public string Label
        {
            get => _label;
            set { _label = value;_needUpdate = true; }
        }
        internal static string defaultFontName = "Times-Roman";
        internal static double defaultFontSize = 20;
        protected string _fontName;
        protected double _fontSize = defaultFontSize;
        public readonly HashSet<Edge> InEdges = new HashSet<Edge>();
        public readonly HashSet<Edge> OutEdges = new HashSet<Edge>();       

        public double FontSize { get => _fontSize; set { _fontSize = value; _needUpdate = true; } }
        public string FontName
        {
            get
            {
                if (string.IsNullOrEmpty(_fontName))
                    return defaultFontName;
                else
                    return _fontName;
            }
            set { _fontName = value; _needUpdate = true; }
        }

        internal readonly Canvas innerCanvas = new Canvas();
        protected Point _center;
        public virtual Point Center
        {
            get => _center;
            set { _center = value; _needUpdate = true; }
        }
        public Path FE_borderShape;
        public ICurve borderCurve;
        public double borderMargin;
        public double borderStrokeThickness = 2;
        //public FrameworkElement FE;

        private NodeBox _parent;
        public NodeBox Parent
        {
            get => _parent;
            set
            {
                if (_parent != null)
                {
                    _parent.ChildNodes.Remove(this);
                }
                _parent = value;
                _parent.ChildNodes.Add(this);
            }
        }

    }
    public class Node : NodeBase
    {
        public Node(string label, double px, double py)
        {
            this.Label = label;
            Center = new Point(px, py);
            shape = NodeShape.Box;
            innerCanvas.Tag = this;
            FE = innerCanvas;

            borderMargin = 3;
        }

        //public Point pos;
        public NodeShape shape;

        //internal uint objId;
        //public override uint Id()
        //{
        //    return objId;
        //}

        public override IRxObjectType IRxType()
        {
            return IRxObjectType.Node;
        }
        public TextBlock FE_label;

        public override void invalidate()
        {
            if (!_needUpdate)
                return;

            Size labelSize = new Size(6, 6);
            innerCanvas.Children.Clear();
            if (!string.IsNullOrEmpty(Label))
            {
                if (FE_label == null)
                {
                    FE_label = new TextBlock();
                    FE_label.FontSize = FontSize;
                }


                //if (label == null || label == "")
                //{
                //    FE_label.Width = labelSize.Width;
                //    FE_label.Height = labelSize.Height;
                //    FE_label.Background = Brushes.Transparent;
                //}  else
                //{ 
                if (FE_label.Text != Label)
                {

                    FE_label.Text = Label;
                    //FE_label.Width = Double.NaN;
                    //FE_label.Height = Double.NaN;
                    //直接Measure出来的DesireSize没有变化，不知道哪里的问题,现在只有新建一个TextBlock对尺寸进行测量

                    var n_t = new TextBlock();
                    n_t.FontSize = 20;
                    n_t.Text = Label;
                    n_t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    labelSize = n_t.DesiredSize;
                    //FE_label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    FE_label.Width = labelSize.Width;
                    FE_label.Height = labelSize.Height;
                }
                else
                {
                    labelSize = FE_label.RenderSize;
                }

                innerCanvas.Children.Add(FE_label);
                Common.setFE_center(FE_label, new Point(Center.X, Center.Y));
            }
            Size borderSize = new Size(labelSize.Width + 2 * borderMargin, labelSize.Height + 2 * borderMargin);
            borderCurve = CurveFactory.CreateRectangleWithRoundedCorners(borderSize.Width, borderSize.Height, 2, 2, new GPoint(Center.X, Center.Y));// center.X, center.Y));
            if (shape != NodeShape.PlainText)
            {
                if (FE_borderShape == null)
                {
                    FE_borderShape = new Path();
                }
                FE_borderShape.StrokeThickness = borderStrokeThickness;
                FE_borderShape.Stroke = Brushes.Black;
                FE_borderShape.Fill = Brushes.Transparent;

                FE_borderShape.Data = CreatePathFromNodeBoundary();
                innerCanvas.Children.Add(FE_borderShape);
                //FE_borderShape.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                //FE_borderShape.Width = FE_borderShape.DesiredSize.Width;
                //FE_borderShape.Height = FE_borderShape.DesiredSize.Height;
                //Common.setFE_center(FE_borderShape, new Point(0, 0));
            }
            // //Common.setFE_center(innerCanvas, center);
            //innerCanvas.RenderTransform = new MatrixTransform(1,0,0,1,center.X,center.Y);
            //if(Parent != null)
            //{
            //    Parent.invalidate();
            //}
        }

        Geometry CreatePathFromNodeBoundary()
        {
            Geometry geometry;
            switch (shape)
            {
                case NodeShape.Box:
                    //case Shape.House:
                    //case Shape.InvHouse:
                    //case Shape.Diamond:
                    //case Shape.Octagon:
                    //case Shape.Hexagon:

                    geometry = Common.CreateGeometryFromMsaglCurve(borderCurve);
                    break;

                //case Shape.DoubleCircle:
                //    geometry = DoubleCircle();
                //    break;
                case NodeShape.PlainText:
                    geometry = null;
                    break;

                default:
                    geometry = GetEllipseGeometry();
                    break;
            }

            return geometry;
        }


        Geometry GetEllipseGeometry()
        {
            return new EllipseGeometry(Center, borderCurve.BoundingBox.Width / 2,
                borderCurve.BoundingBox.Height / 2);
        }
    }




    public class Edge : IRxObject
    {
        public Edge(Node src, Node tar)
        {
            srcNode = src;
            tarNode = tar;
            innerCanvas.Tag = this;
            FE = innerCanvas;
        }
        public override IRxObjectType IRxType()
        {
            return IRxObjectType.Edge;
        }
        public Node label;
        public SmoothedPolyline underlying;

        public ICurve lineCurve;
        public ArrowStyle srcArrowStyle = ArrowStyle.NonSpecified;
        public ArrowStyle tarArrowStyle = ArrowStyle.NonSpecified;
        internal Arrowhead srcArrowhead;
        internal Arrowhead tarArrowhead;
        public double lineStrokeThickness = 2;

        public Path FE_line;
        public Path FE_srcArrowhead;
        public Path FE_tarArrowhead;
        public Path FE_markedDecorator;

        public Node srcNode;
        public Node tarNode;
        internal readonly Canvas innerCanvas = new Canvas();
        public delegate void EdgeMarkedDraw(Edge e);
        public EdgeMarkedDraw markedDraw;

        public static ICurve TrimSplineByBoundaryAndArrowhead(Edge edge, bool narrowestInterval)
        {
            if (edge.lineCurve == null)
                return null;
            var outSpline = Curve.TrimEdgeSplineWithNodeBoundaries(edge.srcNode.borderCurve, edge.tarNode.borderCurve,
                                    edge.lineCurve, narrowestInterval);

            if ((edge.srcArrowhead == null ||
                 edge.srcArrowhead.Length < ApproximateComparer.DistanceEpsilon) &&
                (edge.tarArrowhead == null ||
                 edge.tarArrowhead.Length < ApproximateComparer.DistanceEpsilon))
                return outSpline; //there are no arrowheads

            double sourceArrowheadSavedLength = edge.srcArrowhead != null
                                                    ? edge.srcArrowhead.Length
                                                    : 0;
            double targetArrowheadSavedLength = edge.tarArrowhead != null
                                                    ? edge.tarArrowhead.Length
                                                    : 0;
            var len = (outSpline.End - outSpline.Start).Length;
            if (edge.srcArrowhead != null)
                edge.srcArrowhead.Length = Math.Min(len, sourceArrowheadSavedLength);
            if (edge.tarArrowhead != null)
                edge.tarArrowhead.Length = Math.Min(len, targetArrowheadSavedLength);
            bool success = ArrowheadHelper.CalculateArrowheads(ref outSpline, edge.srcArrowhead, edge.tarArrowhead);
            return outSpline;
        }
        public override void invalidate()
        {
            if (!_needUpdate)
                return;
            //Delegate Common.
            innerCanvas.Children.Clear();
            if (underlying == null)
            {
                underlying = SmoothedPolyline.FromPoints(new[] { Common.AglPoint(srcNode.Center), Common.AglPoint(tarNode.Center) });

            }

            //var lineSeg = new LineSegment();
            //lineCurve = lineSeg;
            if (FE_line == null)
            {
                FE_line = new Path();
            }
            FE_line.Stroke = Brushes.Black;
            FE_line.StrokeThickness = lineStrokeThickness;

            if (srcArrowStyle == ArrowStyle.NonSpecified || srcArrowStyle == ArrowStyle.None) {
                srcArrowhead = null;
            } else {
                srcArrowhead = new Arrowhead() { Length = Edge_Draw.ArrowheadLength };
                if (FE_srcArrowhead == null)
                {
                    FE_srcArrowhead = new Path();
                    FE_srcArrowhead.Stroke = Brushes.Black;
                    FE_srcArrowhead.StrokeThickness = 1;
                    FE_srcArrowhead.Fill = Brushes.Black;
                }
            }
            if (tarArrowStyle == ArrowStyle.NonSpecified || tarArrowStyle == ArrowStyle.None) {
                tarArrowhead = null;
            } else {
                tarArrowhead = new Arrowhead() { Length = Edge_Draw.ArrowheadLength };
                if (FE_tarArrowhead == null)
                {
                    FE_tarArrowhead = new Path();
                    FE_tarArrowhead.Stroke = Brushes.Black;
                    FE_tarArrowhead.StrokeThickness = 1;
                    FE_tarArrowhead.Fill = Brushes.Black;
                }
            }
            lineCurve = underlying.CreateCurve();
            lineCurve = TrimSplineByBoundaryAndArrowhead(this, false);
            FE_line.Data = Common.GetICurveWpfGeometry(lineCurve);
            innerCanvas.Children.Add(FE_line);
            if (srcArrowhead != null)
            {
                var streamGeometry = new StreamGeometry();
                using (StreamGeometryContext context = streamGeometry.Open())
                {
                    Edge_Draw.AddArrow(context, lineCurve.Start, srcArrowhead.TipPosition, 1.0, srcArrowStyle);
                }
                FE_srcArrowhead.Data = streamGeometry;
                innerCanvas.Children.Add(FE_srcArrowhead);
            }
            if (tarArrowhead != null)
            {
                var streamGeometry = new StreamGeometry();
                using (StreamGeometryContext context = streamGeometry.Open())
                {
                    Edge_Draw.AddArrow(context, lineCurve.End, tarArrowhead.TipPosition, 1.0, tarArrowStyle);
                }
                FE_tarArrowhead.Data = streamGeometry;
                innerCanvas.Children.Add(FE_tarArrowhead);
            }
            if (Marked)
            {
                if (markedDraw == null)
                    markedDraw = Edge_Draw.DefaultEdgeMarkedDecoratorDelegate;
                markedDraw(this);
                innerCanvas.Children.Add(FE_markedDecorator);
            }
        }
        public class Edge_Draw
        {
            public static double ArrowheadLength = 10;
            static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
            static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);
            const double ArrowAngle = 30.0; //degrees
            internal static void AddArrow(StreamGeometryContext context, GPoint start, GPoint end, double thickness, ArrowStyle arrowStyle)
            {
                if (thickness > 1)
                {
                    GPoint dir = end - start;
                    GPoint h = dir;
                    double dl = dir.Length;
                    if (dl < 0.001)
                        return;
                    dir /= dl;

                    var s = new GPoint(-dir.Y, dir.X);
                    double w = 0.5 * thickness;
                    GPoint s0 = w * s;

                    s *= h.Length * HalfArrowAngleTan;
                    s += s0;

                    double rad = w / HalfArrowAngleCos;

                    context.BeginFigure(Common.WpfPoint(start + s), true, true);
                    context.LineTo(Common.WpfPoint(start - s), true, false);
                    context.LineTo(Common.WpfPoint(end - s0), true, false);
                    context.ArcTo(Common.WpfPoint(end + s0), new Size(rad, rad),
                                  Math.PI - ArrowAngle, false, SweepDirection.Clockwise, true, false);
                }
                else
                {
                    GPoint dir = end - start;
                    double dl = dir.Length;
                    //take into account the widths
                    double delta = Math.Min(dl / 2, thickness + thickness / 2);
                    dir *= (dl - delta) / dl;
                    end = start + dir;
                    AddArrowWithStyle(context, start, end, dir, arrowStyle);

                }
            }
            internal static void AddArrowWithStyle(StreamGeometryContext con, GPoint start, GPoint end, GPoint dir, ArrowStyle style)
            {
                Func<GPoint, System.Windows.Point> P2P = (GPoint p) => new System.Windows.Point(p.X, p.Y);
                switch (style)
                {

                    case ArrowStyle.NonSpecified:
                    case ArrowStyle.Normal:
                        {
                            dir = dir.Rotate(Math.PI / 2);
                            GPoint s = dir * HalfArrowAngleTan;

                            con.BeginFigure(Common.WpfPoint(start + s), true, true);
                            con.LineTo(Common.WpfPoint(end), true, true);
                            con.LineTo(Common.WpfPoint(start - s), true, true);

                        }
                        break;
                    case ArrowStyle.None:
                        break;

                    case ArrowStyle.StraightLine:
                        con.BeginFigure(Common.WpfPoint(start), false, false);
                        con.LineTo(Common.WpfPoint(end), true, true);
                        break;

                    case ArrowStyle.Tee:
                        {
                            con.BeginFigure(Common.WpfPoint(start), false, false); // .DrawLine(p, PointF(start), PointF(end));
                            con.LineTo(Common.WpfPoint(end), true, true);

                            dir = dir.Rotate90Cw();
                            GPoint s = dir * HalfArrowAngleTan;
                            con.BeginFigure(P2P(start + s), false, false);
                            con.LineTo(P2P(start - s), true, true);
                        }
                        break;
                    case ArrowStyle.Diamond:
                        {
                            dir = dir / 2;
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(ArrowAngle * Math.PI / 180.0);
                            con.BeginFigure(P2P(start), true, true);
                            con.LineTo(P2P(start + dir + h), true, true);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start + dir - h), true, true);
                        }
                        break;
                    case ArrowStyle.ODiamond:
                        {
                            dir = dir / 2;/// dir.Rotate90Ccw()/2;
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(ArrowAngle * Math.PI / 180.0);
                            con.BeginFigure(P2P(start), false, true);
                            con.LineTo(P2P(start + dir + h), true, true);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start + dir - h), true, true);
                        }
                        break;
                    case ArrowStyle.Generalization:
                        {
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(ArrowAngle * Math.PI / 180.0);
                            con.BeginFigure(P2P(start + h), false, true);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start - h), true, true);
                        }
                        break;
                    case ArrowStyle.Circle:
                        {
                            con.BeginFigure(P2P(start), false, true);
                            var r = dir.Length / 2;
                            //drawing a complete circle by two part arc
                            con.ArcTo(P2P(end), new Size(r, r), 180, true, SweepDirection.Clockwise, true, true);
                            con.ArcTo(P2P(start), new Size(r, r), 180, true, SweepDirection.Clockwise, true, true);
                        }
                        break;
                    case ArrowStyle.Vee:
                        {
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(ArrowAngle * Math.PI / 180.0);
                            con.BeginFigure(P2P(start + h), false, false);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start - h), true, true);
                        }
                        break;
                    case ArrowStyle.Triangle:
                        {
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(ArrowAngle * Math.PI / 180.0);
                            con.BeginFigure(P2P(end + h), false, true);
                            con.LineTo(P2P(end - h), true, true);
                            con.LineTo(P2P(start), true, true);
                        }
                        break;
                    default:
                        break;
                }
            }
            public static void DefaultEdgeMarkedDecoratorDelegate(Edge edge)
            {
                Func<GPoint, Point> f = x => Common.WpfPoint(x);
                if (edge.FE_markedDecorator == null)
                {
                    edge.FE_markedDecorator = new Path();
                    edge.FE_markedDecorator.StrokeThickness = 2;
                    edge.FE_markedDecorator.Stroke = Brushes.Red;
                }
                var g = new GeometryGroup();
                var fe = edge.FE_markedDecorator;
                Site s = edge.underlying.HeadSite;
                while (s != null)
                {
                    Site n = s.Next;
                    if (n != null)
                    {
                        g.Children.Add(new LineGeometry(f(s.Point), f(n.Point)));
                    }
                    g.Children.Add(new EllipseGeometry(f(s.Point), 2, 2));
                    s = s.Next;
                }
                fe.Data = g;
            }
        }

    }

    public class NodeBox : NodeBase
    {
        public NodeBox(string label,double px,double py)
        {
            this.Label = label;
            _center = new Point(px, py);
            innerCanvas.Tag = this;
            FE = innerCanvas;

            borderMargin = 15;
        }

        //public Point defaultCenter;

        public readonly HashSet<NodeBase> ChildNodes = new HashSet<NodeBase>();


        public override Point Center
        {
            set
            {
                if (ChildNodes.Count == 0)
                {
                    _center = value;
                    return;
                }
                else
                {
                    Vector deltaMove = value - _center;
                    foreach( var n in ChildNodes)
                    {
                        n.Center += deltaMove;
                    }
                    _needUpdate = true;
                }
            }
        }
        public override void invalidate()
        {
            if (!_needUpdate)
                return;

            foreach (var n in ChildNodes)
                if (n.NeedUpdate)
                    n.invalidate();

            innerCanvas.Children.Clear();
            if (ChildNodes.Count == 0)
            {
                Size borderSize = new Size( 2 * borderMargin, 2 * borderMargin);
                borderCurve = CurveFactory.CreateRectangleWithRoundedCorners(borderSize.Width, borderSize.Height, 2, 2, new GPoint(Center.X, Center.Y));
            }
            else
            {
                //NodeBox 的边框由子Node绝对，故在此之前必须确保所有子Node都已经更新了
                var totalBound = ChildNodes.Select(n => n.borderCurve.BoundingBox).Aggregate((a, b) => { a.Add(b); return a; });
                Size borderSize = new Size(totalBound.Width + 2 * borderMargin, totalBound.Height + 2 * borderMargin);
                _center = Common.WpfPoint(totalBound.Center);
                borderCurve = CurveFactory.CreateRectangleWithRoundedCorners(borderSize.Width, borderSize.Height, 2, 2, totalBound.Center);    
            }

            if (FE_borderShape == null)
            {
                FE_borderShape = new Path();
            }
            FE_borderShape.StrokeThickness = borderStrokeThickness;
            FE_borderShape.Stroke = Brushes.Black;
            FE_borderShape.Fill = Brushes.Transparent;

            FE_borderShape.Data = Common.CreateGeometryFromMsaglCurve(borderCurve);
            innerCanvas.Children.Add(FE_borderShape);
        }
        public override IRxObjectType IRxType()
        {
            return IRxObjectType.NodeBox;
        }
        public TextBlock FE_label;

    }
}
