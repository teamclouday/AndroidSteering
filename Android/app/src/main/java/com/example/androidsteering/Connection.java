// component for communicating with PC side server

package com.example.androidsteering;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothClass;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Process;
import android.util.Log;
import android.widget.RadioGroup;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicBoolean;

enum ConnectionMode
{
    Bluetooth,
    Wifi
}

public class Connection
{
    static class MyBuffer
    {
        private final int MAX_SIZE = 50;
        private final ArrayList<Motion.MyMove> buff = new ArrayList<>();
        private boolean running = false;

        public synchronized void addData(float pitch, float roll)
        {
            if(!running) return;
            buff.add(new Motion.MyMove(0, 0, pitch));
            buff.add(new Motion.MyMove(0, 1, roll));
            while(buff.size() > MAX_SIZE)
                buff.remove(0);
        }

        public synchronized void addData(MotionButton button)
        {
            if(!running) return;
            buff.add(new Motion.MyMove(1, button.ordinal(), 0.0f));
            while(buff.size() > MAX_SIZE)
                buff.remove(0);
        }

        public synchronized Motion.MyMove getData()
        {
            if(buff.size() <= 0) return null;
            return buff.remove(0);
        }

        public synchronized void turnOn(){running = true;}
        public synchronized void turnOff(){running = false;}
    }

    private final MyBuffer globalBuffer;

    private final UUID TARGET_UUID = UUID.fromString("a7bda841-7dbc-4179-9800-1a3eff463f1c");
    private final int DEVICE_CHECK_DATA = 123456;
    private final int DEVICE_CHECK_EXPECTED = 654321;
    private final long MAX_WAIT_TIME = 1500L;

    private final MainActivity mainActivity;

    public boolean connected = false;
    public ConnectionMode connectionMode = ConnectionMode.Bluetooth;
    private boolean running = false;

    // bluetooth components
    private final BluetoothAdapter bthAdapter;
    private BluetoothSocket bthSocket;
    private final BroadcastReceiver bthReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if(BluetoothAdapter.ACTION_STATE_CHANGED.equals(action))
            {
                if(intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR) == BluetoothAdapter.STATE_TURNING_OFF)
                {
                    disconnect();
                }
            }
            else if(BluetoothDevice.ACTION_ACL_DISCONNECTED.equals(action) ||
                    BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED.equals(action))
                disconnect();
        }
    };
    private Thread bthThread;

    public Connection(MainActivity activity, MyBuffer buffer)
    {
        mainActivity = activity;
        globalBuffer = buffer;
        bthAdapter = BluetoothAdapter.getDefaultAdapter();
    }

    // general connect
    public String connect()
    {
        if(connected) disconnect();
        if(connectionMode == ConnectionMode.Bluetooth) return connectBluetooth();
        else return connectWifi();
    }

    // connect to bluetooth
    private String connectBluetooth()
    {
        // register receiver
        IntentFilter filter = new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED);
        filter.addAction(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED);
        filter.addAction(BluetoothDevice.ACTION_ACL_DISCONNECTED);
        mainActivity.registerReceiver(bthReceiver, filter);
        // select target device
        BluetoothDevice bthDevice = null;
        for(BluetoothDevice device : bthAdapter.getBondedDevices())
        {
            if(device.getBluetoothClass().getMajorDeviceClass() == BluetoothClass.Device.Major.COMPUTER)
            {
                if(testConnection(device))
                {
                    bthDevice = device;
                    break;
                }
            }
        }
        // create connection
        if(bthDevice == null) return "Failed to find target bluetooth PC";
        if(bthSocket != null || bthThread != null) disconnect();
        try {
            bthSocket = bthDevice.createInsecureRfcommSocketToServiceRecord(TARGET_UUID);
        } catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[connectBluetooth] Cannot create socket -> " + e.getMessage());
            bthSocket = null;
            return "Failed to init bluetooth";
        }
        try{
            bthSocket.connect();
        }catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[connectBluetooth] Cannot connect socket -> " + e.getMessage());
            bthSocket = null;
            return "Failed to connect bluetooth";
        }
        connected = true;
        // start thread loop
        bthThread = new Thread(() -> {
            Process.setThreadPriority(Process.THREAD_PRIORITY_FOREGROUND);
            try
            {
                DataOutputStream streamOut = new DataOutputStream(bthSocket.getOutputStream());
                while(running)
                {
                    Motion.MyMove data = globalBuffer.getData();
                    if(data == null)
                    {
                        try{
                            Thread.sleep(50);
                        }catch(InterruptedException e){}
                        continue;
                    }
                    streamOut.writeInt(10086);
                    streamOut.writeInt(data.MotionType);
                    streamOut.writeInt(data.MotionStatus);
                    streamOut.writeFloat(data.data);
                }
                streamOut.close();
            }catch(IOException e)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[bthThread] -> " + e.getMessage());
            }finally{
                connected = false;
                unlockRadioGroup();
            }
        });
        running = true;
        bthThread.start();
        return "";
    }

    // connect to wifi
    private String connectWifi()
    {
        return "";
    }

    // disconnect
    public void disconnect()
    {
        if(connectionMode == ConnectionMode.Bluetooth)
        {
            if(bthThread != null)
            {
                running = false;
                try{
                    bthThread.join(MAX_WAIT_TIME);
                }catch(InterruptedException e)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[disconnect](bluetooth) Thread stopped -> " + e.getMessage());
                }finally {
                    bthThread = null;
                }
            }
            if(bthSocket != null)
            {
                try{
                    bthSocket.close();
                }catch(IOException e)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[disconnect](bluetooth) Cannot close socket -> " + e.getMessage());
                }finally {
                    bthSocket = null;
                }
            }
        }
        else if(connectionMode == ConnectionMode.Wifi)
        {

        }
        connected = false;
        unlockRadioGroup();
    }

    private void unlockRadioGroup()
    {
        mainActivity.runOnUiThread(() -> {
            try{
                RadioGroup group = mainActivity.findViewById(R.id.radioGroup);
                group.setEnabled(true);
            }catch(Exception e)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[unlockRadioGroup] -> " + e.getMessage());
            }
        });
    }

    // test bluetooth device
    private boolean testConnection(BluetoothDevice device)
    {
        BluetoothSocket tmp;
        try
        {
            tmp = device.createRfcommSocketToServiceRecord(TARGET_UUID);
        } catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) Cannot create socket");
            return false;
        }
        try
        {
            tmp.connect();
        } catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) Cannot connect to device: " + device.getName());
            return false;
        }
        AtomicBoolean isValid = new AtomicBoolean(false);
        Thread validationThread = new Thread(() -> {
            try
            {
                DataOutputStream streamOut = new DataOutputStream(tmp.getOutputStream());
                streamOut.writeInt(DEVICE_CHECK_DATA);
                streamOut.flush();
                DataInputStream streamIn = new DataInputStream(tmp.getInputStream());
                if(streamIn.readInt() == DEVICE_CHECK_EXPECTED) isValid.set(true);
            }catch(Exception e)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) validationThread -> " + e.getMessage());
            }
        });
        try
        {
            validationThread.start();
            validationThread.join(MAX_WAIT_TIME);
            if(validationThread.isAlive())
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) validationThread exceeds max timeout");
                try{tmp.close();}
                catch(Exception any)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) -> " + any.getMessage());
                }
                return false;
            }
        }catch(InterruptedException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) validationThread exceeds max timeout -> " + e.getMessage());
            try{tmp.close();}
            catch(Exception any)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) -> " + any.getMessage());
            }
            return false;
        }
        try{tmp.close();}
        catch(Exception any)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Bluetooth) -> " + any.getMessage());
        }
        return true;
    }
}
