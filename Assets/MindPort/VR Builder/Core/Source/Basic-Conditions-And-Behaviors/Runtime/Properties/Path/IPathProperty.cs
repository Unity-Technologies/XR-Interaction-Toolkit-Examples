using UnityEngine;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// This property represents a linear path.
    /// </summary>
    public interface IPathProperty : ISceneObjectProperty
    {
        /// <summary>
        /// Get a point on the path.
        /// </summary>
        /// <param name="t">Position on the path, 0 to 1.</param>
        Vector3 GetPoint(float t);

        /// <summary>
        /// Get the direction of the path in a given position.
        /// </summary>
        /// <param name="t">Position on the path, 0 to 1.</param>
        Vector3 GetDirection(float t);
    }
}
