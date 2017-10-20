using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JetBlast.Memory
{
    public class PatternMatch
    {
        private readonly IntPtr _address;

        public static implicit operator IntPtr(PatternMatch value) => value.Get();

        public static implicit operator PatternMatch(IntPtr value) => new PatternMatch(value);

        public PatternMatch(IntPtr address)
        {
            _address = address;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr Get(int offset = 0)
        {
            return _address + offset;
        }

        public int GetInt(int offset = 0)
        {
            var address = Get(offset);
            return Marshal.ReadInt32(address);
        }

        public long GetInt64(int offset = 0)
        {
            var address = Get(offset);
            return Marshal.ReadInt64(address);
        }

        public long GetUInt64(int offset = 0)
        {
            var address = Get(offset);
            return (uint)Marshal.ReadInt64(address);
        }

        public bool GetBool(int offset = 0)
        {
            var address = Get(offset);
            return Marshal.ReadByte(address) != 0;
        }
    }

    public sealed unsafe class Pattern
    {
        private readonly string bytes, mask;
        private readonly List<PatternMatch> result;

        public Pattern(string pattern, string moduleName)
        {
            GenerateMask(pattern, out bytes, out mask);
            result = FindMatches(moduleName);
        }

        public static Pattern Search(string pattern, string moduleName)
        {
            return new Pattern(pattern, moduleName);
        }

        public static Pattern Search(string pattern)
        {
            return new Pattern(pattern, null);
        }
  
        public PatternMatch Get(int index)
        {
            return result[index];
        }

        public PatternMatch First()
        {
            return result.Count > 0 ? Get(0) : null;
        }

        public PatternMatch Last()
        {
            return result.Count > 0 ? Get(result.Count - 1) : null;
        }

        public static void GenerateMask(string pattern, out string byteMask, out string stringMask)
        {
            StringBuilder builder = new StringBuilder(), 
                builder1 = new StringBuilder();

            foreach (var s in pattern.Split(' '))
            {
                if (s.Contains('?'))
                {
                    builder.Append((byte)0);

                    builder1.Append('?');
                }
                else
                {
                    builder.Append((char)Convert.ToByte(s, 16));

                    builder1.Append('x');
                }
            }

            byteMask = builder.ToString();

            stringMask = builder1.ToString();
        }

        private List<PatternMatch> FindMatches(string moduleName)
        {
            Win32Native.MODULEINFO module;

            Win32Native.GetModuleInformation(
                Win32Native.GetCurrentProcess(),
                Win32Native.GetModuleHandle(moduleName),
                out module,
                sizeof(Win32Native.MODULEINFO));

            List<PatternMatch> matches = new List<PatternMatch>();

            var address = module.lpBaseOfDll.ToInt64();

            var endAddress = address + module.SizeOfImage;

            for (; address < endAddress; address++)
            {
                if (ComparePtrBytes((byte*)address, bytes.ToCharArray(), mask.ToCharArray()))
                {
                    matches.Add(new IntPtr(address));
                }
            }

            return matches;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ComparePtrBytes(byte* pointerData, char[] pattern, char[] mask)
        {
            if (pattern.Length != mask.Length) return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                if (mask[i] == 'x' && pointerData[i] != pattern[i])
                    break;

                if (i + 1 == pattern.Length)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class Win32Native
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo,
            int cb);

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }
    }
}
