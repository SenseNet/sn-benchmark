using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    public enum LoadControl {Stay, Increase, Decrease, Exit}
    public class LoadController
    {
        public void Progress(int reqPerSec)
        {
            
        }
        public LoadControl Next()
        {
            // ???

            return LoadControl.Stay;
        }
    }
}
