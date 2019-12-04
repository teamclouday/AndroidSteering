package com.example.androidsteering;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.math.MathUtils;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothSocket;
import android.content.Context;
import android.content.pm.ActivityInfo;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.os.Handler;
import android.view.View;
import android.view.WindowManager;
import android.widget.TextView;

import org.w3c.dom.Text;

import java.util.Locale;

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

public class MainActivity extends AppCompatActivity
{
    private TextView txtViewDebug;
    private TextView txtViewBTH;

    private SensorManager sensorManager;
    private MySensorService serviceSensor;
    private Handler motionHandler;
    private Runnable motionRunner;

    private BluetoothAdapter bthAdapter;
    private BluetoothSocket bthSocket;

    @Override
    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        // set full screen
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        // remove title bar
        getSupportActionBar().hide();
        // set orientation
        setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);
        // create sensor manager and sensor
        sensorManager = (SensorManager)getSystemService(Context.SENSOR_SERVICE);
        // create service object
        serviceSensor = new MySensorService();
        // create motion handler
        motionHandler = new Handler();

        // set main view
        setContentView(R.layout.main);

        // get debug text view
        txtViewDebug = findViewById(R.id.textViewDebug);
        // get bluetooth text view
        txtViewBTH = findViewById(R.id.textViewBTH);

        serviceSensor.start();

        motionRunner = new Runnable() {
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
                txtViewDebug.setText("Steering: "+motionSteer+"\n"+"Acceleration: "+motionAcc);
                motionHandler.postDelayed(this, 20);
            }
        };
    }

    @Override
    public void onResume()
    {
        super.onResume();
        serviceSensor.start();
        motionHandler.postDelayed(motionRunner, 0);
    }

    @Override
    public void onPause()
    {
        super.onPause();
        serviceSensor.stop();
        motionHandler.removeCallbacksAndMessages(motionRunner);
    }

    class MySensorService implements SensorEventListener
    {
        private Sensor accSensor;
        private Sensor magSensor;

        private final float[] accReading = new float[3];
        private final float[] magReading = new float[3];
        private final float[] rotationMatrix = new float[9];
        private final float[] orientationMatrix = new float[3];

        public MotionAcceleration motionAcceleration = MotionAcceleration.NONE;
        public MotionSteering motionSteering = MotionSteering.NONE;

        public int motionPitch = 0;
        public int motionRoll = 0;

        public MySensorService()
        {
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
                motionSteering = MotionSteering.LEFT;
            }
            else if(pitch < -15 && pitch > -80)
            {
                motionSteering = MotionSteering.RIGHT;
            }
            else
            {
                motionSteering = MotionSteering.NONE;
            }
            motionPitch = pitch;

            if(roll < -20 && roll > -80)
            {
                motionAcceleration = MotionAcceleration.FORWARD;
            }
            else if(roll > 10 && roll < 40)
            {
                motionAcceleration = MotionAcceleration.BACKWARD;
            }
            else
            {
                motionAcceleration = MotionAcceleration.NONE;
            }
            motionRoll = roll;
        }

        @Override
        public void onAccuracyChanged(Sensor sensor, int accuracy)
        {

        }
    }
}
