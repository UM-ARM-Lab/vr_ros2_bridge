using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using RosMessageTypes.VrRos2Bridge;
using RosMessageTypes.Geometry;
using static UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputTrackingAggregator;

/// <summary>
///
/// </summary>
public class PublishControllerInfo : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "vr_controller_info";

    // Controller Input Devices - used to access button states.
    List<UnityEngine.XR.InputDevice> controllers = new List<UnityEngine.XR.InputDevice>();

    // Publish the cube's position and rotation every N seconds
    public float publishMessagePeriod = 0.03f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed = 0;


    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ControllersInfoMsg>(topicName, 10);
        ros.RegisterPublisher<PoseStampedMsg>("left_controller_pose", 10);
        ros.RegisterPublisher<PoseStampedMsg>("right_controller_pose", 10);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed > publishMessagePeriod)
        {
            ControllersInfoMsg controllersInfoMsg = new ControllersInfoMsg();

            var desiredCharacteristics = UnityEngine.XR.InputDeviceCharacteristics.Controller;
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, controllers);

            List<ControllerInfoMsg> controllerInfoMsgs = new List<ControllerInfoMsg>();

            foreach (var device in controllers)
            {
                bool isLeftController = (device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Left) != UnityEngine.XR.InputDeviceCharacteristics.None;
                string controllerSide;
                if (isLeftController)
                {
                    controllerSide = "Left";
                }
                else
                {
                    controllerSide = "Right";
                }
                string controllerObjectName = string.Format("{0} Controller Offset", controllerSide);
                var controllerObject = GameObject.Find(controllerObjectName);

                if (controllerObject == null)
                {
                    Debug.Log(string.Format("No such object {0}", controllerObjectName));
                    continue;
                }

                var controllerInfoMsg = new ControllerInfoMsg();
                controllerInfoMsg.controller_name = name;

                // Get pose of the controller, and transform from a Y-up left-handed frame to a Z-up, X-forward, right handed frame
                controllerInfoMsg.controller_pose.position.x = controllerObject.transform.position.x;
                controllerInfoMsg.controller_pose.position.y = controllerObject.transform.position.z;
                controllerInfoMsg.controller_pose.position.z = controllerObject.transform.position.y;
                controllerInfoMsg.controller_pose.orientation.w = controllerObject.transform.rotation.w;
                controllerInfoMsg.controller_pose.orientation.x = -controllerObject.transform.rotation.x;
                controllerInfoMsg.controller_pose.orientation.y = -controllerObject.transform.rotation.z;
                controllerInfoMsg.controller_pose.orientation.z = -controllerObject.transform.rotation.y;

                // For visualization in RViz
                PoseStampedMsg pose_msg = new PoseStampedMsg();
                pose_msg.header.frame_id = "world";
                pose_msg.pose = controllerInfoMsg.controller_pose;
                var pose_topic_name = string.Format("{0}_controller_pose", controllerSide).ToLower();
                ros.Publish(pose_topic_name, pose_msg);

                // Get button press states
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out controllerInfoMsg.trigger_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out controllerInfoMsg.trigger_axis);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out controllerInfoMsg.grip_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out controllerInfoMsg.trackpad_button);

                // Add to list of controller info messages.
                controllerInfoMsgs.Add(controllerInfoMsg);
            }

            // Populate controllers info msg.
            controllersInfoMsg.controllers_info = controllerInfoMsgs.ToArray();

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, controllersInfoMsg);

            timeElapsed = 0;
        }
    }
}