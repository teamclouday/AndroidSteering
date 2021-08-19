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