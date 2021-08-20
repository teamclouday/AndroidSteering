package com.example.androidsteering;

import androidx.fragment.app.Fragment;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;

import androidx.annotation.Nullable;

public class FragmentControlPad extends Fragment
{
    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState)
    {
        return inflater.inflate(R.layout.frag_control_pad, container, false);
    }

    @SuppressLint("ClickableViewAccessibility")
    @Override
    public void onViewCreated(View view, @Nullable Bundle savedInstanceState)
    {
        super.onViewCreated(view, savedInstanceState);
        MainActivity activity = (MainActivity)getActivity();
        assert activity != null;
        Button buttonLT = activity.findViewById(R.id.buttonLTPad);
        Button buttonRT = activity.findViewById(R.id.buttonRTPad);
        buttonLT.setOnTouchListener(activity::touchLT);
        buttonRT.setOnTouchListener(activity::touchRT);
    }
}
