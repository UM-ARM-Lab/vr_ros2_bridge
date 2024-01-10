using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using RosMessageTypes.VrRos2Bridge;

/// <summary>
///
/// </summary>
public class PublishControllerInfo : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "vr_controller_info";
    ControllersInfoMsg controllersInfoMsg;

    // Controller Input Devices - used to access button states.
    List<UnityEngine.XR.InputDevice> controllers = new List<UnityEngine.XR.InputDevice>();

    // Controller Game Objects - used to access controller tracked poses.
    GameObject leftController;
    GameObject rightController;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ControllersInfoMsg>(topicName);
    }

    private void Update()
    {
        var desiredCharacteristics = UnityEngine.XR.InputDeviceCharacteristics.Controller;
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, controllers);

        List<ControllerInfoMsg> controllerInfoMsgs = new List<ControllerInfoMsg>();

        foreach (var device in controllers)
        {
            Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.role.ToString()));
            var name = device.role.ToString();  // TODO: Redo naming using characteristics.

            // TODO: Get pose of the controller, based on whether its the left/right controller.
            
            var controllerInfoMsg = new ControllerInfoMsg();

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

            // TODO: Populate controller info.
        }

        // Finally send the message to server_endpoint.py running in ROS
        ros.Publish(topicName, controllersInfoMsg);
    }
}