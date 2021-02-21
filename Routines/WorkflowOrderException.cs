using System;

namespace Aws.Routines
{
    internal class WorkflowOrderException : Exception
    {
        public WorkflowOrderException(string message) : base(message)
        {

        }
    }
}
