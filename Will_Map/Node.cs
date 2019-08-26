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
        Triangle,
        /// <summary>
        /// special arrow style ToItems
        /// </summary>
        ToItems
    }
    public enum LineStyle
    {
        Solid,
        Dashed,
        Dotted
    }


    public class IRxMap : Canvas, IDisposable
    {
        protected static List<IRxMap> _irxMapList = new List<IRxMap>();
        public static void adjustDisplayLayer(NodeBox parent)
        {
            foreach( var x in _irxMapList)
            {
                IRxObject obj;
                bool ok = x.objDict.TryGetValue(parent.Id(), out obj);
                if(ok && obj == parent)
                {
                    var bottomIndex = Canvas.GetZIndex(parent.innerCanvas);
                    foreach(var c in parent.ChildNodes)
                    {
                        int ci = Canvas.GetZIndex(c.innerCanvas);
                        if(ci <= bottomIndex)
                        {
                            Canvas.SetZIndex(c.innerCanvas, bottomIndex + 1);
                        }
                    }
                    break;
                }

            }
        }

        //private Canvas _adjustPosCanvas;
        public readonly Canvas MapCanvas = new Canvas();
        public readonly Canvas DecoratorCanvas = new Canvas();
        public readonly Dictionary<uint, IRxObject> objDict = new Dictionary<uint, IRxObject>();
        public uint idmax = 0;
        readonly Object idmax_Lock = new object();
        IRxObject _objectOnHitTest;
        public readonly HashSet<IRxObject> MarkedObjects = new HashSet<IRxObject>();
        //public Dictionary<IRxObject,> DecoratorObjectDict


        //protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        //{
        //    return new Size(200, 100);
        //    base.MeasureOverride(constraint);
        //    double width = base
        //        .InternalChildren
        //        .OfType<UIElement>()
        //        .Where(i => i.GetValue(Canvas.LeftProperty) != null)
        //        .Max(i => i.DesiredSize.Width + (double)i.GetValue(Canvas.LeftProperty));

        //    if (Double.IsNaN(width))
        //    {
        //        width = 0;
        //    }

        //    double height = base
        //        .InternalChildren
        //        .OfType<UIElement>()
        //        .Where(i => i.GetValue(Canvas.TopProperty) != null)
        //        .Max(i => i.DesiredSize.Height + (double)i.GetValue(Canvas.TopProperty));

        //    if (Double.IsNaN(height))
        //    {
        //        height = 0;
        //    }

        //    return new Size(width, height);
        //}
        public IRxMap()
        {
            //this .HorizontalAlignment = HorizontalAlignment.Stretch;
            //this .VerticalAlignment = VerticalAlignment.Stretch;
            this.Background = Brushes.Transparent;
            //this.SizeChanged += IRxMap_SizeChanged;
            //_adjustPosCanvas = new Canvas();
            //this.Children.Add(_adjustPosCanvas);
            //_adjustPosCanvas.Children.Add(MapCanvas);
            this.Children.Add(MapCanvas);
            Common.addCordinate(MapCanvas);
            this.Children.Add(DecoratorCanvas);
            _irxMapList.Add(this);
            //Common.addCordinate(this);
            
        }


        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~IRxMap()
        // {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            _irxMapList.Remove(this);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

        private void IRxMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //_adjustPosCanvas.RenderTransform = new MatrixTransform(1, 0, 0, 1, e.NewSize.Width/2, e.NewSize.Height/2);
        }

        public Node addNode(string label, double px, double py)
        {
            var n = new Node(label, px, py);
            return addNode(n);
        }
        Node addNode(Node n)
        {
            lock (idmax_Lock)
            {
                n.objId = idmax;
                idmax++;
            }
            objDict.Add(n.objId, n);
            MapCanvas.Children.Add(n.innerCanvas);
            return n;
        }

        public Edge addEdge(Node n1, Node n2)
        {
            var e = new Edge(n1, n2);
            lock (idmax_Lock)
            {
                e.objId = idmax;
                idmax++;
            }
            objDict.Add(e.objId, e);
            e.objId = idmax;
            MapCanvas.Children.Add(e.innerCanvas);
            n1.OutEdges.Add(e);
            n2.InEdges.Add(e);
            return e;
        }
        public NodeBox addBox(string label, double px, double py)
        {
            NodeBox b = new NodeBox(label, px, py);
            lock (idmax_Lock)
            {
                b.objId = idmax;
                idmax++;
            }
            objDict.Add(b.objId, b);
            Canvas.SetZIndex(b.innerCanvas, -1);
            MapCanvas.Children.Add(b.innerCanvas);
            return b;
        }
        public void removeEdge(Edge e)
        {
            e.srcNode.OutEdges.Remove(e);
            e.tarNode.InEdges.Remove(e);
            removeIRxObject(e);
        }
        public void removeNode(Node n)
        {
            foreach (var e in n.InEdges)
            {
                e.srcNode.OutEdges.Remove(e);
                removeIRxObject(e);
            }
            foreach (var e in n.OutEdges)
            {
                e.tarNode.InEdges.Remove(e);
                removeIRxObject(e);
            }
            removeIRxObject(n);
        }
        private void removeIRxObject(IRxObject obj)
        {
            objDict.Remove(obj.objId);
            MapCanvas.Children.Remove(obj.FE);
        }

        public void update(ICollection<IRxObject> objs)
        {
            foreach (var obj in objs)
            {
                obj.invalidate();
            }
        }
        //public void viewTransform(double scale,Point pos)
        //{
        //    viewTransform(scale, pos.X, pos.Y);
        //}
        public void viewTransform(double scale, double dx, double dy)
        {
            var mt = new MatrixTransform();
            //var mat =  MapCanvas.RenderTransform.Value;
            var mat = Matrix.Identity;
            //            //
            //mat.TranslatePrepend(pos.X, pos.Y);
            //            mat.ScalePrepend(scale, scale);
            //mat.TranslatePrepend(-pos.X, -pos.Y);
            //            //
            mat.ScaleAt(scale, scale, dx, dx);
            //this.MapCanvas.RenderTransform =
            mt.Matrix = mat;
            mt.Matrix = new Matrix(scale, 0, 0, scale, dx, dy);
            this.MapCanvas.RenderTransform = mt;   // new MatrixTransform(scale, 0, 0, scale, pos.X, pos.Y);
            //this.MapCanvas.RenderTransformOrigin = pos;
        }
        public void viewportOnRect(Rect r)
        {
            var scale = Math.Min(ActualWidth / r.Width, ActualHeight / r.Height);
            var mapCenter = new Point((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
            viewTransform(scale, ActualWidth / 2 - mapCenter.X * scale, ActualHeight / 2 - mapCenter.Y * scale);
        }
        public IRxObject getPointObject(Point p)
        {
            //_objectOnHitTest = null;
            //UpdateWithWpfHitObjectUnderMouseOnLocation(p, MyHitTestResultCallbackWithNoCallbacksToTheUser);
            //return _objectOnHitTest;
            return getPointObject(p, null);
        }
        public IRxObject getPointObject(Point p,ISet<IRxObject> exceptSet)
        {
            _objectOnHitTest = null;
            UpdateWithWpfHitObjectUnderMouseOnLocation(p, MyHitTestResultCallbackWithNoCallbacksToTheUser_fore, exceptSet);
            return _objectOnHitTest;
        }
        void UpdateWithWpfHitObjectUnderMouseOnLocation(Point pt, HitTestResultCallback hitTestResultCallback ,ISet<IRxObject> exceptSet)
        {
            //_objectUnderMouseDetectionLocation = pt;
            double MouseHitTolerance = 2;
            // Expand the hit test area by creating a geometry centered on the hit test point.
            var rect = new Rect(new Point(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                new Point(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
            var expandedHitTestArea = new RectangleGeometry(rect);
            // Set up a callback to receive the hit test result enumeration.
            HitTestFilterCallback filter = (DependencyObject depObj) =>
              {
                  var tg = ((depObj as FrameworkElement)?.Parent as Canvas)?.Tag as IRxObject;
                  if (tg != null && exceptSet.Contains(tg))
                      return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                  return HitTestFilterBehavior.Continue;
              };

            VisualTreeHelper.HitTest(MapCanvas, (exceptSet == null) ? null : filter,
                hitTestResultCallback,
                new GeometryHitTestParameters(expandedHitTestArea));
        }
        HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            var innerCanvas = frameworkElement as Canvas;
            if (innerCanvas != null)
            {
                if (innerCanvas.Tag != null)
                {
                    var obj = innerCanvas.Tag as IRxObject;
                    _objectOnHitTest = obj;
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }
        HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser_fore(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            var innerCanvas = frameworkElement.Parent as Canvas;
            if (innerCanvas != null)
            {
                if (innerCanvas.Tag != null)
                {
                    var obj = innerCanvas.Tag as IRxObject;
                    _objectOnHitTest = obj;
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }
        public void ChangingMarkedObjectStatus(IRxObject obj)
        {
            if (obj.Marked)
            {
                //MarkedObjects.Add(obj);
                if (obj.IRxType() == IRxObjectType.Node)
                    decorateMarkedNode(obj as Node);
                else if (obj.IRxType() == IRxObjectType.Edge)
                    //obj.invalidate();
                    decorateMarkedEdge(obj as Edge);
                else // NodeBox
                    decorateMarkedNodeBox(obj as NodeBox);
            }
            else
            {
                //MarkedObjects.Remove(obj);
                undecorateObject(obj);
            }
        }
        public HashSet<IRxObject> markedObjectDecorator = new HashSet<IRxObject>();

        public void undecorateObject(IRxObject obj)
        {
            if (!markedObjectDecorator.Contains(obj))
                return;
            markedObjectDecorator.Remove(obj);
            switch (obj.IRxType())
            {
                case IRxObjectType.Edge:
                    break;
                case IRxObjectType.Node:
                    (obj as Node).borderStrokeThickness /= 2;
                    break;
                case IRxObjectType.NodeBox:
                    (obj as NodeBox).borderStrokeThickness /= 2;
                    break;
            }

            obj.invalidate();
        }

        private void decorateMarkedEdge(Edge edge)
        {
            if (markedObjectDecorator.Contains(edge))
                return;
            markedObjectDecorator.Add(edge);
            //edge.lineStrokeThickness *= 2;
            edge.invalidate();
        }

        private void decorateMarkedNode(Node node)
        {
            if (markedObjectDecorator.Contains(node))
                return;
            markedObjectDecorator.Add(node);
            node.borderStrokeThickness *= 2;
            node.invalidate();
        }
        private void decorateMarkedNodeBox(NodeBox box)
        {
            if (markedObjectDecorator.Contains(box))
                return;
            markedObjectDecorator.Add(box);
            box.borderStrokeThickness *= 2;
            box.invalidate();
        }
        internal void save(string fileName)
        {
            var nodeDataList = new List<NodeTransData>();
            var edgeDataList = new List<EdgeTransData>();
            foreach (var o in objDict)
            {
                var v = o.Value;
                if (v.IRxType() == IRxObjectType.Node)
                {
                    var nt = new NodeTransData(v.Id(), v as Node);
                    nodeDataList.Add(nt);
                }
                else if (v.IRxType() == IRxObjectType.Edge)
                {
                    Edge e = v as Edge;
                    edgeDataList.Add(new EdgeTransData(e.srcNode.Id(), e.tarNode.Id(), e));
                }
            }
            var outstream = System.IO.File.Create(fileName);

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(outstream, nodeDataList);
            formatter.Serialize(outstream, edgeDataList);
            //Clipboard.SetData(DataFormats.Text, "haha");
            outstream.Close();
            //stm.Position = 0;
        }
        public void load(string filename)
        {
            MapCanvas.Children.Clear();
            Common.addCordinate(MapCanvas);
            objDict.Clear();
            idmax = 0;

            //var nodeDataList = new List<NodeTransData>();
            //var edgeDataList = new List<EdgeTransData>();
            var file = System.IO.File.OpenRead(filename);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var nodeDataList = formatter.Deserialize(file) as List<NodeTransData>;
            var edgeDataList = formatter.Deserialize(file) as List<EdgeTransData>;
            //Clipboard.SetData(DataFormats.Text, "haha");
            file.Close();
            var old2new = new Dictionary<uint, uint>();
            foreach (var o in nodeDataList)
            {
                var n = addNode(o.LabelText, o.Center.X, o.Center.Y);
                n.shape = o.Shape;
                old2new[o.tempid] = n.objId;
                n.invalidate();
            }
            foreach (var o in edgeDataList)
            {
                var e = addEdge(objDict[old2new[o.SrcTempId]] as Node, objDict[old2new[o.TarTempId]] as Node);
                e.underlying = o.underlyingPolyline;
                e.srcArrowStyle = o.srcArrowhead;
                e.tarArrowStyle = o.tarArrowhead;
                e.invalidate();
            }
        }
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
                //IRxMap.adjustDisplayLayer(_parent);
                var lyrIndex = Canvas.GetZIndex(innerCanvas);
                var parentLyrIndex = Canvas.GetZIndex(_parent.innerCanvas);
                if (lyrIndex <= parentLyrIndex)
                    Canvas.SetZIndex(_parent.innerCanvas, lyrIndex - 1);
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
        public Node srcNode;
        public Node tarNode;

        public ArrowStyle srcArrowStyle = ArrowStyle.NonSpecified;
        public ArrowStyle tarArrowStyle = ArrowStyle.NonSpecified;
        public LineStyle lineStyle = LineStyle.Solid;
       

        public double lineStrokeThickness = 2;

        public Path FE_line;
        public Path FE_srcArrowhead;
        public Path FE_tarArrowhead;
        public Path FE_markedDecorator;

        internal Arrowhead srcArrowhead;
        internal Arrowhead tarArrowhead;
        public SmoothedPolyline underlying;
        public ICurve lineCurve;
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
            if(lineStyle != LineStyle.Solid)
            {
                if(lineStyle == LineStyle.Dashed)
                    FE_line.StrokeDashArray = new DoubleCollection(new double[] {6,4 });
                else if(lineStyle == LineStyle.Dotted)
                    FE_line.StrokeDashArray = new DoubleCollection(new double[] {1,2 });
            }
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
            /// <summary>
            /// startPoint is arrow tail point , end is arrow head point,dir is vector from start to end not contain stroke thickness
            /// </summary>
            /// <param name="con"></param>
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <param name="dir"></param>
            /// <param name="style"></param>
            internal static void AddArrowWithStyle(StreamGeometryContext con, GPoint start, GPoint end, GPoint dir, ArrowStyle style)
            {
                Func<GPoint, System.Windows.Point> P2P = (GPoint p) => new System.Windows.Point(p.X, p.Y);
                Func<double, double> deg2rad = (double d) => d * Math.PI / 180.0;
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
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(deg2rad(ArrowAngle) );
                            con.BeginFigure(P2P(start), true, true);
                            con.LineTo(P2P(start + dir + h), true, true);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start + dir - h), true, true);
                        }
                        break;
                    case ArrowStyle.ODiamond:
                        {
                            dir = dir / 2;/// dir.Rotate90Ccw()/2;
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(deg2rad(ArrowAngle));
                            con.BeginFigure(P2P(start), false, true);
                            con.LineTo(P2P(start + dir + h), true, true);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start + dir - h), true, true);
                        }
                        break;
                    case ArrowStyle.Generalization:
                        {
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(deg2rad(ArrowAngle));
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
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(deg2rad(ArrowAngle));
                            con.BeginFigure(P2P(start + h), false, false);
                            con.LineTo(P2P(end), true, true);
                            con.LineTo(P2P(start - h), true, true);
                        }
                        break;
                    case ArrowStyle.Triangle:
                        {
                            GPoint h = dir.Rotate90Ccw() * Math.Tan(deg2rad(ArrowAngle));
                            con.BeginFigure(P2P(end + h), false, true);
                            con.LineTo(P2P(end - h), true, true);
                            con.LineTo(P2P(start), true, true);
                        }
                        break;
                    case ArrowStyle.ToItems:
                        {
                            var dl = (dir / 2 * 1.3).Rotate(-deg2rad(22.5));
                            var dr = dl.Rotate(deg2rad(45));
                            var beg = start + dir *0.8 - dr + dl - dr;
                            con.BeginFigure(P2P(beg),false,false);
                            Action<GPoint> lineTo = (GPoint p) => con.LineTo(P2P(p), true, true);
                            lineTo(beg + dr);
                            lineTo(beg + dr - dl);
                            lineTo(beg + 2 * dr - dl);
                            lineTo(beg + 2 * dr - 2 * dl);
                            lineTo(beg + 3 * dr - 2 * dl);
                            lineTo(beg + 3 * dr - 3 * dl);
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
