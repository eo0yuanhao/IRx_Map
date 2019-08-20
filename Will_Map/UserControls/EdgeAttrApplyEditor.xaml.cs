using System;
using System.Collections.Generic;
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

namespace Will_Map.UserControls {
    /// <summary>
    /// EdgeAttrApplyEditor.xaml 的交互逻辑
    /// </summary>
    public partial class EdgeAttrApplyEditor : UserControl {
        CheckBox[] subCheckboxArray;
        public EdgeAttrApplyEditor() {
            InitializeComponent();
            take();
        }
        public ComboBox SrcArrowStyleCmb {
            get => edgeEditor.srcArrowStyle_cmb;
        }
        public ComboBox TarArrowStyleCmb {
            get => edgeEditor.tarArrowStyle_cmb;
        }
        public ComboBox LineStyleCmb {
            get => edgeEditor.lineStyle_cmb;
        }
        public ComboBox DecorateSymbolCmb {
            get => edgeEditor.decSymbol_cmb;
        }
        void take() {
            srcArrowCheck.Tag = edgeEditor.srcArrowStyle_cmb;
            tarArrowCheck.Tag = edgeEditor.tarArrowStyle_cmb;
            lineStyleCheck.Tag = edgeEditor.lineStyle_cmb;
            decSymbolCheck.Tag = edgeEditor.decSymbol_cmb;
            var checkbox = new[] { srcArrowCheck, lineStyleCheck, tarArrowCheck, decSymbolCheck };
            
            for (int i = 0; i < checkbox.Length; i++) {
                var c = checkbox[i];
                c.IsChecked = true;
                c.Checked += checkbox_do;
                c.Unchecked += checkbox_do;
                //c.Tag = corresboundingBuddys[i];

            }
            subCheckboxArray = checkbox;
        }
        void checkbox_do(object sender, RoutedEventArgs e) {
            CheckBox c = sender as CheckBox;
            var ele = c.Tag as UIElement;
            ele.IsEnabled = c.IsChecked.Value;
        }
        private void SelAllBtn_Click(object sender, RoutedEventArgs e) {
            foreach (var u in subCheckboxArray) {
                if (!u.IsChecked.Value)
                    u.IsChecked = true;
            }
        }
        private void UnselAllBtn_Click(object sender, RoutedEventArgs e) {
            foreach (var u in subCheckboxArray) {
                if (u.IsChecked.Value)
                    u.IsChecked = false;
            }
        }

        private void GetPartEdgeAttrBtn_Click(object sender, RoutedEventArgs e) {
            IRxMap amap = MainWindow.ActiveMap();
            if (amap.MarkedObjects.Count != 1)
                return;
            var edge = amap.MarkedObjects.First() as Edge;
            if (edge == null)
                return;

            //var att = vedge.Edge.Attr;
            if (srcArrowCheck.IsChecked.Value) {
                SrcArrowStyleCmb.SelectedValue = edge.srcArrowStyle;
            }
            if (tarArrowCheck.IsChecked.Value) {
                TarArrowStyleCmb.SelectedValue = edge.tarArrowStyle;
            }
            //if (lineStyleCheck.IsChecked.Value) {
            //    LineStyleCmb.SelectedValue = att.FirstStyle;
            //}
            //if (decSymbolCheck.IsChecked.Value) {
            //    DecorateSymbolCmb.SelectedValue = att.DefinedDrawDelegateName;
            //}
        }
    }
}
