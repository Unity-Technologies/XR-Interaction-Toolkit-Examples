using VRBuilder.Editor.PackageManager;

public class NewtonsoftJSONPackageEnabler : Dependency
{
    /// <inheritdoc/>
    public override string Package { get; } = "com.unity.nuget.newtonsoft-json";

    /// <inheritdoc/>
    public override int Priority { get; } = 3;
}
