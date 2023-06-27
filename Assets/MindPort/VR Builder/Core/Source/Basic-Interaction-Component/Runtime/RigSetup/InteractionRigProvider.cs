using UnityEngine;

namespace VRBuilder.BasicInteraction.RigSetup
{
    /// <summary>
    /// Provides all information and methods to setup a scene with a fitting and working rig.
    /// </summary>
    public abstract class InteractionRigProvider
    {
        /// <summary>
        /// The name for this rig, has to be unique.
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Name of the prefab which should be loaded.
        /// </summary>
        public abstract string PrefabName { get; }

        /// <summary>
        /// Decides if this rig is useable at this moment. Can be overwritten to be more sophisticated.
        /// </summary>
        public virtual bool CanBeUsed()
        {
            return true;
        }

        /// <summary>
        /// Returns the tooltip which should be shown when this rig cannot be used.
        /// </summary>
        public virtual string GetSetupTooltip()
        {
            return "";
        }

        /// <summary>
        /// Returns the found Prefab object.
        /// </summary>
        public virtual GameObject GetPrefab()
        {
            if (string.IsNullOrEmpty(PrefabName))
            {
                return null;
            }
            
            return FindPrefab(PrefabName);
        }

        /// <summary>
        /// Will be called when the scene is done setting up this rig to allow additional changes.
        /// </summary>
        public virtual void OnSetup()
        {
            // Allow additional actions.
        }
        
        /// <summary>
        /// Will be called before the rig is instantiated.
        /// </summary>
        public virtual void PreSetup()
        {
            // Allow additional actions.
        }
        
        /// <summary>
        /// Searches the given prefab name and returns it.
        /// </summary>
        protected GameObject FindPrefab(string prefab)
        {
            return Resources.Load(prefab, typeof(GameObject)) as GameObject;
        }
    }
}