using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Reclamation.TimeSeries.Forms
{
    public partial class TraceOptions : UserControl, IExplorerSettings
    {
        public TraceOptions()
        {
            InitializeComponent();
        }

        

        #region IExplorerSettingsView Members

        public void WriteToSettings(PiscesSettings settings)
        {
            settings.SelectedAnalysisType = AnalysisType.TraceAnalysis;
            settings.ExceedanceLevels = exceedanceLevelPicker1.ExceedanceLevels;
            settings.AlsoPlotTrace = this.checkBoxPlotTrace.Checked;
            settings.PlotTrace = this.TraceToPlot;
            settings.traceExceedanceAnalysis = this.traceExceedanceCheckBox.Checked;
            settings.traceAggregationAnalysis = this.traceAggregationCheckBox.Checked;
            settings.sumCYRadio = this.sumCYRadio.Checked;
            settings.sumWYRadio = this.sumWYRadio.Checked;
            settings.sumCustomRangeRadio = this.sumRangeRadio.Checked;
            settings.PlotMinTrace = this.checkBoxPlotMin.Checked;
            settings.PlotAvgTrace = this.checkBoxPlotAvg.Checked;
            settings.PlotMaxTrace = this.checkBoxPlotMax.Checked;
            settings.TimeWindow = timeWindowOptions1.TimeWindow;
            settings.MonthDayRange = this.rangePicker1.MonthDayRange;
        }

        public void ReadFromSettings(PiscesSettings settings)
        {
            this.checkBoxPlotTrace.Checked = settings.AlsoPlotTrace;
            this.maskedTextBoxPlotTrace.Text = settings.PlotTrace.ToString();
            this.traceExceedanceCheckBox.Checked = settings.traceExceedanceAnalysis;
            this.traceAggregationCheckBox.Checked = settings.traceAggregationAnalysis;
            this.sumCYRadio.Checked = settings.sumCYRadio;
            this.sumWYRadio.Checked = settings.sumWYRadio;
            this.sumRangeRadio.Checked = settings.sumCustomRangeRadio;
            this.checkBoxPlotMin.Checked = settings.PlotMinTrace;
            this.checkBoxPlotAvg.Checked = settings.PlotAvgTrace;
            this.checkBoxPlotMax.Checked = settings.PlotMaxTrace;
            this.timeWindowOptions1.TimeWindow = settings.TimeWindow; 
            rangePicker1.BeginningMonth = settings.BeginningMonth;
            rangePicker1.MonthDayRange = settings.MonthDayRange;
        }

        #endregion

        

        private int TraceToPlot
        {
            get
            {
                int trc = 1;
                Int32.TryParse(maskedTextBoxPlotTrace.Text, out trc);
                return trc;
            }
        }

        private void checkBoxPlotYear_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBoxPlotTrace.Checked)
            { this.maskedTextBoxPlotTrace.Enabled = true; }
            else
            { this.maskedTextBoxPlotTrace.Enabled = false; }
        }

        private void traceAggregationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.traceAggregationCheckBox.Checked)
            {
                this.exceedanceAnalysisGroupBox.Enabled = false;
                this.aggregationAnalysisGroupBox.Enabled = true;
            }
        }

        private void traceExceedanceCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.traceExceedanceCheckBox.Checked)
            {
                this.exceedanceAnalysisGroupBox.Enabled = true;
                this.aggregationAnalysisGroupBox.Enabled = false;
            }
        }

        private void sumRangeRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (this.sumRangeRadio.Checked)
            {
                this.rangePicker1.Enabled = true;
            }
            else
            {
                this.rangePicker1.Enabled = false;
            }
        }
    }
}

