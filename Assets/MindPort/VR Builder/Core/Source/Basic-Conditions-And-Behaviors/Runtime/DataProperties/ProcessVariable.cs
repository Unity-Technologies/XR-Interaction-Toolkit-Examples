using System.Runtime.Serialization;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// Struct for a process variable. Accommodates the value coming from a <see cref="IDataProperty{T}"/>, or being a constant value set e.g. in the Step Inspector.
    /// </summary>  
    [DataContract]
    public struct ProcessVariable<T>
    {
        /// <summary>
        /// Constant value.
        /// </summary>
        [DataMember]
        public T ConstValue { get; set; }

        /// <summary>
        /// Property reference for the variable.
        /// </summary>
        [DataMember]
        public ScenePropertyReference<IDataProperty<T>> PropertyReference { get; set; }

        /// <summary>
        /// If true, <see cref="ConstValue"/> is used. Else the value will be fetched from the <see cref="PropertyReference"/>.
        /// </summary>
        [DataMember]
        public bool IsConst { get; set; }

        public ProcessVariable(T constValue, string propertyReferenceName, bool isConst)
        {
            ConstValue = constValue;
            PropertyReference = new ScenePropertyReference<IDataProperty<T>>(propertyReferenceName);
            IsConst = isConst;
        }

        /// <summary>
        /// Returns the current value of this variable.
        /// </summary>
        public T Value => IsConst ? ConstValue : PropertyReference.Value.GetValue();
    }
}