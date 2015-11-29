using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBlocks.Common.Utilities
{
    public static class TimerResolution
    {

        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);


        public static uint SetResolution(uint resolution = 10000)
        {
            uint currentResolution = 0;
            NtSetTimerResolution(resolution, true, ref currentResolution);
            return currentResolution;
        }
    }
}
