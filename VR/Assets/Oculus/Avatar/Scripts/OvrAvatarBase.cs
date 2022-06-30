using Oculus.Avatar;

public class OvrAvatarBase : OvrAvatarComponent
{
    ovrAvatarBaseComponent component = new ovrAvatarBaseComponent();

    void Update()
    {
        if (owner == null)
        {
            return;
        }

        if (CAPI.ovrAvatarPose_GetBaseComponent(owner.sdkAvatar, ref component))
        {
            UpdateAvatar(component.renderComponent);
        }
        else
        {
            owner.Base = null;
            Destroy(this);
        }
    }
}
