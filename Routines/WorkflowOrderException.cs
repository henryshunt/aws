using System;

namespace AWS.Routines
{
    internal class WorkflowOrderException : Exception
    {
        public WorkflowOrderException(string message) : base(message)
        {

        }
    }
}
