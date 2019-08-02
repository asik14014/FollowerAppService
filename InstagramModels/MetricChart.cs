using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramModels
{
    public class MetricChart
    {
        public long x { get; set; }
        public decimal y { get; set; }

        public MetricChart(decimal number, long date)
        {
            y = number;
            x = date;
        }
    }
}
