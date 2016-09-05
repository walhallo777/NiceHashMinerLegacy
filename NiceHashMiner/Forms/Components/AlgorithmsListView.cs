﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using NiceHashMiner.Enums;
using NiceHashMiner.Configs;
using NiceHashMiner.Forms.Components;
using NiceHashMiner.Devices;
using NiceHashMiner.Miners;
using NiceHashMiner.Interfaces;

namespace NiceHashMiner.Forms.Components {
    public partial class AlgorithmsListView : UserControl {

        private const int ENABLED   = 0;
        private const int ALGORITHM = 1;
        private const int SPEED     = 2;
        private const int RATIO     = 3;
        private const int RATE      = 4;

        public interface IAlgorithmsListView {
            void SetCurrentlySelected(ListViewItem lvi, ComputeDevice computeDevice);
            void HandleCheck(ListViewItem lvi);
        }

        public IAlgorithmsListView ComunicationInterface { get; set; }

        ComputeDevice _computeDevice;

        private class DefaultAlgorithmColorSeter : IListItemCheckColorSetter {
            private static Color DISABLED_COLOR = Color.DarkGray;
            private static Color BENCHMARKED_COLOR = Color.LightGreen;
            private static Color UNBENCHMARKED_COLOR = Color.LightBlue;
            public void LviSetColor(ListViewItem lvi) {
                Algorithm algorithm = lvi.Tag as Algorithm;
                if (algorithm != null) {
                    if (algorithm.Skip && !algorithm.IsBenchmarkPending) {
                        lvi.BackColor = DISABLED_COLOR;
                    } else if (algorithm.BenchmarkSpeed > 0 && !algorithm.IsBenchmarkPending) {
                        lvi.BackColor = BENCHMARKED_COLOR;
                    } else {
                        lvi.BackColor = UNBENCHMARKED_COLOR;
                    }
                }
            }
        }

        IListItemCheckColorSetter _listItemCheckColorSetter = new DefaultAlgorithmColorSeter();

        // helper for benchmarking logic
        public bool IsInBenchmark { get; set; }

        public AlgorithmsListView() {
            InitializeComponent();
            // callback initializations
            listViewAlgorithms.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(listViewAlgorithms_ItemSelectionChanged);
            listViewAlgorithms.ItemChecked += new ItemCheckedEventHandler(listViewAlgorithms_ItemChecked);
            IsInBenchmark = false;
        }

        public void RemoveRatioRates() {
            listViewAlgorithms.Columns.RemoveAt(RATE);
            listViewAlgorithms.Columns.RemoveAt(RATIO);
        }

        public void SetAlgorithms(ComputeDevice computeDevice, bool isEnabled) {
            _computeDevice = computeDevice;
            var config = computeDevice.DeviceBenchmarkConfig;
            listViewAlgorithms.Items.Clear();
            foreach (var alg in config.AlgorithmSettings) {
                ListViewItem lvi = new ListViewItem();
                lvi.Checked = !alg.Value.Skip;
                //lvi.Text = alg.Value.NiceHashName;
                ListViewItem.ListViewSubItem sub = lvi.SubItems.Add(alg.Value.NiceHashName);

                //sub.Tag = alg.Value;
                lvi.SubItems.Add(alg.Value.BenchmarkSpeedString());
                lvi.Tag = alg.Value;
                _listItemCheckColorSetter.LviSetColor(lvi);
                listViewAlgorithms.Items.Add(lvi);
            }
            this.Enabled = isEnabled;
        }

        public void RepaintStatus(bool isEnabled, string uuid) {
            if (_computeDevice != null && _computeDevice.UUID == uuid) {
                foreach (ListViewItem lvi in listViewAlgorithms.Items) {
                    Algorithm algo = lvi.Tag as Algorithm;
                    lvi.SubItems[SPEED].Text = algo.BenchmarkSpeedString();
                    _listItemCheckColorSetter.LviSetColor(lvi);
                }
                this.Enabled = isEnabled;
            }
        }

        #region Callbacks Events
        private void listViewAlgorithms_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
            if (ComunicationInterface != null) {
                ComunicationInterface.SetCurrentlySelected(e.Item, _computeDevice);
            }
        }

        private void listViewAlgorithms_ItemChecked(object sender, ItemCheckedEventArgs e) {
            var algo = e.Item.Tag as Algorithm;
            if (algo != null) {
                algo.Skip = !e.Item.Checked;
            }
            if (ComunicationInterface != null) {
                ComunicationInterface.HandleCheck(e.Item);
            }
            var lvi = e.Item as ListViewItem;
            _listItemCheckColorSetter.LviSetColor(lvi);
        }
        #endregion //Callbacks Events

        // benchmark settings
        public void SetSpeedStatus(ComputeDevice computeDevice, AlgorithmType algorithmType, string status) {
            var algorithm = computeDevice.DeviceBenchmarkConfig.AlgorithmSettings[algorithmType];
            algorithm.BenchmarkStatus = status;

            // gui update only if same as selected
            if (_computeDevice != null && computeDevice.UUID == _computeDevice.UUID) {
                foreach (ListViewItem lvi in listViewAlgorithms.Items) {
                    Algorithm algo = lvi.Tag as Algorithm;
                    if (algo != null && algo.NiceHashID == algorithmType) {
                        // TODO handle numbers
                        lvi.SubItems[SPEED].Text = algorithm.BenchmarkSpeedString();
                        _listItemCheckColorSetter.LviSetColor(lvi);
                        break;
                    }
                }
            }
        }
    }
}