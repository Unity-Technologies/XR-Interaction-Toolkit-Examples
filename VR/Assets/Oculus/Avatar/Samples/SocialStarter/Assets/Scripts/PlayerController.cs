using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Oculus.Platform;
using Oculus.Platform.Models;

public class PlayerController : SocialPlatformManager
{

    // Secondary camera to debug and view the whole scene from above
    public Camera spyCamera;

    // The OVRCameraRig for the main player so we can disable it
    private GameObject cameraRig;

    private bool showUI = true;

    public override void Awake()
    {
        base.Awake();
        cameraRig = localPlayerHead.gameObject;
    }

    // Use this for initialization
    public override void Start()
    {
        base.Start();
        spyCamera.enabled = false;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        checkInput();
    }

    // Check for input from the touch controllers
    void checkInput()
    {
        if (UnityEngine.Application.platform == RuntimePlatform.Android)
        {
            // GearVR Controller

            // Bring up friend invite list
            if (OVRInput.GetDown(OVRInput.Button.Back))
            {
                Rooms.LaunchInvitableUserFlow(roomManager.roomID);
            }

            // Toggle Camera
            if (OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad))
            {
                ToggleCamera();
            }

            // Toggle Help UI
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                ToggleUI();
            }
        }
        else
        {
            // PC Touch 

            // Bring up friend invite list
            if (OVRInput.GetDown(OVRInput.Button.Three))
            {
                Rooms.LaunchInvitableUserFlow (roomManager.roomID);
            }

            // Toggle Camera
            if (OVRInput.GetDown(OVRInput.Button.Four))
            {
                ToggleCamera();
            }

            // Toggle Help UI
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
            {
                ToggleUI();
            }
        }
    }

    void ToggleCamera()
    {
        spyCamera.enabled = !spyCamera.enabled;
        localAvatar.ShowThirdPerson = !localAvatar.ShowThirdPerson;
        cameraRig.SetActive(!cameraRig.activeSelf);
    }

    void ToggleUI()
    {
        showUI = !showUI;
        helpPanel.SetActive(showUI);
        localAvatar.ShowLeftController(showUI);
    }
}
