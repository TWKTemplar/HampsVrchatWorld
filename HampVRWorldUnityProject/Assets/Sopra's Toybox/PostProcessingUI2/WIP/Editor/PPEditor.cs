using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[CustomEditor(typeof(PPE))]
public class PPEditor : Editor
{

    public override void OnInspectorGUI()
    {

        //stuff to make it work
        PPE ppe = (PPE)target;
        EditorUtility.SetDirty(ppe);

        var align = new GUIStyle(EditorStyles.boldLabel);
        var header = new GUIStyle(EditorStyles.boldLabel);

        align.alignment = TextAnchor.MiddleCenter;
        header.alignment = TextAnchor.MiddleCenter;
        header.fontSize = 26;

        //Actual UI                
        GUILayout.Label("Post Processing Effects", header);
        GUILayout.Label("");

        if (GUILayout.Button("Update All"))
        {
            ppe.UpdatePP();
        }

        GUILayout.Label("");

        ppe.sound = EditorGUILayout.Toggle("Button sounds?", ppe.sound);

        GUILayout.Label("");

        ppe.aoWeight = EditorGUILayout.Slider("Ambiant Occlusion", ppe.aoWeight, 0, 1);
        ppe.aoMode = EditorGUILayout.Toggle("AO MSVO by default?", ppe.aoMode);

        GUILayout.Label("");

        ppe.bloomWeight = EditorGUILayout.Slider("Bloom", ppe.bloomWeight, 0, 1);

        ppe.caWeight = EditorGUILayout.Slider("Chromatic Aberration", ppe.caWeight, 0, 1);

        ppe.vnWeight = EditorGUILayout.Slider("Vingette", ppe.vnWeight, 0, 1);

        ppe.mbMode = EditorGUILayout.Toggle("Motion Blur by default?", ppe.mbMode);

        GUILayout.Label("");
        GUILayout.Label("Color Correction", align);

        ppe.conWeight = EditorGUILayout.Slider("Contrast", ppe.conWeight, -1, 1);

        ppe.satWeight = EditorGUILayout.Slider("Saturation", ppe.satWeight, -1, 1);

        ppe.expWeight = EditorGUILayout.Slider("Exposure", ppe.expWeight, -1, 1);

        ppe.tempWeight = EditorGUILayout.Slider("Temperature", ppe.tempWeight, -1, 1);

        ppe.tintWeight = EditorGUILayout.Slider("Tint", ppe.tintWeight, -1, 1);
        ppe.ccMode = EditorGUILayout.Toggle("CC FILMIC as default?", ppe.ccMode);

        GUILayout.Label(""); GUILayout.Label("");
        GUILayout.Label("In case things break", align);
        base.OnInspectorGUI();
    }
}
