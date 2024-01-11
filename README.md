# vr_ros2_bridge
Unity project and ROS 2 interface definitions for using the HTC Vive.

Contents:

 - `vr_ros2_bridge` is a Unity project with a minimalistic VR setup, and a C# script called `PublishControllerInfo.cs` that reads from VR info from Unity and publishes via TCP. On the other end of the TCP connection is the C# ROS2 bridge, running on some Ubuntu machine that also has ROS 2 installed.
 - `vr_ros2_bridge_msgs` is a ROS 2 messages package that defines the custom message `ControllersInfo.msg` which is a list of `ControllerInfo.msg`, which contains pose and button statuses.

# Ubuntu Setup

In your ROS 2 colcon workspace, you'll need this repo and the ROS TCP Endpoint repo (`main-ros2` branch).

```
git clone git@github.com:/UM-ARM-Lab/vr_ros2_bridge.git
git clone git@github.com:Unity-Technologies/ROS-TCP-Endpoint.git -b main-ros2
```


# How to run

 - In unity (on Windows), import and run ths `vr_ros2_bridge` project.
 - In Ubuntu, 
 - In Ubuntu, start the C# ROS 2 bridge. See [this guide](https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/setup.md) for details.
