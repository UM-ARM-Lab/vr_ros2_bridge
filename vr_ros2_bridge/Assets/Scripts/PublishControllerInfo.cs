using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using RosMessageTypes.VrRos2Bridge;
using RosMessageTypes.Geometry;

/// <summary>
///
/// </summary>
public class PublishControllerInfo : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "vr_controller_info";

    // Controller Input Devices - used to access button states.
    List<UnityEngine.XR.InputDevice> controllers = new List<UnityEngine.XR.InputDevice>();

    // Controller Game Objects - used to access controller tracked poses.
    public GameObject leftController;
    public GameObject rightController;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ControllersInfoMsg>(topicName);
    }

    private void Update()
    {
        ControllersInfoMsg controllersInfoMsg = new ControllersInfoMsg();

        var desiredCharacteristics = UnityEngine.XR.InputDeviceCharacteristics.Controller;
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, controllers);

        List<ControllerInfoMsg> controllerInfoMsgs = new List<ControllerInfoMsg>();

        foreach (var device in controllers)
        {
            bool isLeftController = (device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Left) != UnityEngine.XR.InputDeviceCharacteristics.None;
            // Debug.Log(string.Format("Device found with name '{0}' and role '{1}'.", device.name, device.role.ToString()));
            var name = isLeftController ? "LeftController" : "RightController";

            var controllerInfoMsg = new ControllerInfoMsg();
            controllerInfoMsg.controller_name = name;

            // Get pose of the controller, based on whether its the left/right controller.
            GameObject thisController = isLeftController ? leftController : rightController;
            controllerInfoMsg.controller_pose.position.x = thisController.transform.position.x;
            controllerInfoMsg.controller_pose.position.y = thisController.transform.position.y;
            controllerInfoMsg.controller_pose.position.z = thisController.transform.position.z;
            controllerInfoMsg.controller_pose.orientation.w = thisController.transform.rotation.w;
            controllerInfoMsg.controller_pose.orientation.x = thisController.transform.rotation.x;
            controllerInfoMsg.controller_pose.orientation.y = thisController.transform.rotation.y;
            controllerInfoMsg.controller_pose.orientation.z = thisController.transform.rotation.z;

            // Get button press states.
            bool triggerValue = false;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue)
            {
                Debug.Log(string.Format("{0}: Trigger button is pressed!", name));
            }

            float triggerAxisValue = (float) -1.0;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerAxisValue))
            {
                Debug.Log(string.Format("{0}: Trigger: {1}", name, triggerAxisValue.ToString()));
            }

            bool gripValue = false;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out gripValue) && gripValue)
            {
                Debug.Log(string.Format("{0}: Grip button is pressed!", name));
            }

            bool primary2DAxisClickValue = false;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out primary2DAxisClickValue) && primary2DAxisClickValue)
            {
                Debug.Log(string.Format("{0}: Trackpad button is pressed!", name));
            }

            // Populate controller button info.
            controllerInfoMsg.trigger_button = triggerValue;
            controllerInfoMsg.trigger_axis = triggerAxisValue;
            controllerInfoMsg.grip_button = gripValue;
            controllerInfoMsg.trackpad_button = primary2DAxisClickValue;

            // Add to list of controller info messages.
            controllerInfoMsgs.Add(controllerInfoMsg);
        }

        // Populate controllers info msg.
        controllersInfoMsg.controllers_info = controllerInfoMsgs.ToArray();
        // TODO: Add header info.

        // Finally send the message to server_endpoint.py running in ROS
        ros.Publish(topicName, controllersInfoMsg);
    }
}