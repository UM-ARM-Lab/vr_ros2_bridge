using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;
using RosMessageTypes.VrRos2Bridge;
using RosMessageTypes.Geometry;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.XR;

/// <summary>
///
/// </summary>
public class PublishControllerInfo : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "vr_controller_info";
    public string frameName = "vr";
    public bool debug_pub = false;

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
        if (debug_pub)
        {
            ros.RegisterPublisher<PoseStampedMsg>("left_controller_pose", 10);
            ros.RegisterPublisher<PoseStampedMsg>("right_controller_pose", 10);
            ros.RegisterPublisher<TwistStampedMsg>("left_controller_twist_stamped", 10);
            ros.RegisterPublisher<TwistStampedMsg>("right_controller_twist_stamped", 10);

        }
    }
    private void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed > publishMessagePeriod)
        {
            ControllersInfoMsg controllersInfoMsg = new ControllersInfoMsg();

            List<UnityEngine.XR.InputDevice> trackers = new List<UnityEngine.XR.InputDevice>();
            var trackerCharacteristics = UnityEngine.XR.InputDeviceCharacteristics.TrackedDevice;
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(trackerCharacteristics, trackers);

            var controllerCharacteristics = UnityEngine.XR.InputDeviceCharacteristics.Controller;
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, controllers);

            List<ControllerInfoMsg> controllerInfoMsgs = new List<ControllerInfoMsg>();
            if (controllers.Count == 0)
            {
                Debug.Log("No controllers found. Make sure they are on. Check the Steam VR Web Console to debug.");
            }

            foreach (var device in controllers)
            {
                bool isLeftController = (device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Left) != UnityEngine.XR.InputDeviceCharacteristics.None;
                string controllerSide;
                if (isLeftController)
                {
                    controllerSide = "left";
                }
                else
                {
                    controllerSide = "right";
                }

                // pose
                Vector3 position;
                Quaternion orientation;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out position);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out orientation);
                bool isTracked;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out isTracked);

                if (!isTracked)
                {
                    continue;
                }

                // twist
                Vector3 linear_velocity;
                Vector3 angular_velocity;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out linear_velocity);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAngularVelocity, out angular_velocity);

                var controllerInfoMsg = new ControllerInfoMsg();
                controllerInfoMsg.controller_name = device.name + " " + controllerSide;

                // Rotate by -90 about Y to make the +X axis forward
                orientation *= Quaternion.Euler(0, -90, 0);
                linear_velocity = Quaternion.Euler(0, -90, 0) * linear_velocity;
                angular_velocity = Quaternion.Euler(0, -90, 0) * angular_velocity;


                // Change from left to right handed coordinate frame. Swap Z and Y, and negate all rotations
                controllerInfoMsg.controller_pose.position.x = position.x;
                controllerInfoMsg.controller_pose.position.y = position.z;
                controllerInfoMsg.controller_pose.position.z = position.y;
                controllerInfoMsg.controller_pose.orientation.w = orientation.w;
                controllerInfoMsg.controller_pose.orientation.x = -orientation.x;
                controllerInfoMsg.controller_pose.orientation.y = -orientation.z;
                controllerInfoMsg.controller_pose.orientation.z = -orientation.y;

                controllerInfoMsg.controller_velocity.linear.x = linear_velocity.x;
                controllerInfoMsg.controller_velocity.linear.y = linear_velocity.z;
                controllerInfoMsg.controller_velocity.linear.z = linear_velocity.y;
                controllerInfoMsg.controller_velocity.angular.x = -angular_velocity.x;
                controllerInfoMsg.controller_velocity.angular.y = -angular_velocity.z;
                controllerInfoMsg.controller_velocity.angular.z = -angular_velocity.y;

                // For visualization in RViz
                var pose_msg = new PoseStampedMsg();
                pose_msg.header.frame_id = frameName;
                pose_msg.pose = controllerInfoMsg.controller_pose;
                var pose_topic_name = string.Format("{0}_controller_pose", controllerSide);

                var twist_msg = new TwistStampedMsg();
                twist_msg.header.frame_id = frameName; // TODO: what frame should this be in???
                twist_msg.twist = controllerInfoMsg.controller_velocity;
                var twist_topic_name = string.Format("{0}_controller_twist_stamped", controllerSide);
                if (debug_pub)
                {
                    ros.Publish(pose_topic_name, pose_msg);
                    ros.Publish(twist_topic_name, twist_msg);
                }

                // Get button press states
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out controllerInfoMsg.trigger_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out controllerInfoMsg.trigger_axis);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out controllerInfoMsg.grip_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out controllerInfoMsg.trackpad_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out controllerInfoMsg.menu_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out controllerInfoMsg.primary_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out controllerInfoMsg.secondary_button);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out controllerInfoMsg.secondary_button);
                var trackpad_axis = Vector2.zero;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out trackpad_axis);
                controllerInfoMsg.trackpad_axis_x = trackpad_axis.x;
                controllerInfoMsg.trackpad_axis_y = trackpad_axis.y;
                var trackpad_axis_touch = Vector2.zero;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out trackpad_axis_touch);
                controllerInfoMsg.trackpad_axis_touch_x = trackpad_axis_touch.x;
                controllerInfoMsg.trackpad_axis_touch_y = trackpad_axis_touch.y;


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