using System;
using System.Collections.Generic;
using System.Text;

namespace SimdJsonSharp
{
    internal static class ThrowHelper
    {
        public static void ThrowPNSE()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
