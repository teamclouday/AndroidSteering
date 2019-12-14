# Android Steering Wheel  

This program tries to emulate a game steering wheel by using an Android phone  

------

### User Guide  
Android Side:  
1. In order to run properly, should turn on bluetooth.  
2. Hold phone in hand, learn the 4 basic controls by moving around while reading the steering and acceleration info on screen.  
3. Tap Show All button to access extra buttons (Same Configuration as Xbox Controller).  

Windows Side:  
1. In order to run properly, need to install vJoy driver (version 2.18). Please go to [this link](https://sourceforge.net/projects/vjoystick/files/Beta%202.x/2.1.8.39-270518/) and download `vJoySetup.exe`. Please check all 4 programs during installation.  
2. After install, configure the device first. Open `Configure vJoy` and make sure no other program is capturing the controller (like a game or steam). Set Number of Buttons to at least 11. Check all the axes. Choose 4 directions in POV and set number to at least 1.  
3. Also turn on the bluetooth.  
4. Start the program, it will show an icon in system tray.  
5. To setup all buttons for a game (or maybe for steam controller setup), click the `Setup Controller` in the icon menu.  
6. To monitor the current status or customize motion values, click `Status Monitor`.  
7. To exit the program completely, click `Exit`   

**Important**  
Before run, make sure your phone and pc has already been paired before.  
The program will only scan device from paired list.  

------

### DEMOS  

This program has been tested on [Assetto Corsa](https://store.steampowered.com/app/244210/Assetto_Corsa/) and [Euro Truck Simulator 2](https://store.steampowered.com/app/227300/Euro_Truck_Simulator_2/).  
It works pretty well, by enabling auto-shifting.  

* Assetto Corsa [Demo Video](https://www.bilibili.com/video/av79162105/)  

Additional guide to Assetto Corsa. In order to reach perfect experience. Load the Configuration Preset for Xbox Controllers and change steering gamma to 1 (1:1) in the advanced setting.  

------

### Dependencies  
1. vJoy (both driver and dll)  
2. 32feet.net (a bluetooth wrapper for Windows C#)  

------

### Project Purpose  
I want to start somewhere learning Android related programming. Also I've been dreaming of driving in a game on PC using my phone (just like playing racing cars in a smart phone). Therefore, this project is an excellent opportunity.  

### Code Tool Choices  
For Android, I choose to code on Android Studio using Java, because it is the most official way of developing an Android app.  
For Windows, I code on Visual Studio 2019 using C#. This is actually related to the vJoy api interfaces (only C++ or C#). The reason I didn't choose C++ is that the published program is apperantly targeting Windows, and I also need to develop a GUI. By using C#, the codes are simpler and the GUI can be easily designed (WinForm).  

------

### Potential Improvements  
1. May update the algorithm of Android code, and send less data every second to save more power.  
2. May develop a Xbox emulator using the same communication approach.  
3. May learn to add USB protocol as a alternative for Bluetooth.  
4. May add additional functions to improve the steering stability.  