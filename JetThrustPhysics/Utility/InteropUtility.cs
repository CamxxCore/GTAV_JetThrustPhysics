using System;
using System.Runtime.InteropServices;

namespace JetBlast.Utility
{
    public static class InteropUtility
    {
        public static float ReadFloat(IntPtr address)
        {
            byte[] bytes = new byte[4];
            Marshal.Copy(address, bytes, 0, 4);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static void WriteFloat(IntPtr address, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Marshal.Copy(bytes, 0, address, 4);
        }
    }
}
