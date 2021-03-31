using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;

namespace oda
{
    public class FileTimer: System.Timers.Timer
    {
        public string FileName;
        public int UsedCount;
        public Object SourceObject;

        public void IncrimentUsedCount()
        {
            Interlocked.Increment(ref UsedCount);
        }
    }
}
