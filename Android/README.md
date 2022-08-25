# Wheel (Android Side)  

Android Steering Wheel Project (Android Application folder)

------

### Motion Signals  

The application will read orientation by using the accelerator sensor and magnetic field sensor.  

__Pitch__:  
* Controls Steering (POV control on xbox controller)  
* Float, range = (-90, 90), in degrees  
* Three modes are:  
  * Rest => range(-2, 2)  
  * Turn Left => range(2, 90)  
  * Turn Right => range(-90, -2)  

__Roll__:  
* Controls Acceleration (LT, RT on xbox controller)  
* Float, range = (-90, 90), in degrees  
* Three modes are:  
  * Rest => range(-40, -30)  
  * Forward => range(-90, -40)  
  * Backward => range(-30, 90)  

Please refer to the values showed on app and adjust your postion  

------

### Connection Signals  

The application will either try to connect a service with unique UUID via Bluetooth, or connect to Wifi hotspot on PC by IP address input.  

The connection starts by a validation process, it sends `123456` to service, and expect a returned value of `654321`. After that, a connection will be established for later communication.  

__Data Pack__ (each):  
1. `10086` as the data pack seperator (4 ints per pack).  
2. motion button indicator (bool)  
3. motion status indicator (int)  
4. motion value (float)  

__Motion Button__:  
* `false` => contains either pitch or roll data, need to read last float number  
* `true` => is a button, can safely ignore last float number  

__Motion Status__:  
* If not motion button:  
  * `0` => float number is pitch value  
  * `1` => float number is roll value  
* If motion button:  
  * `0` => Button `X`
  * `1` => Button `Y`
  * `2` => Button `A`
  * `3` => Button `B`
  * `4` => Button `LB`
  * `5` => Button `RB`
  * `6` => Button `UP`
  * `7` => Button `DOWN`
  * `8` => Button `RIGHT`
  * `9` => Button `LEFT`
  * `10` => Button `BACK`
  * `11` => Button `START`


------

### Steering Angle Restoration

Here's a world coordinate system I stole from [learnopengl](https://developer.android.com/guide/topics/sensors/sensors_overview#sensors-coords):\
![coordinate](https://learnopengl.com/img/getting-started/coordinate_systems_right_handed.png)

In this app, the new angles / orientations are computed by a combination of [Accelerometer and Magnetic Field](https://developer.android.com/guide/topics/sensors/sensors_position#sensors-pos-orient), which are in absolute XYZ coordinate as shown in the image.

Now imagine this situation:

https://user-images.githubusercontent.com/22620163/186577875-96fb6ec0-1b3f-4e13-a498-ec0c23320a47.mp4

_We use the same world coordinate as previous image._
1. User starts by holding phone vertically straight, landscape mode.
2. User first rotates phone forward (around X) to accelerate car. Then +Z follows the rotation to +Z' (keep vertical to the screen).
3. Then user rotate around +Z' to steer car. The angle around +Z' is the expected angle.
4. However, the motion sensor only captures the angle in absolute world space.
 
If use this value from motion sensor directly, there'll be a bug:\
The more the user accelerate car (rotate forward), the smaller the steering angle becomes, which is against instinct of how steering wheel works.\
In my opinion, the steering angle should always be the angle on the plane where the wheel resides, and the plane cannot be assumed to be always vertical in this case.

To restore true steering angle, here's the formula:
```
let x = acceleration angle (around X, from sensor)
let y0 = steering angle (from sensor)
let y1 = true steering angle (to compute)
y1 = arcsin(sin(y0) / cos(x))
```
The math comes from some intense 3D imagination process in head 🤣 If you know any tools that can easily create & rotate lines in 3D space, please let me know!
