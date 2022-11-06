package com.example.androidsteering;

import android.annotation.SuppressLint;
import android.bluetooth.BluetoothAdapter;
import android.content.Context;
import android.content.res.Configuration;
import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.PersistableBundle;
import android.util.DisplayMetrics;
import android.util.Log;
import android.util.Pair;
import android.view.MenuItem;
import android.view.MotionEvent;
import android.view.View;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.RadioGroup;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.ActionBarDrawerToggle;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;
import androidx.core.math.MathUtils;
import androidx.drawerlayout.widget.DrawerLayout;
import androidx.fragment.app.Fragment;
import androidx.fragment.app.FragmentManager;

import com.google.android.material.navigation.NavigationView;

import java.util.Objects;

// Reference of navigation drawer: https://guides.codepath.com/android/fragment-navigation-drawer

enum ControllerMode {
    None, Default, Alter, GamePad
}

public class MainActivity extends AppCompatActivity {
    private DrawerLayout mDrawer;
    private ActionBarDrawerToggle drawerToggle;
    private Toolbar toolbar;

    private Motion serviceMotion;
    private Connection serviceConnection;
    private Thread threadConnect;
    private Thread threadDisconnect;

    private ControllerMode controllerMode;
    private volatile boolean LTPressed = false;
    private volatile boolean RTPressed = false;

    private float displayY;

    private final Handler handlerUpdateUI = new Handler(Looper.getMainLooper());
    private final Runnable runnableUpdateUI = new Runnable() {
        @SuppressLint("DefaultLocale")
        @Override
        public void run() {
            try {
                if (controllerMode == ControllerMode.Default || controllerMode == ControllerMode.Alter) {
                    ProgressBar vHorizontal = findViewById(R.id.progressBarAcc);
                    ProgressBar vVertical = findViewById(R.id.progressBarSteer);
                    float progressHorizontal = (serviceMotion.readRoll() + 90.0f) / 180.0f * 100.0f;
                    float progressVertical = (180.0f - serviceMotion.readPitch()) / 360.0f * 100.0f;
                    vHorizontal.setProgress((int) progressHorizontal);
                    vVertical.setProgress((int) progressVertical);
                }
                if (controllerMode == ControllerMode.Alter) {
                    if (!LTPressed && !RTPressed) {
                        globalBuffer.addData(MotionStatus.ResetAccAngle, 0.0f);
                    } else if (LTPressed) {
                        ProgressBar bar = findViewById(R.id.progressBarLT);
                        globalBuffer.addData(MotionStatus.SetAccRatio, -bar.getProgress() / (float) bar.getMax());
                    } else if (RTPressed) {
                        ProgressBar bar = findViewById(R.id.progressBarRT);
                        globalBuffer.addData(MotionStatus.SetAccRatio, bar.getProgress() / (float) bar.getMax());
                    }
                } else if (controllerMode == ControllerMode.GamePad) {
                    if (!LTPressed && !RTPressed) {
                        globalBuffer.addData(MotionStatus.ResetAccAngle, 0.0f);
                    }
                    if (LTPressed) {
                        ProgressBar bar = findViewById(R.id.progressBarLT);
                        globalBuffer.addData(MotionStatus.SetLTValue, bar.getProgress() / (float) bar.getMax());
                    }
                    if (RTPressed) {
                        ProgressBar bar = findViewById(R.id.progressBarRT);
                        globalBuffer.addData(MotionStatus.SetRTValue, bar.getProgress() / (float) bar.getMax());
                    }
                }
            } catch (Exception e) {
                Log.d(getString(R.string.logTagMain), Objects.requireNonNull(e.getMessage()));
            } finally {
                handlerUpdateUI.postDelayed(this, 20);
            }
        }
    };

    private final Connection.MyBuffer globalBuffer = new Connection.MyBuffer();

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);
        // force fullscreen
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
        // keep screen on
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        // get window size
        DisplayMetrics displayMetrics = new DisplayMetrics();
        getWindowManager().getDefaultDisplay().getMetrics(displayMetrics);
        displayY = displayMetrics.heightPixels * 0.5f;
        // set default fragment
        setFragment(R.id.nav_connection_frag);
        // set action bar
        toolbar = findViewById(R.id.toolbar);
        toolbar.setTitle(R.string.navTitleConnection);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);
        // set navigation drawer view
        NavigationView navigationView = findViewById(R.id.navView);
        navigationView.getMenu().getItem(0).setChecked(true);
        setupDrawerContent(navigationView);

        mDrawer = findViewById(R.id.drawer_layout);
        drawerToggle = new ActionBarDrawerToggle(this, mDrawer, toolbar, R.string.drawer_open, R.string.drawer_close);
        drawerToggle.setDrawerIndicatorEnabled(true);
        drawerToggle.syncState();
        mDrawer.addDrawerListener(drawerToggle);

        // check for motion sensors
        checkSensor();
        // init motion service
        serviceMotion = new Motion(this, globalBuffer);
        serviceMotion.start();
        // init connection service
        serviceConnection = new Connection(this, globalBuffer);
        // start updating UI
        handlerUpdateUI.postDelayed(runnableUpdateUI, 0);
    }

    private void setupDrawerContent(NavigationView navigationView) {
        navigationView.setNavigationItemSelectedListener(menuItem -> {
            selectDrawerItem(menuItem);
            return true;
        });
    }

    public void selectDrawerItem(MenuItem menuItem) {
        setFragment(menuItem.getItemId());
        menuItem.setChecked(true);
        toolbar.setTitle(menuItem.getTitle());
        mDrawer.closeDrawers();
    }

    public void setFragment(int fragmentId) {
        resetController();
        Fragment fragment;
        try {
            if (fragmentId == R.id.nav_connection_frag) {
                fragment = FragmentConnection.class.newInstance();
                controllerMode = ControllerMode.None;
                globalBuffer.turnOff();
            } else if (fragmentId == R.id.nav_control_default_frag) {
                fragment = FragmentControlDefault.class.newInstance();
                controllerMode = ControllerMode.Default;
                globalBuffer.turnOn();
                globalBuffer.setUpdatePitch(true);
                globalBuffer.setUpdateRoll(true);
            } else if (fragmentId == R.id.nav_control_alt_frag) {
                fragment = FragmentControlAlter.class.newInstance();
                controllerMode = ControllerMode.Alter;
                globalBuffer.turnOn();
                globalBuffer.setUpdatePitch(true);
                globalBuffer.setUpdateRoll(false);
            } else if (fragmentId == R.id.nav_control_pad_frag) {
                fragment = FragmentControlPad.class.newInstance();
                controllerMode = ControllerMode.GamePad;
                globalBuffer.turnOn();
                globalBuffer.setUpdatePitch(false);
                globalBuffer.setUpdateRoll(false);
            } else {
                fragment = FragmentConnection.class.newInstance();
                controllerMode = ControllerMode.None;
                globalBuffer.turnOff();
            }

        } catch (Exception e) {
            Log.d(getString(R.string.logTagMain), Objects.requireNonNull(e.getMessage()));
            return;
        }

        FragmentManager fragmentManager = getSupportFragmentManager();
        fragmentManager.beginTransaction().replace(R.id.flContent, fragment).commit();
    }

    @Override
    public void onPostCreate(@Nullable Bundle savedInstanceState, @Nullable PersistableBundle persistentState) {
        super.onPostCreate(savedInstanceState, persistentState);
        drawerToggle.syncState();
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        drawerToggle.onConfigurationChanged(newConfig);
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        serviceMotion.stop();
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {
        if (drawerToggle.onOptionsItemSelected(item)) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    // check if bluetooth is supported and enabled
    private boolean checkBTH() {
        BluetoothAdapter test = BluetoothAdapter.getDefaultAdapter();
        if (test == null) {
            new AlertDialog.Builder(this).setTitle("Not Compatible").setMessage("Your phone does not support Bluetooth").setPositiveButton("OK", (dialog, which) -> System.exit(0)).show();
            return false;
        }
        if (!test.isEnabled()) {
            Toast.makeText(this, "Please enable bluetooth", Toast.LENGTH_SHORT).show();
            return false;
        }
        return true;
    }

    // check if wifi is connected
    private boolean checkWifi() {
        // Reference: https://stackoverflow.com/questions/3841317/how-do-i-see-if-wi-fi-is-connected-on-android
        NetworkInfo test = ((ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE)).getNetworkInfo(ConnectivityManager.TYPE_WIFI);
        if (test == null) {
            new AlertDialog.Builder(this).setTitle("Not Compatible").setMessage("Your phone cannot access wifi").setPositiveButton("OK", (dialog, which) -> System.exit(0)).show();
            return false;
        }
        if (!test.isConnected()) {
            Toast.makeText(this, "Please connect Wifi to PC", Toast.LENGTH_SHORT).show();
            return false;
        }
        return true;
    }

    // check if motion sensor is supported
    public void checkSensor() {
        SensorManager test = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
        if (test == null || test.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD) == null || test.getDefaultSensor(Sensor.TYPE_ACCELEROMETER) == null) {
            new AlertDialog.Builder(this).setTitle("Not Compatible").setMessage("Your phone does not have required sensors").setPositiveButton("OK", (dialog, which) -> System.exit(0)).show();
        }
    }

    // check if service is connected
    public boolean isConnected() {
        return serviceConnection.connected;
    }

    // get service connection mode
    public ConnectionMode getConnectionMode() {
        return serviceConnection.connectionMode;
    }

    // Connection fragment callbacks
    public void setRadioGroupCallback() {
        RadioGroup radioGroup = findViewById(R.id.radioGroup);
        radioGroup.setOnCheckedChangeListener((group, i) -> {
            if (i == R.id.radioButtonBth) {
                serviceConnection.connectionMode = ConnectionMode.Bluetooth;
            } else if (i == R.id.radioButtonWifi) {
                serviceConnection.connectionMode = ConnectionMode.Wifi;
            }
        });
    }

    // connect button callback
    public void connectionButtonOnClick(View view) {
        boolean connected = isConnected();
        // if already connected, start a thread to disconnect
        if (connected) {
            if (threadDisconnect != null && threadDisconnect.isAlive()) {
                Toast.makeText(this, "Already Disconnecting", Toast.LENGTH_SHORT).show();
                return;
            }
            Toast.makeText(this, "Disconnecting...", Toast.LENGTH_SHORT).show();
            threadDisconnect = new Thread(() -> {
                resetController();
                serviceConnection.disconnect();
                runOnUiThread(() -> {
                    if (!isConnected()) {
                        ((Button) view).setText(R.string.buttonConnect);
                    } else ((Button) view).setText(R.string.buttonDisconnect);
                });
            });
            threadDisconnect.start();
        }
        // if not connected, start a thread to connect
        else {
            if (threadConnect != null && threadConnect.isAlive()) {
                Toast.makeText(this, "Already Connecting", Toast.LENGTH_SHORT).show();
                return;
            }
            Toast.makeText(this, "Connecting...", Toast.LENGTH_SHORT).show();
            RadioGroup group = findViewById(R.id.radioGroup);
            if (group.getCheckedRadioButtonId() == R.id.radioButtonBth && !checkBTH()) return;
            else if (group.getCheckedRadioButtonId() == R.id.radioButtonWifi && !checkWifi())
                return;
            threadConnect = new Thread(() -> {
                String result = serviceConnection.connect();
                runOnUiThread(() -> {
                    if (result.length() > 0)
                        Toast.makeText(this, result, Toast.LENGTH_SHORT).show();
                    if (isConnected()) {
                        for (int i = 0; i < group.getChildCount(); i++) {
                            group.getChildAt(i).setEnabled(false);
                        }
                        ((Button) view).setText(R.string.buttonDisconnect);
                    } else ((Button) view).setText(R.string.buttonConnect);
                });
            });
            threadConnect.start();
        }
    }

    // send data to reset controller motion status
    public void resetController() {
        globalBuffer.addData(MotionStatus.ResetSteerAngle, 0.0f);
        globalBuffer.addData(MotionStatus.ResetAccAngle, 0.0f);
    }

    // callback for rest buttons
    public boolean touchX(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.X);
    }

    public boolean touchY(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.Y);
    }

    public boolean touchA(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.A);
    }

    public boolean touchB(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.B);
    }

    public boolean touchLB(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.LB);
    }

    public boolean touchRB(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.RB);
    }

    public boolean touchBACK(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.BACK);
    }

    public boolean touchSTART(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.START);
    }

    public boolean touchUP(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.UP);
    }

    public boolean touchDOWN(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.DOWN);
    }

    public boolean touchLEFT(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.LEFT);
    }

    public boolean touchRIGHT(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.RIGHT);
    }

    public boolean touchHOME(View view, MotionEvent e) {
        return touchButton(view, e, MotionButton.HOME);
    }

    private boolean touchButton(View view, MotionEvent e, MotionButton button) {
        switch (e.getAction()) {
            case MotionEvent.ACTION_DOWN:
                globalBuffer.addData(button, true);
                return true;
            case MotionEvent.ACTION_CANCEL:
            case MotionEvent.ACTION_UP:
                globalBuffer.addData(button, false);
                return true;
        }
        return false;
    }

    public boolean touchLT(View view, MotionEvent e) {
        ProgressBar bar = findViewById(R.id.progressBarLT);
        switch (e.getAction()) {
            case MotionEvent.ACTION_DOWN:
                view.setPressed(true);
                LTPressed = true;
                bar.setVisibility(View.VISIBLE);
                return true;
            case MotionEvent.ACTION_MOVE:
                float ratio = MathUtils.clamp((displayY - e.getRawY()) / (displayY * 0.8f) + 0.5f, 0.0f, 1.0f);
                bar.setProgress((int) (bar.getMax() * ratio));
                return true;
            case MotionEvent.ACTION_UP:
                view.setPressed(false);
                LTPressed = false;
                bar.setVisibility(View.INVISIBLE);
                return true;
        }
        return false;
    }

    public boolean touchRT(View view, MotionEvent e) {
        ProgressBar bar = findViewById(R.id.progressBarRT);
        switch (e.getAction()) {
            case MotionEvent.ACTION_DOWN:
                view.setPressed(true);
                RTPressed = true;
                bar.setVisibility(View.VISIBLE);
                return true;
            case MotionEvent.ACTION_MOVE:
                float ratio = MathUtils.clamp((displayY - e.getRawY()) / (displayY * 0.8f) + 0.5f, 0.0f, 1.0f);
                bar.setProgress((int) (bar.getMax() * ratio));
                return true;
            case MotionEvent.ACTION_UP:
                view.setPressed(false);
                RTPressed = false;
                bar.setVisibility(View.INVISIBLE);
                return true;
        }
        return false;
    }

    public void moveLeftStick(int angle, int strength) {
        Pair<Float, Float> XY = computeJoyStickXY(angle, strength);
        globalBuffer.addData(MotionStatus.SetLeftStickX, XY.first);
        globalBuffer.addData(MotionStatus.SetLeftStickY, XY.second);
    }

    public void moveRightStick(int angle, int strength) {
        Pair<Float, Float> XY = computeJoyStickXY(angle, strength);
        globalBuffer.addData(MotionStatus.SetRightStickX, XY.first);
        globalBuffer.addData(MotionStatus.SetRightStickY, XY.second);
    }

    private Pair<Float, Float> computeJoyStickXY(int angle, int strength) {
        double r = Math.toRadians(angle);
        double s = strength / 100.0;

        float x = (float) (Math.cos(r) * s);
        float y = (float) (Math.sin(r) * s);

        return new Pair<>(x, y);
    }
}
