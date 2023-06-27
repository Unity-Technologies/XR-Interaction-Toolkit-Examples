using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left < right.
    /// </summary>
    public class LessThanOperation<T> : IOperationCommand<T, bool> where T : IComparable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.CompareTo(rightOperand) < 0;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "<";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            LessThanOperation<float> flt = new LessThanOperation<float>();
            LessThanOperation<string> str = new LessThanOperation<string>();
            LessThanOperation<bool> bln = new LessThanOperation<bool>();
        }
    }
}