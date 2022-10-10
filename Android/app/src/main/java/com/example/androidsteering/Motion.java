// component for collecting motion information

package com.example.androidsteering;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Process;
import android.util.Log;

import androidx.core.math.MathUtils;

enum MotionStatus {
    SetSteerAngle(0),
    SetAccAngle(1),
    ResetSteerAngle(2),
    ResetAccAngle(3),
    SetAccRatio(4);

    private final int val;

    MotionStatus(int v) {
        val = v;
    }

    public int getVal() {
        return val;
    }
}

enum MotionButton {
    X(0),
    Y(1),
    A(2),
    B(3),
    LB(4),
    RB(5),
    UP(6),
    DOWN(7),
    RIGHT(8),
    LEFT(9),
    BACK(10),
    START(11);

    private final int val;

    MotionButton(int v) {
        val = v;
    }

    public int getVal() {
        return val;
    }
}

public class Motion implements SensorEventListener {
    static class MyMove {
        boolean MotionButton; // is it a button motion?
        int MotionStatus; // positive number for related status
        float data; // moving data

        public MyMove(boolean type, int status, float d) {
            MotionButton = type;
            MotionStatus = status;
            data = d;
        }
    }

    private final SensorManager sensorManager;

    private final Sensor accSensor;
    private final Sensor magSensor;

    private final MainActivity mainActivity;
    private final Connection.MyBuffer globalBuffer;

    private final float[] accReading = new float[3];
    private final float[] magReading = new float[3];
    private final float[] rotationMatrix = new float[9];
    private final float[] orientation = new float[3];

    private volatile float motionPitch = 0.0f;
    private volatile float motionRoll = 0.0f;

    private Thread dataSubmitThread = null;
    private volatile boolean dataSubmitShouldStop;
    private final int MAX_WAIT_TIME = 1000;
    private final int DATA_UPDATE_FREQ = 5; // wait for milliseconds for next update

    public Motion(MainActivity activity, Connection.MyBuffer buffer) {
        mainActivity = activity;
        globalBuffer = buffer;
        sensorManager = (SensorManager) mainActivity.getSystemService(Context.SENSOR_SERVICE);
        accSensor = sensorManager.getDefaultSensor(Sensor.TYPE_GRAVITY);
        magSensor = sensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD);
    }

    // start sensor callback
    public void start() {
        if (dataSubmitThread != null && dataSubmitThread.isAlive())
            stop();
        // sample period is set to 10ms
        if (!sensorManager.registerListener(this, accSensor, SensorManager.SENSOR_DELAY_GAME))
            Log.d(mainActivity.getString(R.string.logTagMotion), "Failed to register accelerometer");
        else if (!sensorManager.registerListener(this, magSensor, SensorManager.SENSOR_DELAY_GAME))
            Log.d(mainActivity.getString(R.string.logTagMotion), "Failed to register magnetic field");
        else
            Log.d(mainActivity.getString(R.string.logTagMotion), "Sensor listener registered");
        // start data submission thread
        dataSubmitShouldStop = false;
        dataSubmitThread = new Thread(() -> {
            Process.setThreadPriority(Process.THREAD_PRIORITY_FOREGROUND);
            while (!dataSubmitShouldStop) {
                try {
                    globalBuffer.addData(readPitch(), readRoll());
                    Thread.sleep(DATA_UPDATE_FREQ);
                } catch (InterruptedException e) {
                    Log.d(mainActivity.getString(R.string.logTagMotion), e.toString());
                    break;
                }
            }
        });
        dataSubmitThread.start();
    }

    // stop sensor callback
    public void stop() {
        sensorManager.unregisterListener(this);
        Log.d(mainActivity.getString(R.string.logTagMotion), "Sensor listener unregistered");
        dataSubmitShouldStop = true;
        if (dataSubmitThread != null && dataSubmitThread.isAlive()) {
            try {
                dataSubmitThread.join(MAX_WAIT_TIME);
            } catch (InterruptedException e) {
                Log.d(mainActivity.getString(R.string.logTagMotion), e.toString());
            }
        }
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (event == null) return;
        if (event.sensor.getType() == Sensor.TYPE_GRAVITY) {
            System.arraycopy(event.values, 0, accReading, 0, accReading.length);
            updatePose();
        } else if (event.sensor.getType() == Sensor.TYPE_MAGNETIC_FIELD) {
            System.arraycopy(event.values, 0, magReading, 0, magReading.length);
            updatePose();
        }
    }

    // update current pitch roll
    private void updatePose() {
        SensorManager.getRotationMatrix(rotationMatrix, null, accReading, magReading);
        SensorManager.getOrientation(rotationMatrix, orientation);

        // intense math (lol)
        // compute real roll (horizontal rotation, or acceleration angle)
        double roll = Math.asin(MathUtils.clamp(Math.sin(Math.PI * 0.5 + orientation[2]) * Math.cos(orientation[1]), -1.0, 1.0));
        // to compute the real pitch (vertical rotation, or steering angle)
        double rollCosInv = Math.cos(roll);
        rollCosInv = rollCosInv == 0.0 ? 0.0 : 1.0 / rollCosInv;
        double pitch = Math.asin(MathUtils.clamp(Math.sin(orientation[1]) * rollCosInv, -1.0, 1.0));

        // for steering, check whether it is over 90 degrees either side
        if (orientation[2] > 0.0)
            pitch = pitch > 0.0 ? Math.PI - Math.abs(pitch) : Math.abs(pitch) - Math.PI;

//        Log.d("MotionDebug", String.format("[%4.0f,%4.0f,%4.0f] - %4.0f, %4.0f",
//                Math.toDegrees(orientation[0]), Math.toDegrees(orientation[1]), Math.toDegrees(orientation[2]),
//                Math.toDegrees(pitch), Math.toDegrees(roll)
//        ));

        updatePitch((float) Math.toDegrees(pitch));
        updateRoll((float) Math.toDegrees(roll));
    }

    // update pitch
    private synchronized void updatePitch(float newPitch) {
        motionPitch = newPitch;
    }

    // read pitch
    public synchronized float readPitch() {
        return motionPitch;
    }

    // update roll
    private synchronized void updateRoll(float newRoll) {
        motionRoll = newRoll;
    }

    // read roll
    public synchronized float readRoll() {
        return motionRoll;
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
    }
}
