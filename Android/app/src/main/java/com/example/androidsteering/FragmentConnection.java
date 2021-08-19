package com.example.androidsteering;

import androidx.fragment.app.Fragment;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.RadioGroup;

import androidx.annotation.Nullable;

public class FragmentConnection extends Fragment
{
    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.inflate(R.layout.frag_connection, container, false);
    }

    @Override
    public void onViewCreated(View view, @Nullable Bundle savedInstanceState)
    {
        super.onViewCreated(view, savedInstanceState);
        MainActivity activity = (MainActivity)getActivity();
        assert activity != null;
        ConnectionMode mode = activity.getConnectionMode();
        RadioGroup group = activity.findViewById(R.id.radioGroup);
        Button button = activity.findViewById(R.id.buttonConnect);
        if(mode == ConnectionMode.Bluetooth)
        {
            group.check(R.id.radioButtonBth);
        }
        else
        {
            group.check(R.id.radioButtonWifi);
        }
        boolean connected = activity.isConnected();
        group.setEnabled(!connected);
        if(connected)
        {
            button.setText(R.string.buttonDisconnect);
        }
        else
        {
            button.setText(R.string.buttonConnect);
        }
        activity.setRadioGroupCallback();
    }
}
