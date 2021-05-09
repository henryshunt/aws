using System;
using System.Runtime.InteropServices;

namespace Aws
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Only the Linux platform is supported");
                return;
            }

            new Core.Controller().Startup();
        }
    }
}
