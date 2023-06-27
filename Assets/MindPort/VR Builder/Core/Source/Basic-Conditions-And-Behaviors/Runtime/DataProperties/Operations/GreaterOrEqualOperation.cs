using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left >= right.
    /// </summary>
    public class GreaterOrEqualOperation<T> : IOperationCommand<T, bool> where T : IComparable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.CompareTo(rightOperand) >= 0;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ">=";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            GreaterOrEqualOperation<float> flt = new GreaterOrEqualOperation<float>();
            GreaterOrEqualOperation<string> str = new GreaterOrEqualOperation<string>();
            GreaterOrEqualOperation<bool> bln = new GreaterOrEqualOperation<bool>();
        }
    }
}