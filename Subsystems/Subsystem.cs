using AWS.Routines;
using System.Threading;

namespace AWS.Subsystems
{
    internal class Subsystem
    {
        protected Configuration Configuration;
        private Thread SubsystemThread;
        private object ExitLock = new object();

        public void Start(Configuration configuration)
        {
            Configuration = configuration;
            SubsystemThread = new Thread(() => SubsystemProcedure());
            SubsystemThread.Start();
        }

        public void Exit()
        {
            lock (ExitLock)
                SubsystemThread.Abort();
        }

        public virtual void SubsystemProcedure() { }
    }
}
