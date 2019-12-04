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

public class MainActivity extends AppCompatActivity
{

    private SensorManager sensorManager;
    private TextView txtViewDebug;
    private TextView txtViewBTH;
    private MyService service;
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
        service = new MyService();

        // set main view
        setContentView(R.layout.main);

        // get debug text view
        txtViewDebug = findViewById(R.id.textViewDebug);
        // get bluetooth text view
        txtViewBTH = findViewById(R.id.textViewBTH);

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
        private Sensor accSensor;
        private Sensor magSensor;

        private final float[] accReading = new float[3];
        private final float[] magReading = new float[3];
        private final float[] rotationMatrix = new float[9];
        private final float[] remapMatrix = new float[9];
        private final float[] orientationMatrix = new float[3];

        public MyService()
        {
            accSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
            magSensor = sensorManager.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD);
        }

        public void start()
        {
            // sample period is set to 10ms
            sensorManager.registerListener(this, accSensor, SensorManager.SENSOR_DELAY_NORMAL, SensorManager.SENSOR_DELAY_UI);
            sensorManager.registerListener(this, magSensor, SensorManager.SENSOR_DELAY_NORMAL, SensorManager.SENSOR_DELAY_UI);
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
            String message = String.format(Locale.ENGLISH, "Pitch:\t%d\nRoll:\t%d", pitch, roll);
            txtViewDebug.setText(message);
        }

        @Override
        public void onAccuracyChanged(Sensor sensor, int accuracy)
        {

        }
    }
}
