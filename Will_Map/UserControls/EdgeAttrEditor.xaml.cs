using System;
using System.Collections.Generic;
using System.ComponentModel;
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
//using Will_Map;
//using Microsoft.Msagl.Drawing;
//using DStyle = Microsoft.Msagl.Drawing.Style;
//using ArrowStyle = Will_Map.ArrowStyle;
//using Edge = Will_Map.Edge;
namespace Will_Map.UserControls {
    /// <summary>
    /// EdgeAttrEditor.xaml 的交互逻辑
    /// </summary>
    public partial class EdgeAttrEditor : UserControl, INotifyPropertyChanged {
        private class Enum_List<TEnum> {
            public class Enum_Record {
                public string Enum_str { get; set; }
                public TEnum Enum_value { get; set; }
            }
            public List<Enum_Record> List { get; set; }
            public Enum_List() {
                var _list = new List<Enum_Record>();
                var es = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
                foreach (var e in es) {
                    var v = new Enum_Record() {
                        Enum_str = Enum.GetName(typeof(TEnum), e),
                        Enum_value = e
                    };
                    _list.Add(v);
                }
                this.List = _list;
            }
        }

        //bool _iset_edgeDstArrow;
        //bool _iset_edgeDecrate;
        object _preValue;
 
        public class ValueChangedEventArgs<T> : EventArgs {
            public ValueChangedEventArgs() {

            }
            public ValueChangedEventArgs(T p, T c) {
                CurrentValue = c;
                PreviousValue = p;
            }
            //T _curVal, _preVal;
            T CurrentValue { get; set; }
            T PreviousValue { get; set; }
        }

        public event EventHandler<ValueChangedEventArgs<ArrowStyle>> SrcArrowChanged;
        public event EventHandler<ValueChangedEventArgs<ArrowStyle>> TarArrowChanged;
        public event EventHandler<ValueChangedEventArgs<LineStyle>> LineStyleChanged;
        public event EventHandler<ValueChangedEventArgs<string>> DecoratorChanged;
        public event PropertyChangedEventHandler PropertyChanged;



        public void OnPropertyChanged(string s) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }

        }

        ArrowStyle _srcArrowStyle;
        ArrowStyle _tarArrowStyle;
        LineStyle _lineStyle;
        string _decSymbol;
        public ArrowStyle SrcArrowStyle {
            get => _srcArrowStyle;
            set { _preValue = _srcArrowStyle;
                _srcArrowStyle = value;
                RaizeSrcArrowStyleChanged();
            }
        }        
        public ArrowStyle TarArrowStyle {
            get => _tarArrowStyle;
            set {
                _preValue = _tarArrowStyle;
                _tarArrowStyle = value;
                RaizeTarArrowStyleChanged();
            }
        }
        public LineStyle LineStyle
        {
            get => _lineStyle;
            set
            {
                _preValue = _lineStyle;
                _lineStyle = value;
                RaizeLineStyleChanged();
            }
        }
        public string DecorateSymbol {
            get { return _decSymbol; }
            set { _preValue = _decSymbol;
                _decSymbol= value;
                RaizeDecorateSymblChanged();
            }
        }

        //public ComboBox SrcArrowStyleCmb { get => srcArrowStyle_cmb;}
        //public ComboBox TarArrowStyleCmb { get => tarArrowStyle_cmb;}
        //public ComboBox LineStyleCmb { get => lineStyle_cmb;}
        //public ComboBox DecorateSymbolCmb { get => decSymbol_cmb;}
        int _globalCheckState = 0;
        public bool? GlobalCheck {
            get {
                switch (_globalCheckState) {
                    case 0: return null;
                    case 1: return true;
                    case 2: return false;
                    default:
                        return null;
                }
            }
            set {
                if (value == null)
                    _globalCheckState = 0;
                else {
                    if (value == true)
                        _globalCheckState = 1;
                    else _globalCheckState = 2;
                }
                OnPropertyChanged("GlobalCheck");
            }
        }
        #region Event trigger

        private void RaizeSrcArrowStyleChanged() {
            OnPropertyChanged("SrcArrowStyle");
            if (SrcArrowChanged != null) {
                var e = new ValueChangedEventArgs<ArrowStyle>((ArrowStyle)_preValue, _srcArrowStyle);
                SrcArrowChanged(srcArrowStyle_cmb,e);
            }
        }
        private void RaizeTarArrowStyleChanged() {
            OnPropertyChanged("TarArrowStyle");
            if(TarArrowChanged!= null) {
                var e = new ValueChangedEventArgs<ArrowStyle>((ArrowStyle)_preValue, _tarArrowStyle);
                TarArrowChanged(tarArrowStyle_cmb, e);
            }
        }
        private void RaizeLineStyleChanged() {
            OnPropertyChanged("LineStyle");
            if(LineStyleChanged!= null) {
                var e = new ValueChangedEventArgs<LineStyle>((LineStyle)_preValue, _lineStyle);
                LineStyleChanged(lineStyle_cmb, e);
            }
        }
        private void RaizeDecorateSymblChanged() {
            OnPropertyChanged("DecorateSymbol");
            if (DecoratorChanged != null) {
                var e = new ValueChangedEventArgs<string>((string)_preValue, _decSymbol);
                DecoratorChanged(decSymbol_cmb, e);
            }
        }

        #endregion  Event trigger



        public EdgeAttrEditor() {
            InitializeComponent();

            setupControlsData();
        }
        public void SetAttr(Edge ea) {
            _srcArrowStyle = ea.srcArrowStyle;
            _tarArrowStyle = ea.tarArrowStyle;
            //_lineStyle = ea.FirstStyle;
            //_decSymbol = ea.DefinedDrawDelegateName;
            OnPropertyChanged("");
            
        }

        private void setupControlsData() {

            this.DataContext = this;
            #region srcArrowStyle_cmb initial data
            srcArrowStyle_cmb.ItemsSource = new Enum_List<ArrowStyle>().List;
            //srcArrowStyle_cmb.DataContext = this;
            _srcArrowStyle = ArrowStyle.Diamond;

            #endregion

            #region lineStyle_cmb initial data
            {
                var el = new Enum_List<LineStyle>();
                var conCol = new[] { LineStyle.Dashed, LineStyle.Dotted, LineStyle.Solid };
                el.List = el.List.Where(x => conCol.Contains(x.Enum_value)).ToList();
                lineStyle_cmb.ItemsSource = el.List;
                //lineStyle_cmb.DataContext
                _lineStyle = LineStyle.Solid;

            }
            #endregion

            #region dstArrowStyle_cmb initial data
            tarArrowStyle_cmb.ItemsSource = new Enum_List<ArrowStyle>().List;
            _tarArrowStyle = ArrowStyle.NonSpecified;
            //dstArrowStyle_cmb.SelectedIndex = 0;

            #endregion

            #region decSymbol_cmb initial data
            decSymbol_cmb.ItemsSource = new List<string>() { "", "center_of" };
            _decSymbol = "center_of";
            //decSymbol_cmb.SelectedIndex = 0;
            #endregion


        }
        


        private void GlobalCheckbox_CheckChanged(object sender, RoutedEventArgs e) {

        }


    }
}
