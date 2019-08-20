using Microsoft.Msagl.Core.Geometry.Curves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmoothedPolyline = Microsoft.Msagl.Core.Geometry.SmoothedPolyline;
using Site = Microsoft.Msagl.Core.Geometry.Site;
using GPoint = Microsoft.Msagl.Core.Geometry.Point;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using EdgeAttrEditor = Will_Map.UserControls.EdgeAttrEditor;
using EdgeAttrApplyEditor = Will_Map.UserControls.EdgeAttrApplyEditor;

namespace Will_Map
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public class IRxMap:Canvas
    {
        //private Canvas _adjustPosCanvas;
        public readonly Canvas MapCanvas = new Canvas();
        public readonly Canvas DecoratorCanvas = new Canvas();
        public readonly Dictionary<uint, IRxObject> objDict= new Dictionary<uint, IRxObject>();
        public uint idmax = 0;
        readonly Object idmax_Lock= new object();
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

            //Common.addCordinate(this);
        }

        private void IRxMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //_adjustPosCanvas.RenderTransform = new MatrixTransform(1, 0, 0, 1, e.NewSize.Width/2, e.NewSize.Height/2);
        }

        public Node addNode(string label,double px,double py)
        {
            var n = new Node(label,px,py);
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
            var e=new Edge(n1, n2);
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
        public NodeBox addBox(string label,double px,double py)
        {
            NodeBox b = new NodeBox(label, px, py);
            lock (idmax_Lock)
            {
                b.objId = idmax;
                idmax++;
            }
            objDict.Add(b.objId, b);
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
            foreach( var e in n.InEdges)
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
        private void removeIRxObject (IRxObject obj)
        {
            objDict.Remove(obj.objId);
            MapCanvas.Children.Remove(obj.FE);
        }

        public void update(ICollection<IRxObject> objs)
        {
            foreach(var obj in objs)
            {
                obj.invalidate();
            }
        }
        //public void viewTransform(double scale,Point pos)
        //{
        //    viewTransform(scale, pos.X, pos.Y);
        //}
        public void viewTransform(double scale, double dx,double dy)
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
            _objectOnHitTest = null;
            UpdateWithWpfHitObjectUnderMouseOnLocation(p, MyHitTestResultCallbackWithNoCallbacksToTheUser);
            return _objectOnHitTest;
        }
        void UpdateWithWpfHitObjectUnderMouseOnLocation(Point pt, HitTestResultCallback hitTestResultCallback)
        {
            //_objectUnderMouseDetectionLocation = pt;
            double MouseHitTolerance = 2;
            // Expand the hit test area by creating a geometry centered on the hit test point.
            var rect = new Rect(new Point(pt.X - MouseHitTolerance, pt.Y - MouseHitTolerance),
                new Point(pt.X + MouseHitTolerance, pt.Y + MouseHitTolerance));
            var expandedHitTestArea = new RectangleGeometry(rect);
            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(MapCanvas, null,
                hitTestResultCallback,
                new GeometryHitTestParameters(expandedHitTestArea));
        }
        HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;
            var innerCanvas = frameworkElement.Parent as Canvas;
            if(innerCanvas!= null)
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
                } else if (v.IRxType() == IRxObjectType.Edge)
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
            foreach(var o in edgeDataList)
            {
                var e = addEdge(objDict[old2new[o.SrcTempId]] as Node, objDict[ old2new[o.TarTempId]] as Node);
                e.underlying = o.underlyingPolyline;
                e.srcArrowStyle = o.srcArrowhead;
                e.tarArrowStyle = o.tarArrowhead;
                e.invalidate();
            }
        }
    }






    [Serializable]
    public class NodeTransData
    {

        public NodeTransData(uint id, Node node)
        {
            LabelText = node.Label;
            //node attr               
            //var att = node.Attr;
            //Color = att.Color;
            //FillColor = att.FillColor;
            tempid = id;
            Shape = node.shape;
            Center = node.Center;

        }

        public string LabelText;
        public uint tempid;
        public NodeShape Shape;
        //public Color Color;
        //public Color FillColor;
        public Point Center;
    }
    [Serializable]
    public class EdgeTransData
    {
        public EdgeTransData(uint srcId, uint tarId, Edge e)
        {
            SrcTempId = srcId;
            TarTempId = tarId;
            //LabelText = e.label;
            //FirstStyle = e.Attr.FirstStyle;
            //Color = e.Attr.Color;
            srcArrowhead = e.srcArrowStyle;
            tarArrowhead = e.tarArrowStyle;
            geoEdgeCurve = e.lineCurve;
            underlyingPolyline = e.underlying;
            //definedDrawDelegateName = e.DefinedDrawDelegateName;
        }

        //public string LabelText;
        public uint SrcTempId;
        public uint TarTempId;
        //public DStyle FirstStyle;
        public ArrowStyle srcArrowhead;
        public ArrowStyle tarArrowhead;
        public ICurve geoEdgeCurve;
        public SmoothedPolyline underlyingPolyline;

        //public Microsoft.Msagl.Drawing.Color Color;
        //public string definedDrawDelegateName;
        //public Microsoft.Msagl.Drawing.Color FillColor;
    }
    enum Operation_Tools
    {
        Select,
        AddNode,
        AddEdge,
        Apply
    }
    public class Enum_List<TEnum>
    {
        public class Enum_Record
        {
            public string Enum_str { get; set; }
            public TEnum Enum_value { get; set; }
        }
        public List<Enum_Record> List { get; set; }
        public Enum_List()
        {
            var _list = new List<Enum_Record>();
            var es = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            foreach (var e in es)
            {
                var v = new Enum_Record()
                {
                    Enum_str = Enum.GetName(typeof(TEnum), e),
                    Enum_value = e
                };
                _list.Add(v);
            }
            this.List = _list;
        }
    }
    public delegate void VoidDelegate();

    public partial class MainWindow : Window
    {
        public struct DecorateFE
        {   
            public System.Windows.Shapes.Ellipse redCapturePoint;
            public System.Windows.Shapes.Line addEdge_traceLine;
        }
        public DecorateFE _decorateFE;
        public MainWindow()
        {
            InitializeComponent();
            loadDataToControls();
        }

        private void loadDataToControls()
        {

            #region shapeStyle_cmb initial data
            {                
                shapeStyle_cmb.ItemsSource = new Enum_List<NodeShape>().List;

                //_iset_ShapeStyle = true;
                //shapeStyle_cmb.SelectedIndex = 0;
                //_iset_ShapeStyle = false;
            }

            #endregion
        }

        //IRxMap ramp;
        private void Window_Loaded(object sender, RoutedEventArgs evt)
        {
            // ramp = new IRxMap();
            //this.Content = ramp;
            // mainGrid.Children.Add(ramp);

            //Common.addCordinate(ramp.MapCanvas);
            //// ramp.MapCanvas.RenderTransformOrigin = new Point(-100, -100);
            //var tr = ramp.MapCanvas.RenderTransform;
            //// tr.s = tr.Value.Translate(-100, -100);
            //MatrixTransform mt = new MatrixTransform();
            //Matrix mm = ramp.MapCanvas.RenderTransform.Value;
            //mm.Translate(Width / 2, Height / 2);
            //mt.Matrix = mm;
            //ramp.MapCanvas.RenderTransform = mt;

            var b1 = ramp.addBox("box", 10, 10);
            var b2 = ramp.addBox("box2", 30, 30);
            var n1 = ramp.addNode("aa", 0, -100);
            var n2 = ramp.addNode("bb", 100, -100);
            var n3 = ramp.addNode("cc", 20, 20);
            var n4 = ramp.addNode("dd", 30, 50);

            var eg = ramp.addEdge(n1, n2);
            //eg.tarArrowhead = new Microsoft.Msagl.Core.Layout.Arrowhead();
            eg.tarArrowStyle = ArrowStyle.Generalization;
            eg.srcArrowStyle = ArrowStyle.Circle;
            //ramp.update(new[] {(IRxObject) n1, n2, eg });

            n3.Parent = b1;
            n2.Parent = b1;
            n4.Parent = b2;
            b1.Parent = b2;

            n1.invalidate();
            n2.invalidate();
            n3.invalidate();
            n4.invalidate();

            b1.invalidate();
            eg.invalidate();
            
            b2.invalidate();



            //MapPointTranslateToScreenPoint(new Point(0, 0), new Point(ramp.ActualWidth/2, ramp.ActualHeight/2));
            // setViewportWithRatio(new Rect(-200, -200, 400, 400));
            //var gg = ramp.Parent as FrameworkElement;
            //ramp.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //MapPointTranslateToScreenPoint(new Point(0, 0), new Point(gg.ActualWidth/2, gg.ActualHeight/2));
            ramp.KeyDown += Ramp_KeyDown;
            ramp.MouseWheel += IRxmap_MouseWheel;
            ramp.MouseDown += IRxmap_MouseDown;
            ramp.MouseMove += IRxmap_MouseMove;
            ramp.MouseUp += IRxmap_MouseUp;
            ramp.SizeChanged += SizeChanged_xx;

            SelectBtn_CheckChanged(this, null);
            selectBtn.IsChecked = true;

            ramp.Focusable = true;
            ramp.MouseDown += (s, e) => ramp.Focus();
            this.MarkingObjects += MainWindow_ramp_MarkingObjects;
            _currentActiveMap = ramp;

            nodeAttrGroup.Visibility = Visibility.Hidden;
            edgeEditor.Visibility = Visibility.Hidden;
        }

        private void MainWindow_ramp_MarkingObjects(object sender, EventArgs e)
        {
            if(ramp.MarkedObjects.Count == 1)
            {
                var node = ramp.MarkedObjects.First() as Node;
                if(node != null)
                {
                    label_box.Text = node.Label;
                    nodeAttrGroup.Visibility = Visibility.Visible;
                    shapeStyle_cmb.SelectedIndex = (int)node.shape;
                }else
                {
                    var edge = ramp.MarkedObjects.First() as Edge;
                    if (edge == null)
                        return;
                    edgeEditor.Visibility = Visibility.Visible;

                    //ArrowStyle _srcArrowStyle;
                    //ArrowStyle _tarArrowStyle;
                    //LineStyle _lineStyle;
                    //string _decSymbol;
                    edgeEditor.SrcArrowStyle = edge.srcArrowStyle;
                    //edgeEditor.LineStyle = edge.lin
                    edgeEditor.TarArrowStyle = edge.tarArrowStyle;
                }
            }else// if(ramp.MarkedObjects.Count == 0)
            {
                nodeAttrGroup.Visibility = Visibility.Hidden;
                edgeEditor.Visibility = Visibility.Hidden;
                if (label_box.Text != "")
                    label_box.Text = "";
            }
        }
        public static IRxMap ActiveMap()
        {
            return _currentActiveMap;
        }
        void SizeChanged_xx(object sender, SizeChangedEventArgs e)
        {
            setViewportWithRatio(new Rect(-200, -200, 400, 400));
            ramp.SizeChanged -= SizeChanged_xx;
        }
        Operation_Tools _curTool = Operation_Tools.Select;
        Point _mouseDownPosL = new Point();
        bool _pressingL = false;
        Point _mouseDownPosR = new Point();
        bool _pressingR = false;
        bool _objectDragging = false;
        bool _startMouseMove = false;
        bool _edgeCtrlPointDrag = false;
        Site _edgeDraggingSite = null;
        Vector _offsetVector = new Vector();
        Dictionary<NodeBase, Point> _draggingNodeDict = new Dictionary<NodeBase, Point>();
        Dictionary<Edge, SmoothedPolyline> _draggingInOutEdgesDict = new Dictionary<Edge, SmoothedPolyline>();
        Dictionary<Edge, SmoothedPolyline> _draggingInEdgesDict = new Dictionary<Edge, SmoothedPolyline>();
        Dictionary<Edge, SmoothedPolyline> _draggingOutEdgesDict = new Dictionary<Edge, SmoothedPolyline>();
        static IRxMap _currentActiveMap;
        Xceed.Wpf.AvalonDock.Layout.LayoutAnchorable edge_defApplyAttrPanel;

        IRxObject _mouseDownTouchedObject = null;
        Node _mouseDownTouchedNode = null;
        private void IRxmap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_curTool == Operation_Tools.Select)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    _mouseDownPosL = e.GetPosition(ramp.MapCanvas);
                    _pressingL = true;
                    _mouseDownTouchedObject = ramp.getPointObject(e.GetPosition(ramp.MapCanvas));
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    _mouseDownPosR = e.GetPosition(ramp.MapCanvas);
                    _pressingR = true;
                }

                ramp.CaptureMouse();
            } else if (_curTool == Operation_Tools.AddNode)
            {
                var p = e.GetPosition(ramp.MapCanvas);
                var n = ramp.addNode("", p.X, p.Y);
                n.invalidate();
            } else if (_curTool == Operation_Tools.AddEdge)
            {
                _mouseDownPosL = e.GetPosition(ramp.MapCanvas);
                _pressingL = true;
                _mouseDownTouchedNode = ramp.getPointObject(e.GetPosition(ramp.MapCanvas)) as Node;
                if (ramp.DecoratorCanvas.Children.Contains(_decorateFE.redCapturePoint))
                    ramp.DecoratorCanvas.Children.Remove(_decorateFE.redCapturePoint);
                ramp.CaptureMouse();
            }
            else if(_curTool == Operation_Tools.Apply)
            {
                var edge = ramp.getPointObject(e.GetPosition(ramp.MapCanvas)) as Edge;
                if (edge != null)
                {
                    if(edge_defApplyAttrPanel != null)
                    {
                        EdgeAttrApplyEditor applyEditor = edge_defApplyAttrPanel.Content as EdgeAttrApplyEditor;
                        var ed = applyEditor.edgeEditor;
                        if (applyEditor.srcArrowCheck.IsChecked.Value)
                            edge.srcArrowStyle = ed.SrcArrowStyle;
                        if (applyEditor.tarArrowCheck.IsChecked.Value)
                            edge.tarArrowStyle = ed.TarArrowStyle;
                        edge.invalidate();
                    }
                }
            }
            _startMouseMove = true;

        }
        private void IRxmap_MouseMove(object sender, MouseEventArgs evt)
        {
            if (_curTool == Operation_Tools.Select && _pressingL)
            {
                if (_mouseDownTouchedObject == null)
                    return;
                if (_mouseDownTouchedObject.IRxType() == IRxObjectType.Node)
                {
                    Point pos = evt.GetPosition(ramp.MapCanvas);
                    processDragNode(pos);
                    return;
                } else if (_mouseDownTouchedObject.IRxType() == IRxObjectType.Edge)
                {
                    Point pos = evt.GetPosition(ramp.MapCanvas);
                    processDragEdge(pos);
                    return;
                }else if(_mouseDownTouchedObject.IRxType() == IRxObjectType.NodeBox)
                {
                    Point pos = evt.GetPosition(ramp.MapCanvas);
                    processDragNodeBox(pos);
                }
            } else if (_curTool == Operation_Tools.AddEdge)
            {
                if (_pressingL)
                {
                    if (_mouseDownTouchedNode != null)
                    {
                        if(_decorateFE.addEdge_traceLine == null)
                        {
                            _decorateFE.addEdge_traceLine = new System.Windows.Shapes.Line();
                            _decorateFE.addEdge_traceLine.StrokeThickness = 2;
                            _decorateFE.addEdge_traceLine.Stroke = Brushes.Black;
                        }
                        var map_p1 = _mouseDownTouchedNode.Center;
                        var p1 = ramp.MapCanvas.TranslatePoint(map_p1, ramp.DecoratorCanvas);
                        _decorateFE.addEdge_traceLine.X1 = p1.X;
                        _decorateFE.addEdge_traceLine.Y1 = p1.Y;
                        var p2 = evt.GetPosition(ramp.DecoratorCanvas);
                        _decorateFE.addEdge_traceLine.X2 = p2.X;
                        _decorateFE.addEdge_traceLine.Y2 = p2.Y;
                        if (!ramp.DecoratorCanvas.Children.Contains(_decorateFE.addEdge_traceLine))
                            ramp.DecoratorCanvas.Children.Add(_decorateFE.addEdge_traceLine);
                        drawDecorate_capturePoint(evt);
                    }
                }
                else
                {
                    drawDecorate_capturePoint(evt);
                }

            }
        }

        private void IRxmap_MouseUp(object sender, MouseButtonEventArgs evt)
        {
            if (_curTool == Operation_Tools.Select && (_pressingL || _pressingR))
            {
                OnSelectToolMouseUp(evt);
                if (evt.LeftButton == MouseButtonState.Released && evt.RightButton == MouseButtonState.Released)
                    ramp.ReleaseMouseCapture();
            } else if (_curTool == Operation_Tools.AddEdge)
            {

                if (_mouseDownTouchedNode != null)
                {
                    var _mouseUpTouchedNode = ramp.getPointObject(evt.GetPosition(ramp.MapCanvas)) as Node;
                    if (_mouseUpTouchedNode != null)
                    {
                        ramp.addEdge(_mouseDownTouchedNode, _mouseUpTouchedNode).invalidate();
                    }
                    if (ramp.DecoratorCanvas.Children.Contains(_decorateFE.addEdge_traceLine))
                    {
                        ramp.DecoratorCanvas.Children.Remove(_decorateFE.addEdge_traceLine);

                    }
                        
                    if (ramp.DecoratorCanvas.Children.Contains(_decorateFE.redCapturePoint))
                        ramp.DecoratorCanvas.Children.Remove(_decorateFE.redCapturePoint);
                }
                _mouseDownTouchedNode = null;
            }
            _pressingL = false;
            _startMouseMove = false;
            ///※※※  ReleaseMouseCapture() 会导致内部调用其它事件处理程序，故需要放到最后
            if (evt.LeftButton == MouseButtonState.Released && evt.RightButton == MouseButtonState.Released)
                ramp.ReleaseMouseCapture();

        }

        void drawDecorate_capturePoint(MouseEventArgs evt)
        {
            var map_pos = evt.GetPosition(ramp.MapCanvas);
            var node = ramp.getPointObject(map_pos) as Node;
            if (node == null)
            {
                if (ramp.DecoratorCanvas.Children.Contains(_decorateFE.redCapturePoint))
                    ramp.DecoratorCanvas.Children.Remove(_decorateFE.redCapturePoint);
            }
            else
            {

                //添加捕捉点
                if (_decorateFE.redCapturePoint == null)
                {
                    _decorateFE.redCapturePoint = new System.Windows.Shapes.Ellipse();
                    _decorateFE.redCapturePoint.Fill = Brushes.Red;
                    _decorateFE.redCapturePoint.Width = 10;
                    _decorateFE.redCapturePoint.Height = 10;
                }
                var capP = _decorateFE.redCapturePoint;
                Common.setFE_center(capP, ramp.MapCanvas.TranslatePoint(map_pos, ramp.DecoratorCanvas));
                if (!ramp.DecoratorCanvas.Children.Contains(_decorateFE.redCapturePoint))
                    ramp.DecoratorCanvas.Children.Add(capP);
            }
        }


        private void OnSelectToolMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!_objectDragging)
                {
                    const double enoughDistance = 2.0;
                    var pos = e.GetPosition(ramp.MapCanvas);
                    bool nodeClick = false;
                    if (Math.Abs(pos.X - _mouseDownPosL.X) < enoughDistance && Math.Abs(pos.Y - _mouseDownPosL.Y) < enoughDistance)
                        nodeClick = true;

                    if (nodeClick)
                    {
                        if (ramp.MarkedObjects.Count > 0)
                        {
                            demarkAllObjects();
                        }

                        var obj = ramp.getPointObject(e.GetPosition(ramp.MapCanvas));
                        if (obj != null)
                        {
                            obj.Marked = true;
                            ramp.MarkedObjects.Add(obj);
                            ramp.ChangingMarkedObjectStatus(obj);
                        }
                    }
                    else
                    {
                        demarkAllObjects();
                        SelectAndMark_Node_ByRect(new Rect(_mouseDownPosL, pos));
                        //if (ramp.MarkedObjects.Count != 0)
                            
                    }
                    RaizeMarkingObjects();

                }

                _pressingL = false;
                _objectDragging = false;
                _draggingNodeDict.Clear();
                _draggingInEdgesDict.Clear();
                _draggingInOutEdgesDict.Clear();
                _draggingOutEdgesDict.Clear();
                _edgeCtrlPointDrag = false;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                //popup context menu
                if (ramp.MarkedObjects.Count == 0)
                {

                }
                else
                {
                    Edge edge = ramp.MarkedObjects.First() as Edge;
                    if (edge != null)
                    {
                        ProcessRightClickOnSelectedEdge(new GPoint(_mouseDownPosR.X, _mouseDownPosR.Y), edge);
                    }
                    else
                    {
                        foreach (Node n in ramp.MarkedObjects)
                        {

                        }
                    }

                }

                //PopupMenus(params Tuple<string, VoidDelegate>[] menuItems);
                _pressingR = false;
            }

        }

        private void RaizeMarkingObjects()
        {
            MarkingObjects?.Invoke(this, null);
        }

        void processDragNode(Point curPos)
        {
            //var node = _mouseDownTouchedObject as Node;
            var pos = curPos;
            if (!_objectDragging)
            {
                const double enoughDistance = 2.0;
                //Func<double,double> sq = x => x * x;
                //if( sq(pos.X -_mouseDownPos.X) + sq(pos.Y- _mouseDownPos.Y) > sq(  enoughDistance* enoughDistance))
                if (Math.Abs(pos.X - _mouseDownPosL.X) >= enoughDistance || Math.Abs(pos.Y - _mouseDownPosL.Y) >= enoughDistance)
                {
                    _objectDragging = true;
                    //_offsetVector = _mouseDownPos - node.center;
                    if (!_mouseDownTouchedObject.Marked)
                    {
                        var node = _mouseDownTouchedObject as Node;
                        demarkAllObjects();
                        setObjectMark(node, true);
                        //_draggingNodeDict.Add(node, node.center);
                    }
                    foreach (Node i in ramp.MarkedObjects)
                    {
                        _draggingNodeDict.Add(i, i.Center);
                        Action<Dictionary<Edge, SmoothedPolyline>, Edge> onceAdd =
                                (aa, bb) => { if (!aa.ContainsKey(bb)) aa.Add(bb, bb.underlying.Clone()); };
                        foreach (var x in i.OutEdges)
                        {
                            if (x.tarNode.Marked)
                                onceAdd(_draggingInOutEdgesDict, x);
                            //_draggingInOutEdgesDict.Add(x,x.underlying.Clone());
                            else
                                onceAdd(_draggingOutEdgesDict, x);
                            //_draggingOutEdgesDict.Add(x,x.underlying.Clone());
                        }
                        foreach (var x in i.InEdges)
                        {
                            if (x.srcNode.Marked)
                                onceAdd(_draggingInOutEdgesDict, x);
                            //_draggingInOutEdgesDict.Add(x,x.underlying.Clone());
                            else
                                onceAdd(_draggingInEdgesDict, x);
                            //_draggingInEdgesDict.Add(x,x.underlying.Clone());
                        }

                    }


                }

            }
            if (_objectDragging)
            {
                //double deltaX = pos.X - _mouseDownPos.X;
                //double deltaY = pos.Y - _mouseDownPos.Y;
                //Vector deltaV = pos - _mouseDownPos;
                //node.center = pos - _offsetVector;
                //node.invalidate();
                Vector delta = pos - _mouseDownPosL;
                HashSet<NodeBox> needUpdateBoxes = new HashSet<NodeBox>() ;
                foreach (var x in _draggingNodeDict)
                {
                    x.Key.Center = x.Value + delta;
                    x.Key.invalidate();
                    if (x.Key.Parent != null)
                        needUpdateBoxes.Add(x.Key.Parent);
                }
                HashSet<NodeBox> boxParentBoxes = new HashSet<NodeBox>();
                foreach(var box in needUpdateBoxes)
                {
                    var b = box;
                    while(b.Parent!= null)
                    {
                        b = b.Parent;
                        boxParentBoxes.Add(b);                        
                    }
                }
                needUpdateBoxes.UnionWith(boxParentBoxes);
                foreach(var box in needUpdateBoxes)
                {
                    box.invalidate();
                }                
                foreach (var x in _draggingInOutEdgesDict)
                {
                    x.Key.underlying = SmoothedPolylineHelper.SmoothedPolylineTranslate(x.Value, delta);
                    x.Key.invalidate();
                }
                foreach (var x in _draggingInEdgesDict)
                {
                    Edge edge = x.Key;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        edge.underlying = CreateStraightUnderlyingPolyline(edge.srcNode, edge.tarNode);//   Common.SmoothedPolylineTransform( x.Value,delta);
                    else
                    {
                        Func<Vector, GPoint> f = v => new GPoint(v.X, v.Y);
                        var opos = edge.srcNode.Center;
                        var old_vec = _draggingNodeDict[edge.tarNode] - opos;
                        var new_vec = edge.tarNode.Center - opos;
                        var scale = new_vec.Length / old_vec.Length;
                        var rotate = (GPoint.Angle(f(old_vec), f(new_vec))) * 180 / Math.PI;
                        Matrix mat = new Matrix();
                        mat.RotateAt(rotate, opos.X, opos.Y);
                        mat.ScaleAt(scale, scale, opos.X, opos.Y);
                        edge.underlying = SmoothedPolylineHelper.SmoothedPolylineTransform(x.Value, mat);
                    }
                    edge.invalidate();
                }
                foreach (var x in _draggingOutEdgesDict)
                {
                    Edge edge = x.Key;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        edge.underlying = CreateStraightUnderlyingPolyline(edge.srcNode, edge.tarNode); //Common.SmoothedPolylineTransform(x.Value, delta);
                    else
                    {
                        Func<Vector, GPoint> f = v => new GPoint(v.X, v.Y);
                        var opos = edge.tarNode.Center;
                        var old_vec = _draggingNodeDict[edge.srcNode] - opos;
                        var new_vec = edge.srcNode.Center - opos;
                        var scale = new_vec.Length / old_vec.Length;
                        var rotate = (GPoint.Angle(f(old_vec), f(new_vec))) * 180 / Math.PI;
                        Matrix mat = new Matrix();
                        mat.RotateAt(rotate, opos.X, opos.Y);
                        mat.ScaleAt(scale, scale, opos.X, opos.Y);
                        edge.underlying = SmoothedPolylineHelper.SmoothedPolylineTransform(x.Value, mat);
                    }
                    edge.invalidate();
                }
            }
        }
        private void processDragEdge(Point pos)
        {
            if (_startMouseMove == true)
            {
                var edge = _mouseDownTouchedObject as Edge;
                var cornerInfo = SmoothedPolylineHelper.AnalyzeInsertOrDeletePolylineCorner(edge.underlying, Common.AglPoint(pos), edge.lineStrokeThickness + 1.0);
                if (cornerInfo == null)
                {
                    _edgeCtrlPointDrag = false;
                    return;
                }

                if (cornerInfo.Item2 == PolylineCornerType.CornerToDelete)
                {
                    _edgeCtrlPointDrag = true;
                    _edgeDraggingSite = cornerInfo.Item1;
                }
                _startMouseMove = false;
            }
            if (_edgeCtrlPointDrag == false)
                return;

            Site s = _edgeDraggingSite;
            s.Point = Common.AglPoint(pos);
            (_mouseDownTouchedObject as Edge).invalidate();

        }
        void processDragNodeBox(Point curPos)
        {
            var pos = curPos;
            if (!_objectDragging)
            {   ///判断是否开始拖动
                const double enoughDistance = 2.0;
                if (Math.Abs(pos.X - _mouseDownPosL.X) >= enoughDistance || Math.Abs(pos.Y - _mouseDownPosL.Y) >= enoughDistance)
                {
                    _objectDragging = true;
                    var box = _mouseDownTouchedObject as NodeBox;
                    if (!box.Marked)
                    {                        
                        demarkAllObjects();
                        setObjectMark(box, true);
                    }

                    foreach (NodeBase i in box.ChildNodes)
                    {
                        _draggingNodeDict.Add(i, i.Center);
                        Action<Dictionary<Edge, SmoothedPolyline>, Edge> onceAdd =
                                (aa, bb) => { if (!aa.ContainsKey(bb)) aa.Add(bb, bb.underlying.Clone()); };
                        foreach (var x in i.OutEdges)
                        {
                            if (x.tarNode.Marked)
                                onceAdd(_draggingInOutEdgesDict, x);
                            else
                                onceAdd(_draggingOutEdgesDict, x);
                        }
                        foreach (var x in i.InEdges)
                        {
                            if (x.srcNode.Marked)
                                onceAdd(_draggingInOutEdgesDict, x);
                            else
                                onceAdd(_draggingInEdgesDict, x);
                        }

                    }


                }

            }
            if (_objectDragging)
            {
                Vector delta = pos - _mouseDownPosL;
                foreach (var x in _draggingNodeDict)
                {
                    x.Key.Center = x.Value + delta;
                    x.Key.invalidate();
                }
                foreach (var x in _draggingInOutEdgesDict)
                {
                    x.Key.underlying = SmoothedPolylineHelper.SmoothedPolylineTranslate(x.Value, delta);
                    x.Key.invalidate();
                }
                foreach (var x in _draggingInEdgesDict)
                {
                    Edge edge = x.Key;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        edge.underlying = CreateStraightUnderlyingPolyline(edge.srcNode, edge.tarNode);//   Common.SmoothedPolylineTransform( x.Value,delta);
                    else
                    {
                        Func<Vector, GPoint> f = v => new GPoint(v.X, v.Y);
                        var opos = edge.srcNode.Center;
                        var old_vec = _draggingNodeDict[edge.tarNode] - opos;
                        var new_vec = edge.tarNode.Center - opos;
                        var scale = new_vec.Length / old_vec.Length;
                        var rotate = (GPoint.Angle(f(old_vec), f(new_vec))) * 180 / Math.PI;
                        Matrix mat = new Matrix();
                        mat.RotateAt(rotate, opos.X, opos.Y);
                        mat.ScaleAt(scale, scale, opos.X, opos.Y);
                        edge.underlying = SmoothedPolylineHelper.SmoothedPolylineTransform(x.Value, mat);
                    }
                    edge.invalidate();
                }
                foreach (var x in _draggingOutEdgesDict)
                {
                    Edge edge = x.Key;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        edge.underlying = CreateStraightUnderlyingPolyline(edge.srcNode, edge.tarNode); //Common.SmoothedPolylineTransform(x.Value, delta);
                    else
                    {
                        Func<Vector, GPoint> f = v => new GPoint(v.X, v.Y);
                        var opos = edge.tarNode.Center;
                        var old_vec = _draggingNodeDict[edge.srcNode] - opos;
                        var new_vec = edge.srcNode.Center - opos;
                        var scale = new_vec.Length / old_vec.Length;
                        var rotate = (GPoint.Angle(f(old_vec), f(new_vec))) * 180 / Math.PI;
                        Matrix mat = new Matrix();
                        mat.RotateAt(rotate, opos.X, opos.Y);
                        mat.ScaleAt(scale, scale, opos.X, opos.Y);
                        edge.underlying = SmoothedPolylineHelper.SmoothedPolylineTransform(x.Value, mat);
                    }
                    edge.invalidate();
                }
                (_mouseDownTouchedObject as NodeBox).invalidate();
            }
        }
        void updateEdgeWhenPartNodeModified(Edge edge)
        {

        }
        static SmoothedPolyline CreateStraightUnderlyingPolyline(Node src, Node tar)
        {
            Func<Point, GPoint> g = p => new GPoint(p.X, p.Y);
            SmoothedPolyline sm = SmoothedPolyline.FromPoints(new[] { g(src.Center), g(tar.Center) });
            return sm;
        }
        static SmoothedPolyline CreateStraightUnderlyingPolyline(Edge edge)
        {
            return CreateStraightUnderlyingPolyline(edge.srcNode, edge.tarNode);
        }
        static void DragEdgeAsStraightLine(Point delta, Edge edge)
        {
            Common.CreateSimpleEdgeCurveWithUnderlyingPolyline(edge);
        }

        void ProcessRightClickOnSelectedEdge(GPoint mouseRightButtonDownPoint, Edge edge)
        {
            //mouseRightButtonDownPoint = viewer.ScreenToSource(e);

            var cornerInfo = SmoothedPolylineHelper.AnalyzeInsertOrDeletePolylineCorner(edge.underlying, mouseRightButtonDownPoint, edge.lineStrokeThickness + 0.02);

            if (cornerInfo == null)
                return;

            var edgeRemoveCouple = new Tuple<string, VoidDelegate>("Remove edge",
                                                                    () => ramp.removeEdge(edge));
            var edgeModifyToStraightLine = new Tuple<string, VoidDelegate>("become to straight line",
                                                () => { edge.underlying = CreateStraightUnderlyingPolyline(edge); edge.invalidate(); });

            if (cornerInfo.Item2 == PolylineCornerType.PreviousCornerForInsertion)
                PopupMenus(
                    new Tuple<string, VoidDelegate>("Insert polyline corner",
                        () => { SmoothedPolylineHelper.InsertSite(cornerInfo.Item1, Common.AglPoint(_mouseDownPosR)); edge.invalidate(); }),
                    edgeModifyToStraightLine, edgeRemoveCouple);
            else if (cornerInfo.Item2 == PolylineCornerType.CornerToDelete)
                PopupMenus(
                    new Tuple<string, VoidDelegate>("Delete polyline corner",
                                                              () => { SmoothedPolylineHelper.DeleteSite(cornerInfo.Item1); edge.invalidate(); }),
                    edgeModifyToStraightLine, edgeRemoveCouple);
        }
        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            contextMenu.Closed += (s, e) => { ContextMenuService.SetContextMenu(ramp, null); };
            ContextMenuService.SetContextMenu(ramp, contextMenu);

        }
        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate)
        {
            var menuItem = new MenuItem { Header = title };
            menuItem.Click += (RoutedEventHandler)(delegate { voidVoidDelegate(); });
            return menuItem;
        }
        void demarkAllObjects()
        {
            foreach (var o in ramp.MarkedObjects)
            {
                o.Marked = false;
                ramp.ChangingMarkedObjectStatus(o);
            }
            ramp.MarkedObjects.Clear();
        }
        void SelectAndMark_Node_ByRect(Rect r)
        {
            var rect =
                new Microsoft.Msagl.Core.Geometry.Rectangle(r.Left, r.Top, r.Right, r.Bottom);
            foreach (var obj in ramp.objDict)
            {
                var node = obj.Value as Node;
                if (node != null)
                {
                    if (rect.Intersects(node.borderCurve.BoundingBox))
                        setObjectMark(node, true);
                }
            }
        }
        void setObjectMark(IRxObject obj, bool mark)
        {
            if (mark)
            {
                obj.Marked = true;
                ramp.MarkedObjects.Add(obj);
                ramp.ChangingMarkedObjectStatus(obj);
            }
            else
            {
                obj.Marked = false;
                ramp.MarkedObjects.Remove(obj);
                ramp.ChangingMarkedObjectStatus(obj);
            }
        }
        private void Ramp_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                if (ramp.MarkedObjects.Count == 0)
                    return;
                if(ramp.MarkedObjects.First().IRxType() == IRxObjectType.Edge)
                {
                    ramp.removeEdge(ramp.MarkedObjects.First() as Edge);
                }
                else
                {
                    foreach(Node n in ramp.MarkedObjects)
                    {
                        ramp.removeNode(n);
                    }
                }

            }
            else if (e.Key == Key.T)
            {
                var pos = new Point(100, 00);
                var left = (double)ramp.MapCanvas.GetValue(Canvas.LeftProperty);
                var top = (double)ramp.MapCanvas.GetValue(Canvas.TopProperty);
                var np = ramp.MapCanvas.PointToScreen(pos);

                NativeMethods.SetCursorPos((int)(np.X), (int)(np.Y));
            }
            else if (e.Key == Key.V)
            {
                MessageBox.Show($"{ramp.ActualWidth} x {ramp.ActualHeight}");
            }else if(e.Key == Key.S)
            {
                ramp.Focus();
            }else if(e.Key == Key.Q)
            {
                Debug.Print( ramp.DecoratorCanvas.Children.Count.ToString());
            }
        }

        double mapScale = 1;
        public void IRxmap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const int singleDelta = 120;
            int v = e.Delta / singleDelta;
            var curScale = v > 0 ? (v * 1.25) : (-v * 0.8);
            if (v < 0 && curScale * mapScale < 0.05)
                return;
            mapScale = mapScale * curScale;
            var epos = e.GetPosition(ramp.MapCanvas);
            ScaleOnPoint(mapScale, epos);

        }
        public void ScaleOnPoint(double scale, Point pos)
        {
            //var tpos = ramp.TranslatePoint(epos, ramp.MapCanvas);
            ///Debug.Print($"p.x:{pos.X},p.y:{pos.Y}");
            //ramp.viewScaleOnPoint(mapScale, epos);// new Point(0,100)); // new Point(0,0));
            var centerOfZoomOnScreen =
                ramp.MapCanvas.TransformToAncestor((FrameworkElement)ramp).Transform(pos);

            //SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X * scale,
            //             centerOfZoomOnScreen.Y + centerOfZoom.Y * scale);
            ramp.viewTransform(mapScale, centerOfZoomOnScreen.X - pos.X * mapScale,
                    centerOfZoomOnScreen.Y - pos.Y * mapScale);
        }

        void MapPointTranslateToScreenPoint(Point mapPoint, Point screenPoint)
        {
            //var scale = CurrentScale;
            ramp.viewTransform(mapScale, screenPoint.X - mapScale * mapPoint.X, screenPoint.Y + mapScale * mapPoint.Y);
        }

        void setViewportWithRatio(Rect r)
        {
            double scaleW = ramp.ActualWidth / r.Width;
            double scaleH = ramp.ActualHeight / r.Height;
            double scale = Math.Min(scaleW, scaleH);
            mapScale = scale;
            ramp.viewTransform(mapScale, ramp.ActualWidth / 2 - (r.Left + r.Right) / 2, ramp.ActualHeight / 2 - (r.Top + r.Bottom) / 2);

        }

        private void MenuItem_attr_Click(object sender, RoutedEventArgs e)
        {
            AttrPane.IsVisible = true;
        }

        private void MenuItem_defAttr_Click(object sender, RoutedEventArgs evt)
        {
            if (edge_defApplyAttrPanel == null)
            {
                var p = edge_defApplyAttrPanel = new Xceed.Wpf.AvalonDock.Layout.LayoutAnchorable();
                EdgeAttrApplyEditor e = new EdgeAttrApplyEditor();
                p.Content = e;
                p.Title = "默认属性";
                //p.Parent = leftAnchorPane;
                leftAnchorPane.Children.Add(p);
                p.CanDockAsTabbedDocument = false;

            }
            if (!edge_defApplyAttrPanel.IsVisible)
            {
                edge_defApplyAttrPanel.IsVisible = true;
            }
        }

        private void MenuItem_doc_Click(object sender, RoutedEventArgs e)
        {

        }
        ToggleButton _currentPressedBtn;
        private string _fileName = "";
        public event EventHandler MarkingObjects;

        void pressingBtn(ToggleButton btn)
        {
            var preBtn = _currentPressedBtn;

            if (preBtn == null)
            {
                _currentPressedBtn = btn;
                return;
            }
            if (preBtn == btn)
            {
                var b = btn.IsChecked.Value;
                if (!b)
                    _currentPressedBtn = null;
                return;
            }
            preBtn.IsChecked = false;
            //btn.IsChecked = true;
            _currentPressedBtn = btn;
        }
        private void NodeBtn_CheckChanged(object sender, RoutedEventArgs e)
        {
            pressingBtn(nodeBtn);
            _curTool = Operation_Tools.AddNode;
        }

        private void EdgeBtn_CheckChanged(object sender, RoutedEventArgs e)
        {
            pressingBtn(edgeBtn);
            _curTool = Operation_Tools.AddEdge;
        }

        private void SelectBtn_CheckChanged(object sender, RoutedEventArgs e)
        {
            pressingBtn(selectBtn);
            _curTool = Operation_Tools.Select;
        }

        private void ApplyBtn_CheckChanged(object sender, RoutedEventArgs e)
        {
            pressingBtn(applyBtn);
            _curTool = Operation_Tools.Apply;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_fileName == "")
            {
                SaveAsBtn_Click(sender, e);
            }
            else
            {
                //make sure file path is valid
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(_fileName)))
                    return;
                ramp.save(_fileName);
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "BAGL Files(*.bagl)|*.bagl"
            };

            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                loadFile(openFileDialog.FileName);
            }
        }
        public void loadFile(string filename)
        {
            try
            {
                _fileName = filename;
                ramp.load(_fileName);
                this.Title = System.IO.Path.GetFileNameWithoutExtension(_fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void SaveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog { Filter = "BAGL Files(*.bagl)|*.bagl" };
            try
            {
                if (saveFileDialog.ShowDialog().GetValueOrDefault())
                {
                    _fileName = saveFileDialog.FileName;
                    ramp.save(_fileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            var objs = ramp.MarkedObjects;
            if (objs.Count == 0 || objs.First().IRxType() == IRxObjectType.Edge)
                return;

            var stm = new System.IO.MemoryStream();
            var nodeDataList = new List<NodeTransData>();
            var edgeDataList = new List<EdgeTransData>();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            foreach (Node n in objs)
            {
                var nt = new NodeTransData(n.Id(), n);
                nodeDataList.Add(nt);
                foreach (Edge edge in n.InEdges)
                {
                    if (objs.Contains(edge.srcNode))
                        edgeDataList.Add(new EdgeTransData(edge.srcNode.Id(), edge.tarNode.Id(), edge));
                }
            }
            formatter.Serialize(stm, nodeDataList);
            formatter.Serialize(stm, edgeDataList);
            stm.Flush();
            Clipboard.SetData("IRxmap.v0.1", stm);
        }

        private void PasteBtn_Click(object sender, RoutedEventArgs e)
        {
            var stm = (System.IO.MemoryStream)Clipboard.GetData("IRxmap.v0.1");
            if (stm == null)
                return;

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var nodeDataList = (List<NodeTransData>)formatter.Deserialize(stm);
            var edgeDataList = (List<EdgeTransData>)formatter.Deserialize(stm);
            var old2new_id_dict = new Dictionary<uint, uint>();
            foreach (NodeTransData o in nodeDataList)
            {
                var n = ramp.addNode(o.LabelText, o.Center.X, o.Center.Y);
                n.shape = o.Shape;
                old2new_id_dict[o.tempid] = n.objId;
                n.invalidate();
            }
            foreach (var o in edgeDataList)
            {
                var eg = ramp.addEdge(ramp.objDict[old2new_id_dict[o.SrcTempId]] as Node,
                                        ramp.objDict[old2new_id_dict[o.TarTempId]] as Node);
                eg.underlying = o.underlyingPolyline;
                eg.srcArrowStyle = o.srcArrowhead;
                eg.tarArrowStyle = o.tarArrowhead;
                eg.invalidate();
            }
        }


        private void Label_box_LostFocus(object sender, RoutedEventArgs e)
        {
            var objs = ramp.MarkedObjects;
            if (objs.Count == 0 || objs.First().IRxType() != IRxObjectType.Node)
                return;
            var node = objs.First() as Node;
            if (node.Label == null )
            {
                if (label_box.Text == "")
                    return;
            }else if(node.Label == label_box.Text)
            {
                return;
            }
            node.Label = label_box.Text;
            node.invalidate();
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ramp.Focus();
            }
            //e.Handled = true;
        }

        private void ShapeStyle_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var node = ramp.MarkedObjects.First() as Node;
            Debug.Assert(node != null);
            node.shape = (NodeShape)shapeStyle_cmb.SelectedIndex;
            node.invalidate();
        }
        private void EdgeEditor_SrcArrowChanged(object sender, EdgeAttrEditor.ValueChangedEventArgs<ArrowStyle> e)
        {
            var edge = ramp.MarkedObjects.First() as Edge;
            edge.srcArrowStyle = edgeEditor.SrcArrowStyle;
            edge.invalidate();
        }
        private void EdgeEditor_TarArrowChanged(object sender, EdgeAttrEditor.ValueChangedEventArgs<ArrowStyle> e)
        {
            var edge = ramp.MarkedObjects.First() as Edge;
            edge.tarArrowStyle = edgeEditor.TarArrowStyle;
            edge.invalidate();
        }

        private void EdgeEditor_LineStyleChanged(object sender, EdgeAttrEditor.ValueChangedEventArgs<LineStyle> e)
        {
        }

        private void EdgeEditor_DecoratorChanged(object sender, EdgeAttrEditor.ValueChangedEventArgs<string> e)
        {

        }

        private void MenuItem_originCenter_Click(object sender, RoutedEventArgs e)
        {
            MapPointTranslateToScreenPoint(new Point(0, 0), new Point(ramp.ActualWidth / 2, ramp.ActualHeight / 2));

        }

        private void MenuItem_entierMap_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Msagl.Core.Geometry.Rectangle entireBox = new Microsoft.Msagl.Core.Geometry.Rectangle();
            bool initbox = true;
            foreach( var o in ramp.objDict)
            {
                Microsoft.Msagl.Core.Geometry.Rectangle box;
                if (o.Value.IRxType() == IRxObjectType.Node)
                {
                    box = (o.Value as Node).borderCurve.BoundingBox;
                    if (initbox)
                    {
                        entireBox = box;
                        initbox = false;
                    } else
                        entireBox.Add(box);
                }
            }
            var mapRect = new Rect(entireBox.Left, entireBox.Bottom, entireBox.Width, entireBox.Height);
            var center = mapRect.TopLeft + (mapRect.BottomRight - mapRect.TopLeft) / 2;
            Matrix mat = new Matrix();
            mat.ScaleAt(1.2, 1.2, center.X, center.Y);
            mapRect.Transform(mat);
            ramp.viewportOnRect(mapRect);
            mapScale = ramp.MapCanvas.RenderTransform.Value.M11;
            //var scaleW = ramp.ActualWidth / entireBox.Width;
            //var scaleH = ramp.ActualHeight / entireBox.Height;
            //var scale = Math.Min(scaleW, scaleH);
            //var mapCenter = new Point((mapRect.Left + mapRect.Right) / 2, (mapRect.Top + mapRect.Bottom) / 2);
            //ramp.viewTransform(scale, ramp.ActualWidth / 2 - mapCenter.X * scale, ramp.ActualHeight / 2 - mapCenter.Y * scale);
            //MapPointTranslateToScreenPoint(centerPos, new Point(ramp.ActualWidth / 2, ramp.ActualHeight / 2));
            //setViewportWithRatio(new Rect(entireBox.Left, entireBox.Bottom, , entireBox.Height));
        }

        private void MenuItem_originScale_Click(object sender, RoutedEventArgs e)
        {
            //var rampCenter = new Point(ramp.ActualWidth / 2, ramp.ActualHeight / 2);
            //var mapPosOnRampCenter = ramp.TranslatePoint(rampCenter, ramp.MapCanvas);
            //ramp.viewTransform(1, mapPosOnRampCenter.X,- mapPosOnRampCenter.Y);
            //ScaleOnPoint(1, mapPosOnRampCenter);
        }
    }
}
