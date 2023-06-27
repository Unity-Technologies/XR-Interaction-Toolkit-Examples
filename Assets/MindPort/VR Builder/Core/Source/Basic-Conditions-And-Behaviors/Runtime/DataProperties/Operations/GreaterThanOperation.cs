using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left > right.
    /// </summary>
    public class GreaterThanOperation<T> : IOperationCommand<T, bool> where T : IComparable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.CompareTo(rightOperand) > 0;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ">";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            GreaterThanOperation<float> flt = new GreaterThanOperation<float>();
            GreaterThanOperation<string> str = new GreaterThanOperation<string>();
            GreaterThanOperation<bool> bln = new GreaterThanOperation<bool>();
        }
    }
}