using System;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// A command that executes an operation between two values.
    /// </summary>
    /// <typeparam name="TOperand">Type of the operands.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    public interface IOperationCommand<TOperand, TResult> : IFormattable
    {
        /// <summary>
        /// Executes the operation on the provided operands.
        /// </summary>
        TResult Execute(TOperand leftOperand, TOperand rightOperand);
    }
}