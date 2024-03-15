// SPDX-License-Identifier: MIT
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    [EditorTool("Gaussian Measure Tool", typeof(GaussianSplatRenderer), typeof(GaussianToolContext))]
    class GaussianMeasureTool : GaussianTool
    {
        private Vector3 m_StartPoint;
        private bool m_MeasureStarted;
        Vector3 point1 = Vector3.zero;
        Vector3 point2 = Vector3.zero;

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView))
                return;

            var gs = GetRenderer();
            if (!gs || !CanBeEdited())
                return;

            var evt = Event.current;
            var camera = SceneView.currentDrawingSceneView.camera;

            // Rect region
            var center = evt.mousePosition;
            float halfWidth = 5;
            float halfHeight = 5;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                Vector2 topLeft = new Vector2(center.x - halfWidth, center.y - halfHeight);
                Vector2 bottomRight = new Vector2(center.x + halfWidth, center.y + halfHeight);
                Rect rect = FromToRect(topLeft, bottomRight);

                Vector2 rectMin = HandleUtility.GUIPointToScreenPixelCoordinate(rect.min);
                Vector2 rectMax = HandleUtility.GUIPointToScreenPixelCoordinate(rect.max);
                gs.EditUpdateSelection(rectMin, rectMax, sceneView.camera, false);
                GaussianSplatRendererEditor.RepaintAll();

                if (!m_MeasureStarted)
                {
                    point1 = GetSelectionCenterLocal();
                    Debug.Log($"point1: {point1}");
                    m_MeasureStarted = true;
                }
                else
                {
                    point2 = GetSelectionCenterLocal();
                    Debug.Log($"point2: {point2}");
                    m_MeasureStarted = false;

                    // Calculate and display the distance between point1 and point2
                    float distance = Vector3.Distance(gs.transform.TransformPoint(point1), gs.transform.TransformPoint(point2));
                    Debug.Log($"Distance: {distance}");
                }

                evt.Use();
            }

            if (m_MeasureStarted && point1 != Vector3.zero)
            {
                // Draw a line between the start point and the current mouse position
                Handles.color = Color.green;
                m_StartPoint = gs.transform.TransformPoint(point1);
                Handles.DrawLine(m_StartPoint, HandleUtility.GUIPointToWorldRay(evt.mousePosition).origin);
            }
            else if (!m_MeasureStarted && point1 != Vector3.zero && point2 != Vector3.zero)
            {
                // Draw a line between point1 and point2
                Handles.color = Color.green;
                Handles.DrawLine(gs.transform.TransformPoint(point1), gs.transform.TransformPoint(point2));
            }
        }
    }
}