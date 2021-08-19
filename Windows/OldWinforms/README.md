# Android Steering Wheel (Windows Application)  

This folder contains code for the windows program, which will catch the signals sent by android phone and drive the vJoy to simulate a game steering wheel

------

### Program Logics  

3 main threads:  
1. system tray icon thread  
2. Bluetooth server thread  
3. Wheel driver thread  

#### System Tray Thread  
This thread will create new thread for 2 UIs if needed, and will also manage when to exit the whole program  

#### Bluetooth Thread  
This thread will start the Bluetooth listener with unique UUID, try to accept a potential client.  
If connected, check the received data to determine whether the connected client is going to send useful data.  
Specifically, the input stream will contain `0x7FFFFFFF` as first 4 bytes to indicate a incoming data flow, `0x7FFFFFFF` show again to indicate the closing of the input stream.  
For the input stream itself, every 16 bytes (4 int) is regarded as a pack. The started int is always `10086` and the rest 3 ints are real input data. Therefore, the code will check every 16 bytes to make sure the data is received in valid order.  

#### Wheel Thread  
This thread will aquire a vJoy device and check if the device is valid.  
Then it will starts reading data from a global buffer, where data are previously stored by the bluetooth thread. The read and write on global buffer uses object lock to ensure data safety.  
Then for each instruction, trigger the device driver to send emulated signals.  