﻿using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using eXpand.Persistent.Base.PivotChart;

namespace eXpand.Persistent.BaseImpl.PivotChart {
    public class PivotOptionsChartDataSource : BaseObject, IPivotOptionsChartDataSource {
        public PivotOptionsChartDataSource(Session session) : base(session) {
        }
    }
}