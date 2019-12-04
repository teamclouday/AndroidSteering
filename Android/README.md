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

