﻿using System;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace Reclamation.TimeSeries.Forms.Calculations
{
    public partial class CalculationProperties : Form
    {
        public CalculationProperties()
        {
            m_series = new CalculationSeries();
            InitializeComponent();
        }
        CalculationSeries m_series;

        PiscesTree tree1;

        public CalculationProperties(CalculationSeries s, ITreeModel model, TimeSeriesDatabase db)
        {
            string[] DBunits = db.GetUniqueUnits();

            InitializeComponent();
            tree1 = new PiscesTree(model);
            tree1.ExpandRootNodes();

            tree1.AllowDrop = false;
            tree1.Parent = this.splitContainer1.Panel1;
            tree1.Dock = DockStyle.Fill;
            tree1.RemoveCommandLine();

            m_series = s;
            basicEquation1.SeriesExpression = m_series.Expression;
            basicEquation1.SiteID = m_series.SiteID;
        }

        public bool Calculate
        {
            get { return this.basicEquation1.Calculate; }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            m_series.Expression = basicEquation1.SeriesExpression;
            var a = this.basicEquation1.SiteID + "_" + basicEquation1.Parameter;
            if (a != "")
            {
                m_series.Name = a;
                string tn = basicEquation1.TimeInterval.ToString().ToLower() + "_" + TimeSeriesDatabase.SafeTableName(a);
                tn = tn.Replace("irregular", "instant");
                m_series.Table.TableName = tn;

                TimeSeriesName x = new TimeSeriesName(a, basicEquation1.TimeInterval);
                m_series.SiteID = x.siteid;
            }
            
            

            string errorMessage = "";
            m_series.TimeInterval = basicEquation1.TimeInterval;
            if ( m_series.TimeSeriesDatabase.Parser.VariableResolver is Parser.HydrometVariableResolver 
               || m_series.IsValidExpression(basicEquation1.SeriesExpression, out errorMessage))
            {
               
            }
            else
            {
                
               var result = MessageBox.Show("Your equation may have an error. Click OK to proceed.\n" + errorMessage,"Use this Equation?", MessageBoxButtons.OKCancel);
                if( result == DialogResult.Cancel)
                  DialogResult = DialogResult.None;   
            }
        }
    }
}
