package com.example.androidsteering;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Context;
import android.content.pm.ActivityInfo;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.os.Handler;
import android.view.WindowManager;
import android.widget.TextView;

public class MainActivity extends AppCompatActivity
{

    private SensorManager sensorManager;
    private TextView txtViewDebug;
    private MyService service;

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
        service = new MyService();

        // set main view
        setContentView(R.layout.main);

        // get debug textview
        txtViewDebug = findViewById(R.id.textViewDebug);

        service.start();
    }

    @Override
    public void onResume()
    {
        super.onResume();
        service.start();
    }

    @Override
    public void onPause()
    {
        super.onPause();
        service.stop();
    }

    class MyService implements SensorEventListener
    {
        private Sensor rotationSensor;
        private final float[] rotationMatrix = new float[16];

        public MyService()
        {
            rotationSensor = sensorManager.getDefaultSensor(Sensor.TYPE_GAME_ROTATION_VECTOR);
            rotationMatrix[0] = 1;
            rotationMatrix[4] = 1;
            rotationMatrix[8] = 1;
            rotationMatrix[12] = 1;
        }

        public void start()
        {
            // sample period is set to 10ms
            sensorManager.registerListener(this, rotationSensor, 10000);
        }

        public void stop()
        {
            sensorManager.unregisterListener(this);
        }


        @Override
        public void onSensorChanged(SensorEvent event)
        {
            if(event.sensor.getType() == Sensor.TYPE_GAME_ROTATION_VECTOR)
            {
                SensorManager.getRotationMatrixFromVector(rotationMatrix, event.values);
                String debugText = "[";
                for(int i = 0; i < 4; i++)
                {
                    for(int j = 0; j < 4; j++)
                    {
                        debugText += String.format("%.1f", rotationMatrix[i*4+j]);
                        if(j != 3)
                            debugText += "\t";
                    }
                    if(i != 3)
                        debugText += "\n";
                }
                debugText += "]";
                txtViewDebug.setText(debugText);
            }
        }

        private float findQuaternionTwist(float[] q, float[] axis)
        {

        }

        @Override
        public void onAccuracyChanged(Sensor sensor, int accuracy)
        {

        }
    }
}
