using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left and right are not equal.
    /// </summary>
    public class NotEqualToOperation<T> : IOperationCommand<T, bool> where T : IEquatable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.Equals(rightOperand) == false;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "!=";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            NotEqualToOperation<float> flt = new NotEqualToOperation<float>();
            NotEqualToOperation<string> str = new NotEqualToOperation<string>();
            NotEqualToOperation<bool> bln = new NotEqualToOperation<bool>();
        }
    }
}