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
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.os.Handler;
// import android.util.Log;
import android.view.View;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.ByteBuffer;
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

enum MotionButton
{
    X,
    Y,
    A,
    B,
    LB,
    RB,
    UP,
    DOWN,
    RIGHT,
    LEFT,
    BACK,
    START
}

enum BluetoothStatus
{
    NONE,
    SEARCHING,
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

        checkBTH();
        checkSensor();

        serviceSensor = new MySensorService();
        localBuffer = new MyBuffer();

        UIHandler = new Handler();
        bthHandler = new Handler();

        setContentView(R.layout.main);
        findViewById(R.id.btRetry).setVisibility(View.GONE);

        txtViewDebug = findViewById(R.id.textViewDebug);
        txtViewBTH = findViewById(R.id.textViewBTH);

        UIRunner = new Runnable() {
            @Override
            public void run() {
                String motionSteer;
                switch(serviceSensor.motionSteering)
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
                motionSteer += "><" + serviceSensor.motionPitch;
                String motionAcc;
                switch(serviceSensor.motionAcceleration)
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
                motionAcc += "><" + serviceSensor.motionRoll;
                txtViewDebug.setText(String.format("Steering: %s\nAcceleration: %s", motionSteer, motionAcc));

                String bthStatus;
                BluetoothStatus status = BluetoothStatus.NONE;
                if(serviceBTH != null)
                    status = serviceBTH.myStatus;
                switch(status)
                {
                    case SEARCHING:
                        bthStatus = "Searching";
                        break;
                    case CONNECTED:
                        bthStatus = "Device Connected";
                        break;
                    default:
                        bthStatus = "No Connection";
                        break;
                }
                if(txtViewBTH.getVisibility() != View.GONE)
                    txtViewBTH.setText(String.format("Bluetooth: %s", bthStatus));

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
                    serviceBTH.destroy();
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
                        if(serviceBTH.readStatus() == BluetoothStatus.SEARCHING)
                        {
                            bthHandler.postDelayed(this, 50);
                            return;
                        }
                        else if(!serviceBTH.connect())
                        {
                            serviceBTH.updatePotential();
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
        findViewById(R.id.btRetry).setVisibility(View.GONE);
        findViewById(R.id.textViewBTH).setVisibility(View.VISIBLE);
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
            serviceBTH.destroy();
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
            return;
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

    public void showAllButtons(View view)
    {
        if(((Button)view).getText().toString().equals("Show All"))
        {
            findViewById(R.id.buttonA).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonB).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonX).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonY).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonLB).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonRB).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonUP).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonDOWN).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonLEFT).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonRIGHT).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonBACK).setVisibility(View.VISIBLE);
            findViewById(R.id.buttonSTART).setVisibility(View.VISIBLE);
            ((Button)view).setText(R.string.HideAll);
        }
        else
        {
            findViewById(R.id.buttonA).setVisibility(View.GONE);
            findViewById(R.id.buttonB).setVisibility(View.GONE);
            findViewById(R.id.buttonX).setVisibility(View.GONE);
            findViewById(R.id.buttonY).setVisibility(View.GONE);
            findViewById(R.id.buttonLB).setVisibility(View.GONE);
            findViewById(R.id.buttonRB).setVisibility(View.GONE);
            findViewById(R.id.buttonUP).setVisibility(View.GONE);
            findViewById(R.id.buttonDOWN).setVisibility(View.GONE);
            findViewById(R.id.buttonLEFT).setVisibility(View.GONE);
            findViewById(R.id.buttonRIGHT).setVisibility(View.GONE);
            findViewById(R.id.buttonBACK).setVisibility(View.GONE);
            findViewById(R.id.buttonSTART).setVisibility(View.GONE);
            ((Button)view).setText(R.string.ShowAll);
        }
    }

    class MyBthService
    {
        private final UUID TARGET_UUID = UUID.fromString("a7bda841-7dbc-4179-9800-1a3eff463f1c");
        private final int MAX_TRANSACTIONS_PER_CONNECTION = 10;
        private final int CONNECTION_SEPERATOR = 0x7FFFFFFF;

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
                        Intent enableBTH = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
                        startActivityForResult(enableBTH, 1);
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
                else if(BluetoothAdapter.ACTION_DISCOVERY_FINISHED.equals(action))
                {
                    updateStatus(BluetoothStatus.NONE);
                    findTargetDevice();
                }
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

        public MyBthService() {
            registerReceiver(myBluetoothStatusReceiver, new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED));
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
                    if(device.getBluetoothClass().getMajorDeviceClass() == 256)
                    {
                        potentialDevices.add(device);
                    }
                }
                try
                {
                    unregisterReceiver(myReceiver);
                } catch(IllegalArgumentException e)
                {
                    // Log.d("MyBthService", "updatePotential: receiver already unregistered");
                }
                IntentFilter filter = new IntentFilter(BluetoothAdapter.ACTION_DISCOVERY_FINISHED);
                // filter.addAction(BluetoothDevice.ACTION_FOUND);
                registerReceiver(myReceiver, filter);
                updateStatus(BluetoothStatus.SEARCHING);
                myAdapter.startDiscovery();
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
            for(BluetoothDevice d : potentialDevices)
            {
                if(testConnection(d))
                {
                    targetDevice = d;
                    break;
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
                // Log.d("MyBthService", "connect: receiver already unregistered");
            }

            ByteBuffer buffer = ByteBuffer.allocate(4);
            buffer.putInt(CONNECTION_SEPERATOR);
            write(buffer.array());

            IntentFilter filter = new IntentFilter(BluetoothDevice.ACTION_ACL_DISCONNECT_REQUESTED);
            filter.addAction(BluetoothDevice.ACTION_ACL_DISCONNECTED);
            registerReceiver(myReceiver, filter);
            updateStatus(BluetoothStatus.CONNECTED);
            return true;
        }

        public void writeData()
        {
            if(myStatus != BluetoothStatus.CONNECTED) return;
            ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
            for(int i = 0; i < MAX_TRANSACTIONS_PER_CONNECTION; i++)
            {
                MyMove data = localBuffer.getData();
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
                bb.putInt(data.data);
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
                unregisterReceiver(myReceiver);
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
                unregisterReceiver(myBluetoothStatusReceiver);
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
            sensorManager.registerListener(this, accSensor, SensorManager.SENSOR_DELAY_GAME);
            sensorManager.registerListener(this, magSensor, SensorManager.SENSOR_DELAY_GAME);
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
            if(pitch > 2 && pitch < 85)
            {
                updateMotionSteer(MotionSteering.LEFT);
            }
            else if(pitch < -2 && pitch > -85)
            {
                updateMotionSteer(MotionSteering.RIGHT);
            }
            else
            {
                updateMotionSteer(MotionSteering.NONE);
            }
            updatePitch(pitch);

            if(roll < -5 && roll > -85)
            {
                updateMotionAcc(MotionAcceleration.FORWARD);
            }
            else if(roll > 5 && roll < 85)
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

        public synchronized void addData(MotionButton button)
        {
            if(buff.size() >= MAX_SIZE) return;
            if(serviceBTH == null || serviceBTH.readStatus() != BluetoothStatus.CONNECTED) return;
            buff.add(new MyMove(2, button.ordinal(), 0));
        }

        public synchronized MyMove getData()
        {
            if(buff.size() <= 0) return null;
            return buff.remove(0);
        }
    }

    class MyMove
    {
        int MotionType; // 0 for acceleration, 1 for steering
        int MotionStatus; // positive number for related status
        int data; // moving data
        public MyMove(int type, int status, int d)
        {
            MotionType = type;
            MotionStatus = status;
            data = d;
        }
    }

    public void pressX(View view)
    {
        localBuffer.addData(MotionButton.X);
    }

    public void pressY(View view)
    {
        localBuffer.addData(MotionButton.Y);
    }

    public void pressA(View view)
    {
        localBuffer.addData(MotionButton.A);
    }

    public void pressB(View view)
    {
        localBuffer.addData(MotionButton.B);
    }

    public void pressLB(View view)
    {
        localBuffer.addData(MotionButton.LB);
    }

    public void pressRB(View view)
    {
        localBuffer.addData(MotionButton.RB);
    }

    public void pressBACK(View view)
    {
        localBuffer.addData(MotionButton.BACK);
    }

    public void pressSTART(View view)
    {
        localBuffer.addData(MotionButton.START);
    }

    public void pressUP(View view)
    {
        localBuffer.addData(MotionButton.UP);
    }

    public void pressDOWN(View view)
    {
        localBuffer.addData(MotionButton.DOWN);
    }

    public void pressLEFT(View view)
    {
        localBuffer.addData(MotionButton.LEFT);
    }

    public void pressRIGHT(View view)
    {
        localBuffer.addData(MotionButton.RIGHT);
    }
}
