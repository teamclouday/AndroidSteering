// component for collecting motion information

package com.example.androidsteering;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;

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

public class Motion implements SensorEventListener
{
    static class MyMove
    {
        int MotionType; // 0 for acceleration, 1 for steering
        int MotionStatus; // positive number for related status
        float data; // moving data
        public MyMove(int type, int status, float d)
        {
            MotionType = type;
            MotionStatus = status;
            data = d;
        }
    }

    private SensorManager sensorManager;

    private Sensor accSensor;
    private Sensor magSensor;

    private Context activityContext;

    private final float[] accReading = new float[3];
    private final float[] magReading = new float[3];
    private final float[] rotationMatrix = new float[9];
    private final float[] orientationMatrix = new float[3];

    private MotionAcceleration motionAcceleration = MotionAcceleration.NONE;
    private MotionSteering motionSteering = MotionSteering.NONE;

    private float motionPitch = 0.0f;
    private float motionRoll = 0.0f;

    public Motion(Context context)
    {
        activityContext = context;
        sensorManager = (SensorManager)context.getSystemService(Context.SENSOR_SERVICE);
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

        float pitch = (float)Math.toDegrees(orientationMatrix[1]);
        float roll = (float)Math.abs(Math.toDegrees(orientationMatrix[2]))-90;

        // update motion steering
        if(pitch > 2.0 && pitch < 85.0)
        {
            updateMotionSteer(MotionSteering.LEFT);
        }
        else if(pitch < -2.0 && pitch > -85.0)
        {
            updateMotionSteer(MotionSteering.RIGHT);
        }
        else
        {
            updateMotionSteer(MotionSteering.NONE);
        }
        updatePitch(pitch);

        if(roll < -5.0 && roll > -85.0)
        {
            updateMotionAcc(MotionAcceleration.FORWARD);
        }
        else if(roll > 5.0 && roll < 85.0)
        {
            updateMotionAcc(MotionAcceleration.BACKWARD);
        }
        else
        {
            updateMotionAcc(MotionAcceleration.NONE);
        }
        updateRoll(roll);

        Connection.buffer.addData(readMotionSteer(), readMotionAcc(), readPitch(), readRoll());
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

    private synchronized void updatePitch(float newPitch)
    {
        motionPitch = newPitch;
    }

    public synchronized float readPitch()
    {
        return motionPitch;
    }

    private synchronized void updateRoll(float newRoll)
    {
        motionRoll = newRoll;
    }

    public synchronized float readRoll()
    {
        return motionRoll;
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {}

}
