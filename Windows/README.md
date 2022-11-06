# Steering Wheel (Windows side)

Android Steering Wheel Project (Windows Application folder) written in WPF.

------

### Window  

__Main Window__:  
Displays most of useful UI  

__Configure Window__:  
Used to configure controller buttons and axis  

------

### Controller Signal Processing  

* For each button press signal, start a new thread to trigger it  
* For each motion signal (acceleration / steering), convert value and apply `SmoothStep` function  
* Acceleraion (Roll):  
  * Real range is `(-90, 90)`, with resting mode in range `(-40, -30)`  
  * Interpreted range is `(-80, 10)`  
    Overflowing values are clamped  
* Steering (Pitch)  
  * Real range is `(-90, 90)`, with no resting mode  
  * Interpreted range is `(-45, 45)`  
    Overflowing values are clamped  

------

### Dependencies  
* `32feet.net` for bluetooth connection  
* `vJoyInterfaceWrap.dll` for vjoy interface  