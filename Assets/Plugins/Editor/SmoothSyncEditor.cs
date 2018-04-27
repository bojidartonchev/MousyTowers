using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Smooth
{
    [CustomEditor(typeof(SmoothSync))]
    public class SmoothSyncEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SmoothSync myTarget = (SmoothSync)target;

            if (myTarget.childObjectToSync)
            {
                Color oldColor = GUI.contentColor;
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("Syncing child", myTarget.childObjectToSync.name);
                GUI.contentColor = oldColor;
            }
            DrawDefaultInspector();
        }
    }
}