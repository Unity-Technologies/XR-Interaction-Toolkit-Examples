using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// True if left and right are equal.
    /// </summary>
    public class EqualToOperation<T> : IOperationCommand<T, bool> where T : IEquatable<T>
    {
        /// <inheritdoc/>
        public bool Execute(T leftOperand, T rightOperand)
        {
            return leftOperand != null && leftOperand.Equals(rightOperand);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "==";
        }

        /// <summary>
        /// Constructs concrete types in order for them to be seen by IL2CPP's ahead of time compilation.
        /// </summary>
        private class AOTHelper
        {
            EqualToOperation<float> flt = new EqualToOperation<float>();
            EqualToOperation<string> str = new EqualToOperation<string>();
            EqualToOperation<bool> bln = new EqualToOperation<bool>();
        }
    }
}