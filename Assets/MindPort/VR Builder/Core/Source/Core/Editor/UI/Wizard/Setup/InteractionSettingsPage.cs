using UnityEngine;
using VRBuilder.Core.Settings;
using VRBuilder.Editor.UI;
using VRBuilder.Editor.UI.Wizard;

/// <summary>
/// Wizard page where the user can set up interaction preferences.
/// </summary>
internal class InteractionSettingsPage : WizardPage
{
    [SerializeField]
    private bool makeGrabbablesKinematic;

    public InteractionSettingsPage() : base("Interaction Settings")
    {
        makeGrabbablesKinematic = InteractionSettings.Instance.MakeGrabbablesKinematic;
    }

    public override void Draw(Rect window)
    {
        GUILayout.BeginArea(window);

        GUILayout.Label("Interaction Settings", BuilderEditorStyles.Title);

        GUILayout.Label("You can choose how VR Builder configures objects that it makes grabbable. If you check the option below, the object will ignore physics and stay in place when the user releases it, even in mid-air. Otherwise, it will fall to the floor due to gravity.", BuilderEditorStyles.Paragraph);

        GUILayout.Space(16);

        makeGrabbablesKinematic = GUILayout.Toggle(makeGrabbablesKinematic, "Make newly created grabbables ignore physics", BuilderEditorStyles.Toggle);

        GUILayout.EndArea();
    }

    public override void Apply()
    {
        base.Apply();

        InteractionSettings.Instance.MakeGrabbablesKinematic = makeGrabbablesKinematic;
        InteractionSettings.Instance.Save();
    }
}
