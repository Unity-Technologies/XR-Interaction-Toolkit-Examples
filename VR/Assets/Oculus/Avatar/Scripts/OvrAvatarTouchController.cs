using Oculus.Avatar;

public class OvrAvatarTouchController : OvrAvatarComponent
{
    public bool isLeftHand = true;
    ovrAvatarControllerComponent component = new ovrAvatarControllerComponent();

    void Update()
    {
        if (owner == null)
        {
            return;
        }

        bool hasComponent = false;
        if (isLeftHand)
        {
            hasComponent = CAPI.ovrAvatarPose_GetLeftControllerComponent(owner.sdkAvatar, ref component);
        }
        else
        {
            hasComponent = CAPI.ovrAvatarPose_GetRightControllerComponent(owner.sdkAvatar, ref component);
        }

        if (hasComponent)
        {
            UpdateAvatar(component.renderComponent);
        }
        else
        {
            if (isLeftHand)
            {
                owner.ControllerLeft = null;

            }
            else
            {
                owner.ControllerRight = null;
            }

            Destroy(this);
        }
    }
}
