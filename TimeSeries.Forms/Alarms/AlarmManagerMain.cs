﻿using Reclamation.Core;
using Reclamation.TimeSeries.Alarms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Reclamation.TimeSeries.Forms.Alarms
{
    public partial class AlarmManagerMain : Form
    {
        public AlarmManagerMain()
        {
            InitializeComponent();
        }
        
        public AlarmManagerMain(TimeSeriesDatabase db)
        {
            AlarmManagerControl c = new AlarmManagerControl(db);
            this.Controls.Add(c);
            c.Dock = DockStyle.Fill;

        }

        

    }
}
