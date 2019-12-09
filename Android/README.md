# Android Steering Wheel (Android Application)  

This folder contains code for the android application, which will record the motion signals and send to the connected computer via bluetooth.  

------

### Motion Signals  

The application will read orientation by using the accelerator sensor and magnetic field sensor.  
However, the pitch and roll values are converted.  

**Pitch**:  
Angle about the wheel. Turning left will result in positive values. Turning right will result in negative values.  

**Roll**:  
Angle about the car acceleration. Lean forward will result in a negative value (accelerate). Lean backward will result in a positive value (decelerate).  

------

### Bluetooth Signals  

The application will try to connect a service with unique UUID via Bluetooth. The service will be the Windows side program.  
Then the bluetooth service (on another thread) will start reading data stored in a global buffer and send them through Bluetooth.  
The data will contain:  
1. `0x7FFFFFF` as first 4 byte if the connection has just been established  
2. `10086` as the data pack seperator (4 ints per pack).  
3. rest data (3 ints) that contains type, detailed type and actual data.  