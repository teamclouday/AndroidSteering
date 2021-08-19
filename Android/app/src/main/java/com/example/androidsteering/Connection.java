// component for communicating with PC side server

package com.example.androidsteering;

import android.app.AlertDialog;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothClass;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.wifi.WifiManager;
import android.os.Process;
import android.text.InputType;
import android.text.format.Formatter;
import android.util.Log;
import android.util.Patterns;
import android.widget.EditText;
import android.widget.RadioGroup;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.UnknownHostException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
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
        private boolean updatePitch = true;
        private boolean updateRoll = true;

        public synchronized void addData(float pitch, float roll)
        {
            if(!running) return;
            if(updatePitch) buff.add(new Motion.MyMove(false, 0, pitch));
            if(updateRoll) buff.add(new Motion.MyMove(false, 1, roll));
            while(buff.size() > MAX_SIZE)
                buff.remove(0);
        }

        public synchronized void addData(int status, float val)
        {
            if(!running) return;
            buff.add(new Motion.MyMove(false, status, val));
            while(buff.size() > MAX_SIZE)
                buff.remove(0);
        }

        public synchronized void addData(MotionButton button)
        {
            if(!running) return;
            buff.add(new Motion.MyMove(true, button.getVal(), 0.0f));
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
        public synchronized void setUpdatePitch(boolean val){updatePitch = val;}
        public synchronized void setUpdateRoll(boolean val){updateRoll = val;}
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

    // wifi components
    private Socket wifiSocket;
    private Thread wifiThread;
    public String wifiAddress = "192.168.";
    private final int wifiPort = 55555;

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
                        }catch(InterruptedException e){break;}
                        continue;
                    }
                    streamOut.writeInt(10086);
                    streamOut.writeBoolean(data.MotionButton);
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
        // prepare IP address
        WifiManager wm = (WifiManager) mainActivity.getApplicationContext().getSystemService(Context.WIFI_SERVICE);
        try{
            InetAddress address = InetAddress.getByAddress(
                    ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(wm.getConnectionInfo().getIpAddress()).array()
            );
            wifiAddress = address.getHostAddress();
        }catch(UnknownHostException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[connectWifi] -> " + e.getMessage());
        }
        // get IP address input
        AtomicBoolean decided = new AtomicBoolean(false);
        AtomicBoolean validAddress = new AtomicBoolean(false);
        mainActivity.runOnUiThread(() -> {
            // create alert dialog
            AlertDialog.Builder builder = new AlertDialog.Builder(mainActivity);
            builder.setTitle("Set/Verify IP address (Auto-filled)");
            // create input text
            final EditText ipInput = new EditText(mainActivity);
            ipInput.setText(wifiAddress);
            ipInput.setInputType(InputType.TYPE_CLASS_PHONE);
            builder.setView(ipInput);
            // set buttons
            builder.setPositiveButton("OK", (dialog, which) -> {
                if(Patterns.IP_ADDRESS.matcher(ipInput.getText().toString()).matches())
                {
                    if(!ipInput.getText().toString().isEmpty())
                    {
                        wifiAddress = ipInput.getText().toString();
                        validAddress.set(true);
                    }
                    decided.set(true);
                }
                decided.set(true);
            });
            builder.setNegativeButton("Cancel", (dialog, which) -> {
                dialog.cancel();
                decided.set(true);
            });
            builder.show();
        });
        while(!decided.get())
        {
            try{
                Thread.sleep(50);
            }catch(InterruptedException e){break;}
        }
        if(!validAddress.get()) return "Invalid IP address";
        // validate connection
        if(!testConnection()) return "Cannot connect to " + wifiAddress;
        // create connection
        if(wifiSocket != null || wifiThread != null) disconnect();
        wifiSocket = new Socket();
        try{
            wifiSocket.bind(null);
        }catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[connectWifi] Cannot bind socket -> " + e.getMessage());
        }
        try{
            wifiSocket.connect(new InetSocketAddress(wifiAddress, wifiPort), (int) MAX_WAIT_TIME);
        }catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[connectWifi] Cannot connect socket -> " + e.getMessage());
        }
        connected = true;
        // start thread loop
        wifiThread = new Thread(() -> {
            Process.setThreadPriority(Process.THREAD_PRIORITY_FOREGROUND);
            try
            {
                DataOutputStream streamOut = new DataOutputStream(wifiSocket.getOutputStream());
                while(running)
                {
                    Motion.MyMove data = globalBuffer.getData();
                    if(data == null)
                    {
                        try{
                            Thread.sleep(50);
                        }catch(InterruptedException e){break;}
                        continue;
                    }
                    streamOut.writeInt(10086);
                    streamOut.writeBoolean(data.MotionButton);
                    streamOut.writeInt(data.MotionStatus);
                    streamOut.writeFloat(data.data);
                }
                streamOut.close();
            }catch(IOException e)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[wifiThread] -> " + e.getMessage());
            }finally{
                connected = false;
                unlockRadioGroup();
            }
        });
        running = true;
        wifiThread.start();
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
            if(wifiThread != null)
            {
                running = false;
                try{
                    wifiThread.join(MAX_WAIT_TIME);
                }catch(InterruptedException e)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[disconnect](wifi) Thread stopped -> " + e.getMessage());
                }finally {
                    wifiThread = null;
                }
            }
            if(wifiSocket != null)
            {
                try{
                    wifiSocket.close();
                }catch(IOException e)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[disconnect](wifi) Cannot close socket -> " + e.getMessage());
                }finally {
                    wifiSocket = null;
                }
            }
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

    // test connection for wifi IP
    private boolean testConnection()
    {
        Socket tmp = new Socket();
        try{
            tmp.bind(null);
        }catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) -> " + e.getMessage());
            return false;
        }
        try{
            tmp.connect(new InetSocketAddress(wifiAddress, wifiPort), (int) MAX_WAIT_TIME);
        }catch(IOException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) -> " + e.getMessage());
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
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) validationThread -> " + e.getMessage());
            }
        });
        try
        {
            validationThread.start();
            validationThread.join(MAX_WAIT_TIME);
            if(validationThread.isAlive())
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) validationThread exceeds max timeout");
                try{tmp.close();}
                catch(Exception any)
                {
                    Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) -> " + any.getMessage());
                }
                return false;
            }
        }catch(InterruptedException e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) validationThread exceeds max timeout -> " + e.getMessage());
            try{tmp.close();}
            catch(Exception any)
            {
                Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) -> " + any.getMessage());
            }
            return false;
        }
        try{tmp.close();}
        catch(Exception e)
        {
            Log.d(mainActivity.getString(R.string.logTagConnection), "[testConnection](Wifi) -> " + e.getMessage());
        }
        return true;
    }
}
