using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace JetThrustPhysics
{
    public static class InteropExt
    {
        public static float ReadFloat(IntPtr address)
        {
            float[] result = new float[1];
            Marshal.Copy(address, result, 0, 1);
            return result[0];
        }

    }
}
