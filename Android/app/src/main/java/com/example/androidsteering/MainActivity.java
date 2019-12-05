package com.example.androidsteering;

import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.pm.ActivityInfo;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.os.Handler;
import android.util.Log;
import android.view.View;
import android.view.WindowManager;
import android.widget.TextView;
import android.widget.Toast;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.Objects;
import java.util.Set;
import java.util.UUID;

enum MotionSteering
{
    LEFT,
    NONE,
    RIGHT
}

enum MotionAcceleration
{
    FORWARD,
    NONE,
    BACKWARD
}

enum BluetoothStatus
{
    NONE,
    CONNECTED
}

public class MainActivity extends AppCompatActivity
{
    private TextView txtViewDebug;
    private TextView txtViewBTH;

    private MySensorService serviceSensor;
    private MyBthService serviceBTH;

    private Handler UIHandler;
    private Runnable UIRunner;

    private Handler bthHandler;
    private Runnable bthRunner;

    private MyBuffer localBuffer;

    @Override
    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        Objects.requireNonNull(getSupportActionBar()).hide();
        setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);

        checkBTH();
        checkSensor();

        serviceSensor = new MySensorService();
        localBuffer = new MyBuffer();

        UIHandler = new Handler();
        bthHandler = new Handler();

        setContentView(R.layout.main);

        txtViewDebug = findViewById(R.id.textViewDebug);
        txtViewBTH = findViewById(R.id.textViewBTH);

        serviceSensor.start();

        UIRunner = new Runnable() {
            @Override
            public void run() {
                String motionSteer;
                switch(serviceSensor.readMotionSteer())
                {
                    case LEFT:
                        motionSteer = "Left";
                        break;
                    case RIGHT:
                        motionSteer = "Right";
                        break;
                    default:
                        motionSteer = "None";
                        break;
                }
                motionSteer += "><" + serviceSensor.readPitch();
                String motionAcc;
                switch(serviceSensor.readMotionAcc())
                {
                    case FORWARD:
                        motionAcc = "Forward";
                        break;
                    case BACKWARD:
                        motionAcc = "Backward";
                        break;
                    default:
                        motionAcc = "None";
                        break;
                }
                motionAcc += "><" + serviceSensor.readRoll();
                txtViewDebug.setText(String.format("Steering: %s\nAcceleration: %s", motionSteer, motionAcc));
                UIHandler.postDelayed(this, 20);
            }
        };

        bthRunner = new Runnable() {
            @Override
            public void run() {
                if(serviceBTH == null)
                {
                    serviceBTH = new MyBthService();
                }
                if(!serviceBTH.isCreated())
                {
                    serviceBTH.disconnect();
                    serviceBTH = null;
                    bthHandler.removeCallbacksAndMessages(this);
                    findViewById(R.id.btRetry).setVisibility(View.VISIBLE);
                    findViewById(R.id.textViewBTH).setVisibility(View.GONE);
                    return;
                }
                else
                {
                    if(serviceBTH.readStatus() != BluetoothStatus.CONNECTED)
                    {
                        if(!serviceBTH.connect())
                        {
                            serviceBTH.disconnect();
                            serviceBTH = null;
                            bthHandler.removeCallbacksAndMessages(this);
                            findViewById(R.id.btRetry).setVisibility(View.VISIBLE);
                            findViewById(R.id.textViewBTH).setVisibility(View.GONE);
                            return;
                        }
                    }
                    serviceBTH.writeData();
                }
                bthHandler.postDelayed(this, 20);
            }
        };
    }

    @Override
    public void onResume()
    {
        super.onResume();
        serviceSensor.start();
        UIHandler.postDelayed(UIRunner, 0);
        bthHandler.postDelayed(bthRunner, 0);
    }

    @Override
    public void onPause()
    {
        super.onPause();
        serviceSensor.stop();
        UIHandler.removeCallbacksAndMessages(UIRunner);
        bthHandler.removeCallbacksAndMessages(bthRunner);
        if(serviceBTH != null)
            serviceBTH.disconnect();
    }

    @Override
    public void onDestroy()
    {
        if(serviceBTH != null)
            serviceBTH.disconnect();
        super.onDestroy();
    }

    private void checkBTH()
    {
        BluetoothAdapter test = BluetoothAdapter.getDefaultAdapter();
        if(test == null)
        {
            new AlertDialog.Builder(this)
                    .setTitle("Not Compatible")
                    .setMessage("Your phone does not support Bluetooth")
                    .setPositiveButton("OK", new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialog, int which) {
                            System.exit(0);
                        }
                    })
                    .show();
        }

        if(!test.isEnabled())
        {
            Intent enableBTH = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
            startActivityForResult(enableBTH, 1);
        }
    }

    private void checkSensor()
    {
        SensorManager test = (SensorManager)getSystemService(Context.SENSOR_SERVICE);
        if(test == null || test.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD) == null || test.getDefaultSensor(Sensor.TYPE_ACCELEROMETER) == null)
        {
            new AlertDialog.Builder(this)
                    .setTitle("Not Compatible")
                    .setMessage("Your phone does not have required sensors")
                    .setPositiveButton("OK", new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialog, int which) {
                            System.exit(0);
                        }
                    })
                    .show();
        }
    }

    public void retryBTH(View view)
    {
        bthHandler.postDelayed(bthRunner, 0);
        findViewById(R.id.btRetry).setVisibility(View.GONE);
        findViewById(R.id.textViewBTH).setVisibility(View.VISIBLE);
    }

    class MyBthService
    {
        private final UUID TARGET_UUID = UUID.fromString("a7bda841-7dbc-4179-9800-1a3eff463f1c");
        private final int MAX_TRANSACTIONS_PER_CONNECTION = 40;

        private BluetoothSocket mySocket;
        private BluetoothAdapter myAdapter;
        private BluetoothDevice targetDevice;
        private BluetoothStatus myStatus = BluetoothStatus.NONE;

        private BroadcastReceiver myReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                String action = intent.getAction();
                if(BluetoothAdapter.ACTION_STATE_CHANGED.equals(action))
                {
                    if(intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, -1) == BluetoothAdapter.STATE_OFF)
                    {
                        disconnect();
                        Intent enableBTH = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
                        startActivityForResult(enableBTH, 1);
                    }
                }
                else if(BluetoothDevice.ACTION_FOUND.equals(action))
                {
                    BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);
                    if(potentialDevices.size() > 0)
                    {
                        for(MyBluetoothDevice d : potentialDevices)
                        {
                            if(d.device == device)
                            {
                                d.isAlive = true;
                            }
                        }
                    }
                }
                else if(BluetoothDevice.ACTION_ACL_DISCONNECTED.equals(action))
                {
                    disconnect();
                }
                else if(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED.equals(action))
                {
                    disconnect();
                }
            }
        };

        private ArrayList<MyBluetoothDevice> potentialDevices;

        private boolean initSuccess = true;

        class MyBluetoothDevice
        {
            public BluetoothDevice device;
            public boolean isAlive = false;
            MyBluetoothDevice(BluetoothDevice d)
            {
                device = d;
            }
        }

        public MyBthService() {
            myAdapter = BluetoothAdapter.getDefaultAdapter();
            updateStatus(BluetoothStatus.NONE);
            updatePotential();
        }

        private void updatePotential()
        {
            Set<BluetoothDevice> pairedDevices = myAdapter.getBondedDevices();
            potentialDevices = new ArrayList<>();
            if (pairedDevices.size() > 0)
            {
                for(BluetoothDevice device : pairedDevices)
                {
                    if(device.getBluetoothClass().getMajorDeviceClass() == 256)
                    {
                        potentialDevices.add(new MyBluetoothDevice(device));
                    }
                }
                try
                {
                    unregisterReceiver(myReceiver);
                } catch(IllegalArgumentException e)
                {
                    Log.d("MyBthService", "updatePotential: receiver already unregistered");
                }

                registerReceiver(myReceiver, new IntentFilter(BluetoothDevice.ACTION_FOUND));
                registerReceiver(myReceiver, new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED));
                findTargetDevice();
            }
            else
            {
                Toast.makeText(getApplicationContext(), "Please pair a computer and try again", Toast.LENGTH_LONG).show();
                initSuccess = false;
            }
        }

        public boolean isCreated()
        {
            return initSuccess;
        }

        private void findTargetDevice()
        {
            myAdapter.startDiscovery();
            for(MyBluetoothDevice d : potentialDevices)
            {
                if(d.isAlive)
                {
                    if(testConnection(d.device))
                    {
                        targetDevice = d.device;
                        break;
                    }
                }
            }
            if(targetDevice == null)
            {
                Toast.makeText(getApplicationContext(), "None of the computers can be connected\nPlease check the receiver on computer", Toast.LENGTH_LONG).show();
                initSuccess = false;
            }
            else
            {
                try
                {
                    unregisterReceiver(myReceiver);
                } catch(IllegalArgumentException e)
                {
                    Log.d("MyBthService", "findTargetDevice: receiver already unregistered");
                }
                registerReceiver(myReceiver, new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED));
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
                Toast.makeText(getApplicationContext(), "Failed to connect device: " + targetDevice.getName(), Toast.LENGTH_LONG).show();
                mySocket = null;
                return false;
            }
            try
            {
                mySocket.connect();
            } catch(IOException e)
            {
                Toast.makeText(getApplicationContext(), "Failed to connect device: " + targetDevice.getName(), Toast.LENGTH_LONG).show();
                mySocket = null;
                return false;
            }
            try
            {
                unregisterReceiver(myReceiver);
            } catch(IllegalArgumentException e)
            {
                Log.d("MyBthService", "connect: receiver already unregistered");
            }

            registerReceiver(myReceiver, new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED));
            registerReceiver(myReceiver, new IntentFilter(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED));
            registerReceiver(myReceiver, new IntentFilter(BluetoothDevice.ACTION_ACL_DISCONNECTED));
            updateStatus(BluetoothStatus.CONNECTED);
            return true;
        }

        public void writeData()
        {
            if(myStatus != BluetoothStatus.CONNECTED) return;
            for(int i = 0; i < MAX_TRANSACTIONS_PER_CONNECTION; i++)
            {
                MyMove data = localBuffer.getData();
                if(data == null) break;
                ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
                outputStream.write(data.MotionType);
                outputStream.write(data.MotionStatus);
                outputStream.write(data.data);
                outputStream.write(10086); // used as data separator and validator

                OutputStream output;
                try
                {
                    output = mySocket.getOutputStream();
                } catch (IOException e) {
                    Log.d("MyBthService", "writeData: failed to get output stream from socket");
                    break;
                }
                try
                {
                    output.write(outputStream.toByteArray());
                } catch (IOException e) {
                    Log.d("MyBthService", "writeData: failed to write to output stream");
                    break;
                }
            }
        }

        public void disconnect()
        {
            updateStatus(BluetoothStatus.NONE);
            try
            {
                unregisterReceiver(myReceiver);
            } catch(IllegalArgumentException e)
            {
                Log.d("MyBthService", "disconnect: receiver already unregistered");
            }
            if(mySocket == null) return;
            try
            {
                mySocket.close();
            } catch(IOException e)
            {
                Log.d("MyBthService", "Cannot close socket");
                mySocket = null;
                return;
            }
            mySocket = null;
        }

        private boolean testConnection(BluetoothDevice device)
        {
            BluetoothSocket tmp;
            try
            {
                tmp = device.createRfcommSocketToServiceRecord(TARGET_UUID);
            } catch(IOException e)
            {
                Log.d("MyBthService", "Cannot create socket");
                return false;
            }
            try
            {
                tmp.connect();
            } catch(IOException e)
            {
                Log.d("MyBthService", "Cannot connect to device: " + device.getName());
                return false;
            }
            try
            {
                tmp.close();
            } catch(IOException e)
            {
                Log.d("MyBthService", "Cannot disconnect device: " + device.getName());
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

    class MySensorService implements SensorEventListener
    {
        private SensorManager sensorManager;

        private Sensor accSensor;
        private Sensor magSensor;

        private final float[] accReading = new float[3];
        private final float[] magReading = new float[3];
        private final float[] rotationMatrix = new float[9];
        private final float[] orientationMatrix = new float[3];

        private MotionAcceleration motionAcceleration = MotionAcceleration.NONE;
        private MotionSteering motionSteering = MotionSteering.NONE;

        private int motionPitch = 0;
        private int motionRoll = 0;

        public MySensorService()
        {
            sensorManager = (SensorManager)getSystemService(Context.SENSOR_SERVICE);
            accSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
            magSensor = sensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD);
        }

        public void start()
        {
            // sample period is set to 10ms
            sensorManager.registerListener(this, accSensor, SensorManager.SENSOR_DELAY_FASTEST);
            sensorManager.registerListener(this, magSensor, SensorManager.SENSOR_DELAY_FASTEST);
        }

        public void stop()
        {
            sensorManager.unregisterListener(this);
        }

        @Override
        public void onSensorChanged(SensorEvent event)
        {
            if(event.sensor.getType() == Sensor.TYPE_ACCELEROMETER)
            {
                System.arraycopy(event.values, 0, accReading, 0, accReading.length);
                update();
            }
            else if(event.sensor.getType() == Sensor.TYPE_MAGNETIC_FIELD)
            {
                System.arraycopy(event.values, 0, magReading, 0, magReading.length);
                update();
            }
        }

        private void update()
        {
            SensorManager.getRotationMatrix(rotationMatrix, null, accReading, magReading);
            SensorManager.getOrientation(rotationMatrix, orientationMatrix);

            int pitch = (int)Math.toDegrees(orientationMatrix[1]);
            int roll = (int)Math.abs(Math.toDegrees(orientationMatrix[2]))-90;

            // update motion steering
            if(pitch > 15 && pitch < 80)
            {
                updateMotionSteer(MotionSteering.LEFT);
            }
            else if(pitch < -15 && pitch > -80)
            {
                updateMotionSteer(MotionSteering.RIGHT);
            }
            else
            {
                updateMotionSteer(MotionSteering.NONE);
            }
            updatePitch(pitch);

            if(roll < -25 && roll > -80)
            {
                updateMotionAcc(MotionAcceleration.FORWARD);
            }
            else if(roll > 10 && roll < 40)
            {
                updateMotionAcc(MotionAcceleration.BACKWARD);
            }
            else
            {
                updateMotionAcc(MotionAcceleration.NONE);
            }
            updateRoll(roll);

            localBuffer.addData(readMotionSteer(), readMotionAcc(), readPitch(), readRoll());
        }

        private synchronized void updateMotionAcc(MotionAcceleration newState)
        {
            motionAcceleration = newState;
        }

        public synchronized MotionAcceleration readMotionAcc()
        {
            return motionAcceleration;
        }

        private synchronized void updateMotionSteer(MotionSteering newState)
        {
            motionSteering = newState;
        }

        public synchronized MotionSteering readMotionSteer()
        {
            return motionSteering;
        }

        private synchronized void updatePitch(int newPitch)
        {
            motionPitch = newPitch;
        }

        public synchronized int readPitch()
        {
            return motionPitch;
        }

        private synchronized void updateRoll(int newRoll)
        {
            motionRoll = newRoll;
        }

        public synchronized int readRoll()
        {
            return motionRoll;
        }

        @Override
        public void onAccuracyChanged(Sensor sensor, int accuracy) {}
    }

    class MyBuffer
    {
        private final int MAX_SIZE = 50;
        private ArrayList<MyMove> buff = new ArrayList<>();

        public synchronized void addData(MotionSteering s1, MotionAcceleration s2, int pitch, int roll)
        {
            if(buff.size() >= MAX_SIZE) return;
            if(serviceBTH == null || serviceBTH.readStatus() != BluetoothStatus.CONNECTED) return;
            buff.add(new MyMove(0, s1.ordinal(), pitch));
            buff.add(new MyMove(1, s2.ordinal(), roll));
        }

        public synchronized MyMove getData()
        {
            if(buff.size() <= 0) return null;
            return buff.remove(0);
        }
    }

    class MyMove
    {
        int MotionType; // 0 for steering, 1 for acceleration
        int MotionStatus; // positive number for related status
        int data; // moving data
        public MyMove(int type, int status, int d)
        {
            MotionType = type;
            MotionStatus = status;
            data = d;
        }
    }
}
