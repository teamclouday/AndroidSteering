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
import android.widget.Toast;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.Set;
import java.util.UUID;

enum BluetoothStatus
{
    NONE,
    SEARCHING,
    CONNECTED
}

public class Connection
{
    class MyBuffer
    {
        private final int MAX_SIZE = 50;
        private ArrayList<Motion.MyMove> buff = new ArrayList<>();

        public synchronized void addData(MotionSteering s1, MotionAcceleration s2, float pitch, float roll)
        {
            if(buff.size() >= MAX_SIZE) return;
            buff.add(new Motion.MyMove(0, s1.ordinal(), pitch));
            buff.add(new Motion.MyMove(1, s2.ordinal(), roll));
        }

        public synchronized void addData(MotionButton button)
        {
            if(buff.size() >= MAX_SIZE) return;
            buff.add(new Motion.MyMove(2, button.ordinal(), 0.0f));
        }

        public synchronized Motion.MyMove getData()
        {
            if(buff.size() <= 0) return null;
            return buff.remove(0);
        }
    }

    static MyBuffer buffer;

    private final UUID TARGET_UUID = UUID.fromString("a7bda841-7dbc-4179-9800-1a3eff463f1c");
    private final int MAX_TRANSACTIONS_PER_CONNECTION = 10;
    private final int CONNECTION_SEPERATOR = 0x7FFFFFFF;

    private Context activityContext;

    private BluetoothSocket mySocket;
    private BluetoothAdapter myAdapter;
    private BluetoothDevice targetDevice;
    private BluetoothStatus myStatus = BluetoothStatus.NONE;

    private BroadcastReceiver myBluetoothStatusReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if(BluetoothAdapter.ACTION_STATE_CHANGED.equals(action))
            {
                if(intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, -1) == BluetoothAdapter.STATE_OFF)
                {
                    disconnect();
                }
            }
        }
    };

    private BroadcastReceiver myReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
//                if(BluetoothDevice.ACTION_FOUND.equals(action))
//                {
//                    Log.d("MyBthService", "onReceive: Hello");
//                    BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);
//                    if(potentialDevices.size() > 0)
//                    {
//                        for(MyBluetoothDevice d : potentialDevices)
//                        {
//                            if(d.device.equals(device))
//                            {
//                                d.isAlive = true;
//                            }
//                        }
//                    }
//                }
            if(BluetoothDevice.ACTION_ACL_DISCONNECTED.equals(action))
            {
                disconnect();
            }
            else if(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED.equals(action))
            {
                disconnect();
            }
//                else if(BluetoothAdapter.ACTION_DISCOVERY_FINISHED.equals(action))
//                {
//                    updateStatus(BluetoothStatus.NONE);
//                    findTargetDevice();
//                }
        }
    };

    private ArrayList<BluetoothDevice> potentialDevices;

    private boolean initSuccess = true;

//        class MyBluetoothDevice
//        {
//            public BluetoothDevice device;
//            public boolean isAlive = false;
//            MyBluetoothDevice(BluetoothDevice d)
//            {
//                device = d;
//            }
//        }

    public Connection(Context context)
    {
        activityContext = context;
        activityContext.registerReceiver(myBluetoothStatusReceiver, new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED));
        myAdapter = BluetoothAdapter.getDefaultAdapter();
        updateStatus(BluetoothStatus.NONE);
        updatePotential();
    }

    public void updatePotential()
    {
        Set<BluetoothDevice> pairedDevices = myAdapter.getBondedDevices();
        potentialDevices = new ArrayList<>();
        if (pairedDevices.size() > 0)
        {
            for(BluetoothDevice device : pairedDevices)
            {
                if(device.getBluetoothClass().getMajorDeviceClass() == BluetoothClass.Device.Major.COMPUTER)
                {
                    potentialDevices.add(device);
                }
            }
            try
            {
                activityContext.unregisterReceiver(myReceiver);
            } catch(IllegalArgumentException e)
            {
                // Log.d("MyBthService", "updatePotential: receiver already unregistered");
            }
            IntentFilter filter = new IntentFilter(BluetoothAdapter.ACTION_DISCOVERY_FINISHED);
            // filter.addAction(BluetoothDevice.ACTION_FOUND);
            activityContext.registerReceiver(myReceiver, filter);
            updateStatus(BluetoothStatus.SEARCHING);
            findTargetDevice();
//                myAdapter.startDiscovery();
        }
        else
        {
            Toast.makeText(activityContext, "Please pair a computer and try again", Toast.LENGTH_LONG).show();
            initSuccess = false;
        }
    }

    public boolean isCreated()
    {
        return initSuccess;
    }

    private void findTargetDevice()
    {
        for(BluetoothDevice d : potentialDevices)
        {
            if(testConnection(d))
            {
                targetDevice = d;
                updateStatus(BluetoothStatus.NONE);
                break;
            }
        }
        if(targetDevice == null)
        {
            Toast.makeText(activityContext, "None of the computers can be connected\nPlease check the receiver on computer", Toast.LENGTH_LONG).show();
            initSuccess = false;
        }
        else
        {
            try
            {
                activityContext.unregisterReceiver(myReceiver);
            } catch(IllegalArgumentException e)
            {
                // Log.d("MyBthService", "findTargetDevice: receiver already unregistered");
            }
        }
    }

    public boolean connect()
    {
        if(targetDevice == null) return false;
        if(mySocket != null) disconnect();
        try
        {
            mySocket = targetDevice.createInsecureRfcommSocketToServiceRecord(TARGET_UUID);
        } catch(IOException e)
        {
            Toast.makeText(activityContext, "Failed to connect device: " + targetDevice.getName(), Toast.LENGTH_LONG).show();
            mySocket = null;
            return false;
        }
        try
        {
            mySocket.connect();
        } catch(IOException e)
        {
            Toast.makeText(activityContext, "Failed to connect device: " + targetDevice.getName(), Toast.LENGTH_LONG).show();
            mySocket = null;
            return false;
        }
        try
        {
            activityContext.unregisterReceiver(myReceiver);
        } catch(IllegalArgumentException e)
        {
            // Log.d("MyBthService", "connect: receiver already unregistered");
        }

        ByteBuffer buffer = ByteBuffer.allocate(4);
        buffer.putInt(CONNECTION_SEPERATOR);
        write(buffer.array());

        IntentFilter filter = new IntentFilter(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED);
        filter.addAction(BluetoothDevice.ACTION_ACL_DISCONNECTED);
        activityContext.registerReceiver(myReceiver, filter);
        updateStatus(BluetoothStatus.CONNECTED);
        return true;
    }

    public void writeData()
    {
        if(myStatus != BluetoothStatus.CONNECTED) return;
        ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
        for(int i = 0; i < MAX_TRANSACTIONS_PER_CONNECTION; i++)
        {
            Motion.MyMove data = buffer.getData();
            if(data == null) break;
            ByteBuffer bb = ByteBuffer.allocate(4);
            bb.putInt(10086);
            outputStream.write(bb.array(), 0, 4); // used as data separator and validator
            bb.clear();
            bb.putInt(data.MotionType);
            outputStream.write(bb.array(), 0, 4);
            bb.clear();
            bb.putInt(data.MotionStatus);
            outputStream.write(bb.array(), 0, 4);
            bb.clear();
            bb.putFloat(data.data);
            outputStream.write(bb.array(), 0, 4);
        }
        write(outputStream.toByteArray());
    }

    private void write(byte[] data)
    {
        OutputStream output;
        try
        {
            output = mySocket.getOutputStream();
        } catch (IOException e) {
            // Log.d("MyBthService", "writeData: failed to get output stream from socket");
            updateStatus(BluetoothStatus.NONE);
            return;
        }
        try
        {
            output.write(data);
        } catch (IOException e) {
            // Log.d("MyBthService", "writeData: failed to write to output stream");
            updateStatus(BluetoothStatus.NONE);
        }
    }

    public void disconnect()
    {
        updateStatus(BluetoothStatus.NONE);
        try
        {
            activityContext.unregisterReceiver(myReceiver);
        } catch(IllegalArgumentException e)
        {
            // Log.d("MyBthService", "disconnect: receiver already unregistered");
        }
        if(mySocket == null) return;

        ByteBuffer buffer = ByteBuffer.allocate(4);
        buffer.putInt(CONNECTION_SEPERATOR);
        write(buffer.array());

        try
        {
            mySocket.close();
        } catch(IOException e)
        {
            // Log.d("MyBthService", "Cannot close socket");
            mySocket = null;
            return;
        }
        mySocket = null;
    }

    public void destroy()
    {
        disconnect();
        try
        {
            activityContext.unregisterReceiver(myBluetoothStatusReceiver);
        } catch(IllegalArgumentException e)
        {
            // Log.d("MyBthService", "destroy: receiver already unregistered");
        }
    }

    private boolean testConnection(BluetoothDevice device)
    {
        BluetoothSocket tmp;
        try
        {
            tmp = device.createRfcommSocketToServiceRecord(TARGET_UUID);
        } catch(IOException e)
        {
            // Log.d("MyBthService", "Cannot create socket");
            return false;
        }
        try
        {
            tmp.connect();
        } catch(IOException e)
        {
            // Log.d("MyBthService", "Cannot connect to device: " + device.getName());
            return false;
        }
        try
        {
            tmp.close();
        } catch(IOException e)
        {
            // Log.d("MyBthService", "Cannot disconnect device: " + device.getName());
            return false;
        }
        return true;
    }

    private synchronized void updateStatus(BluetoothStatus newStatus)
    {
        myStatus = newStatus;
    }

    public synchronized BluetoothStatus readStatus()
    {
        return myStatus;
    }
}
