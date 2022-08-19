// component for collecting motion information

package com.example.androidsteering;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.util.Log;

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
    private final float[] orientationMatrix = new float[3];

    private float motionPitch = 0.0f;
    private float motionRoll = 0.0f;

    public static final float LTVal = 0.0f;
    public static final float RTVal = -70.0f;

    public Motion(MainActivity activity, Connection.MyBuffer buffer) {
        mainActivity = activity;
        globalBuffer = buffer;
        sensorManager = (SensorManager) mainActivity.getSystemService(Context.SENSOR_SERVICE);
        accSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
        magSensor = sensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD);
    }

    // start sensor callback
    public void start() {
        // sample period is set to 10ms
        if (!sensorManager.registerListener(this, accSensor, SensorManager.SENSOR_DELAY_GAME))
            Log.d(mainActivity.getString(R.string.logTagMotion), "Failed to register accelerometer");
        else if (!sensorManager.registerListener(this, magSensor, SensorManager.SENSOR_DELAY_GAME))
            Log.d(mainActivity.getString(R.string.logTagMotion), "Failed to register magnetic field");
        else
            Log.d(mainActivity.getString(R.string.logTagMotion), "Sensor listener registered");
    }

    // stop sensor callback
    public void stop() {
        sensorManager.unregisterListener(this);
        Log.d(mainActivity.getString(R.string.logTagMotion), "Sensor listener unregistered");
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (event.sensor.getType() == Sensor.TYPE_ACCELEROMETER) {
            System.arraycopy(event.values, 0, accReading, 0, accReading.length);
            update();
        } else if (event.sensor.getType() == Sensor.TYPE_MAGNETIC_FIELD) {
            System.arraycopy(event.values, 0, magReading, 0, magReading.length);
            update();
        }
    }

    // update current pitch roll
    private void update() {
        SensorManager.getRotationMatrix(rotationMatrix, null, accReading, magReading);
        SensorManager.getOrientation(rotationMatrix, orientationMatrix);

        float pitch = (float) Math.toDegrees(orientationMatrix[1]);
        float roll = (float) Math.abs(Math.toDegrees(orientationMatrix[2])) - 90;

        updatePitch(pitch);
        updateRoll(roll);

        globalBuffer.addData(readPitch(), readRoll());

//        Log.d(mainActivity.getString(R.string.logTagMotion), "Sensor data update");
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
