﻿using SynoDuplicateFolders.Data;
using SynoDuplicateFolders.Extensions;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Linq;
using SynoDuplicateFolders.Data.Core;

namespace SynoDuplicateFolders.Controls
{
    public enum vhcViewMode
    {
        Shares,
        Volume,
        VolumeTotals
    }
    public partial class VolumeHistoricChart : UserControl
    {
        private ISynoReportCache src = null;
        private ISynoChartData data = null;

        private vhcViewMode _view = vhcViewMode.VolumeTotals;
        private SynoReportType _viewtype = SynoReportType.VolumeUsage;
        private List<string> _displaying_traces = null;

        private IChartConfiguration _legends = null;
        private bool invalidated = false;
        private readonly List<string> unknown_traces = new List<string>();

        private FileSizeFormatSize shares_range = FileSizeFormatSize.PB;

        public VolumeHistoricChart()
        {
            InitializeComponent();
            chart1.Visible = false;
            chart1.FormatNumber += Chart1_FormatNumber;
        }

        private void Chart1_FormatNumber(object sender, FormatNumberEventArgs e)
        {
            if (sender is Axis)
            {
                Axis a = sender as Axis;
                if (a.AxisName == AxisName.Y)
                {
                    if (_view == vhcViewMode.Shares || _view == vhcViewMode.VolumeTotals)
                    {
                        e.LocalizedValue = Convert.ToInt64(e.Value).ToFileSizeString(shares_range);
                    }
                }
            }
        }

        public IChartConfiguration Configuration
        {
            get { return _legends; }
            set { _legends = value; }
        }

        public vhcViewMode View
        {
            get { return _view; }
            set
            {
                switch (value)
                {
                    case vhcViewMode.Shares:
                    case vhcViewMode.Volume:
                    case vhcViewMode.VolumeTotals:
                        if (_view != value)
                        {
                            _view = value;
                            _viewtype = _view == vhcViewMode.Shares ? SynoReportType.ShareList : SynoReportType.VolumeUsage;
                            DataSource = src;

                        }
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
        }
        public ISynoReportCache DataSource
        {
            set
            {
                src = value;
                if (src != null)
                {
                    switch (_viewtype)
                    {
                        case SynoReportType.ShareList:
                        case SynoReportType.VolumeUsage:
                            data = src.GetReport(_viewtype) as ISynoChartData;
                            break;
                        default:
                            break;
                    }
                    if (data != null)
                    {
                        chart1.Series.Clear();
                        unknown_traces.Clear();

                        long peak = 0;

                        _displaying_traces = data.Series;
                        if (_viewtype == SynoReportType.VolumeUsage)
                        {
                            _displaying_traces = data.Series.Where(s => s.Contains("Total")==(_view == vhcViewMode.VolumeTotals)).ToList();
                        }

                        foreach (string s in _displaying_traces)
                        {

                                Series series1 = new Series()
                                {
                                    Name = s,
                                    IsVisibleInLegend = true,
                                    IsXValueIndexed = false,
                                    ChartType = SeriesChartType.StepLine
                                };

                                Console.Write("trace " + s + ": ");
                                if (_legends.ContainsKey(s))
                                {
                                    Console.WriteLine(" picking dictionary color");
                                    series1.Color = _legends[s].Color;
                                }
                                else
                                {
                                    unknown_traces.Add(s);
                                    Console.WriteLine(" picking default color");
                                }

                                chart1.Series.Add(series1);


                                series1.Points.Clear();
                                if (_viewtype == SynoReportType.ShareList)
                                {

                                    foreach (IXYDataPoint dp in data[s])
                                    {
                                        if (peak < (long)dp.Y) peak = (long)dp.Y;
                                        series1.Points.AddXY(dp.X, dp.Y);
                                    }
                                }
                                else
                                {
                                    foreach (IXYDataPoint dp in data[s])
                                    {
                                        if (_viewtype == SynoReportType.VolumeUsage && s.Contains("Total"))
                                        {
                                            if (peak < (long)dp.Y) peak = (long)dp.Y;
                                        }
                                        series1.Points.AddXY(dp.X, dp.Y);
                                    }
                                }
                            }
                        shares_range = peak.GetFileSizeRange();

                        invalidated = true;

                        chart1.Invalidate();
                        chart1.Visible = true;
                    }
                }
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            HitTestResult h = chart1.HitTest(e.X, e.Y);
            switch (h.ChartElementType)
            {
                case ChartElementType.LegendItem:

                    LegendItem li = h.Object as LegendItem;
                    Console.WriteLine(li.SeriesName);
                    break;
                case ChartElementType.Axis:
                    Axis a = h.Object as Axis;
                    switch (a.AxisName)
                    {
                        case AxisName.Y:
                            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
                            break;
                        case AxisName.Y2:
                            break;
                        default:
                            break;
                    }
                    break;
                case ChartElementType.PlottingArea:
                    int v = Enum.GetValues(typeof(vhcViewMode)).Length;
                    View = (vhcViewMode)(((int)_view + 1) %  v) ;
                    break;
                default:
                    break;
            }

        }

        private void chart1_PostPaint(object sender, ChartPaintEventArgs e)
        {
            if (invalidated && unknown_traces.Count > 0)
            {

                int index = 0;
                for (index = 0; index < _displaying_traces.Count; index++)
                {
                    if (unknown_traces.Contains(data.Series[index]))
                    {
                        _legends.Add(data.Series[index], chart1.Series[index].Color, true);
                    }                    
                }
            }
            invalidated = false;
        }

        private void chart1_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            string text =string.Empty;
            HitTestResult h = e.HitTestResult;

            if (h.ChartElementType == ChartElementType.DataPoint)
            {
                DataPoint dp = h.Series.Points[h.PointIndex];
                if (_viewtype == SynoReportType.ShareList)
                {
                    text = string.Format("{0}: {2} ({1})",
                        h.Series.Name,
                        DateTime.FromOADate(dp.XValue),
                        ((long)dp.YValues[0]).ToFileSizeString());
                }
                else
                {
                    if (h.Series.Name.Contains("Total"))
                    {
                        text = string.Format("{0}: {2} ({1})",
                            h.Series.Name,
                            DateTime.FromOADate(dp.XValue),
                            ((long)dp.YValues[0]).ToFileSizeString());

                    }
                    else
                    {
                        text = string.Format("{0}: {2:0.0}%, ({1})",
                            h.Series.Name,
                            DateTime.FromOADate(dp.XValue),
                            (float)dp.YValues[0]);
                    }
                }
                if (!e.Text.Equals(text)) e.Text = text;

            }
        }
    }
}
