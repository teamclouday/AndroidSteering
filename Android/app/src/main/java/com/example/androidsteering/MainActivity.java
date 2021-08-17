package com.example.androidsteering;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.ActionBarDrawerToggle;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.drawerlayout.widget.DrawerLayout;

import android.bluetooth.BluetoothAdapter;
import android.content.Context;
import android.content.Intent;
import android.content.res.Configuration;
import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.os.Bundle;
// import android.util.Log;
import android.os.PersistableBundle;
import android.view.MenuItem;
import android.view.View;
import android.view.WindowManager;
import androidx.appcompat.widget.Toolbar;

import com.google.android.material.navigation.NavigationView;

import java.util.Objects;

// Reference of navigation drawer: https://guides.codepath.com/android/fragment-navigation-drawer

public class MainActivity extends AppCompatActivity
{
    private DrawerLayout mDrawer;
    private ActionBarDrawerToggle drawerToggle;

    private Motion serviceMotion;
    private Connection serviceConnection;
    private Toolbar toolbar;

    @Override
    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);
        // force fullscreen
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
        // keep screen on
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
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
        drawerToggle = new ActionBarDrawerToggle(this, mDrawer, toolbar, R.string.drawer_open,  R.string.drawer_close);
        drawerToggle.setDrawerIndicatorEnabled(true);
        drawerToggle.syncState();
        mDrawer.addDrawerListener(drawerToggle);

        checkSensor();

        serviceMotion = new Motion(this);
    }

    private void setupDrawerContent(NavigationView navigationView)
    {
        navigationView.setNavigationItemSelectedListener(
                menuItem -> {
                    selectDrawerItem(menuItem);
                    return true;
                });
    }

    public void selectDrawerItem(MenuItem menuItem)
    {

        menuItem.setChecked(true);
        toolbar.setTitle(menuItem.getTitle());
        mDrawer.closeDrawers();
    }

    @Override
    public void onPostCreate(@Nullable Bundle savedInstanceState, @Nullable PersistableBundle persistentState)
    {
        super.onPostCreate(savedInstanceState, persistentState);
        drawerToggle.syncState();
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig)
    {
        super.onConfigurationChanged(newConfig);
        drawerToggle.onConfigurationChanged(newConfig);
    }

    @Override
    public void onResume()
    {
        super.onResume();
    }

    @Override
    public void onPause()
    {
        super.onPause();
    }

    @Override
    public void onDestroy()
    {
        super.onDestroy();
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item)
    {
        if (drawerToggle.onOptionsItemSelected(item))
        {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    private void checkBTH()
    {
        BluetoothAdapter test = BluetoothAdapter.getDefaultAdapter();
        if(test == null)
        {
            new AlertDialog.Builder(this)
                    .setTitle("Not Compatible")
                    .setMessage("Your phone does not support Bluetooth")
                    .setPositiveButton("OK", (dialog, which) -> System.exit(0))
                    .show();
            return;
        }

        if(!test.isEnabled())
        {
            Intent enableBTH = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
            startActivity(enableBTH);
        }
    }

    public void checkSensor()
    {
        SensorManager test = (SensorManager)getSystemService(Context.SENSOR_SERVICE);
        if(test == null || test.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD) == null || test.getDefaultSensor(Sensor.TYPE_ACCELEROMETER) == null)
        {
            new AlertDialog.Builder(this)
                    .setTitle("Not Compatible")
                    .setMessage("Your phone does not have required sensors")
                    .setPositiveButton("OK", (dialog, which) -> System.exit(0))
                    .show();
        }
    }

    public void pressX(View view) { Connection.buffer.addData(MotionButton.X); }

    public void pressY(View view)
    {
        Connection.buffer.addData(MotionButton.Y);
    }

    public void pressA(View view)
    {
        Connection.buffer.addData(MotionButton.A);
    }

    public void pressB(View view)
    {
        Connection.buffer.addData(MotionButton.B);
    }

    public void pressLB(View view)
    {
        Connection.buffer.addData(MotionButton.LB);
    }

    public void pressRB(View view)
    {
        Connection.buffer.addData(MotionButton.RB);
    }

    public void pressBACK(View view)
    {
        Connection.buffer.addData(MotionButton.BACK);
    }

    public void pressSTART(View view)
    {
        Connection.buffer.addData(MotionButton.START);
    }

    public void pressUP(View view)
    {
        Connection.buffer.addData(MotionButton.UP);
    }

    public void pressDOWN(View view)
    {
        Connection.buffer.addData(MotionButton.DOWN);
    }

    public void pressLEFT(View view)
    {
        Connection.buffer.addData(MotionButton.LEFT);
    }

    public void pressRIGHT(View view)
    {
        Connection.buffer.addData(MotionButton.RIGHT);
    }
}
