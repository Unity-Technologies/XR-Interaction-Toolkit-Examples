using System.Runtime.Serialization;

namespace VRBuilder.Core.Conditions
{
    /// <summary>
    /// The data interface for "object inside target" conditions.
    /// </summary>
    public interface IObjectInTargetData : IConditionData
    {
        /// <summary>
        /// The delay before the condition will trigger.
        /// </summary>
        [DataMember]
        float RequiredTimeInside { get; set; }
    }
}
