package com.example.androidsteering;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

public class FragmentControlDefault extends Fragment {
    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState) {
        return inflater.inflate(R.layout.frag_control_default, container, false);
    }

    @SuppressLint("ClickableViewAccessibility")
    @Override
    public void onResume() {
        super.onResume();
        MainActivity activity = (MainActivity) getActivity();
        assert activity != null;

        activity.findViewById(R.id.buttonA).setOnTouchListener(activity::touchA);
        activity.findViewById(R.id.buttonB).setOnTouchListener(activity::touchB);
        activity.findViewById(R.id.buttonX).setOnTouchListener(activity::touchX);
        activity.findViewById(R.id.buttonY).setOnTouchListener(activity::touchY);
    }
}
