namespace VRBuilder.Editor.PackageManager
{
    /// <summary>
    /// Adds Unity's Post-Processing package as a dependency.
    /// </summary>
    public class PostProcessingPackageEnabler : Dependency
    {
        /// <inheritdoc/>
        public override string Package { get; } = "com.unity.postprocessing";

        /// <inheritdoc/>
        public override int Priority { get; } = 10;

        /// <inheritdoc/>
        protected override string[] Layers { get; } = { "Post-Processing" };
    }
}
