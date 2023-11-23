using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JTDevice
{
    public class Logger
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
