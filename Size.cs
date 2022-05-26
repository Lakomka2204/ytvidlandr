using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytvidlandr
{
    public struct Size
    {
        public double kB
        {
            get
            {
                return bytes / 1024;
            }
        }
        public double MB
        {
            get
            {
                return bytes / 1024 / 1024;
            }
        }
        public double GB
        {
            get
            {
                return bytes / 1024 / 1024 / 1024;
            }
        }
        double bytes;
        public double Bytes
        {
            get
            {
                return bytes;
            }
        }
        public string Auto()
        {
            return GB >= 1.0 ? GB.ToString("0.##") + "GB" : MB >= 1.0 ? MB.ToString("0.##") + "MB" : kB.ToString("0.##") + "kB";
        }
        public Size SetBytes(double bytes)
        {
            this.bytes = bytes;
            return this;
        }
        public Size(double bytes)
        {
            this.bytes = bytes;
        }
    }

}
