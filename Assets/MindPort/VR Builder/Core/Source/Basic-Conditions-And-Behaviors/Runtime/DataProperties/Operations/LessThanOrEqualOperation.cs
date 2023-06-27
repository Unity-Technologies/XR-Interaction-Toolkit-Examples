using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left <= right.
    /// </summary>
    public class LessThanOrEqualOperation<T> : IOperationCommand<T, bool> where T : IComparable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.CompareTo(rightOperand) <= 0;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "<=";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            LessThanOrEqualOperation<float> flt = new LessThanOrEqualOperation<float>();
            LessThanOrEqualOperation<string> str = new LessThanOrEqualOperation<string>();
            LessThanOrEqualOperation<bool> bln = new LessThanOrEqualOperation<bool>();
        }
    }
}