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
        private Vector3 m_EndPoint;
        private bool m_MeasureStarted;
        Vector3 point1 = Vector3.zero;
        Vector3 point2 = Vector3.zero;
        private GameObject m_StartMarker;
        private GameObject m_EndMarker;
        private LineRenderer m_LineRenderer;
        public override void OnActivated()
        {
            base.OnActivated();

            // Create marker objects and line renderer
            m_StartMarker = CreateMarker();
            m_EndMarker = CreateMarker();
            m_LineRenderer = CreateLineRenderer();
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            if (m_StartMarker != null)
                DestroyImmediate(m_StartMarker);
            if (m_EndMarker != null)
                DestroyImmediate(m_EndMarker);
            if (m_LineRenderer != null)
                DestroyImmediate(m_LineRenderer.gameObject);
        }
        private GameObject CreateMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.localScale = Vector3.one * 0.05f;
            marker.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            marker.hideFlags = HideFlags.HideAndDontSave;
            return marker;
        }

        private LineRenderer CreateLineRenderer()
        {
            GameObject lineObject = new GameObject("MeasureLine");
            lineObject.hideFlags = HideFlags.HideAndDontSave;
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.sharedMaterial.color = Color.green;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.positionCount = 2;
            return lineRenderer;
        }

        private void UpdateMarkers()
        {
            if (m_StartMarker != null)
                m_StartMarker.transform.position = m_StartPoint;

            if (m_EndMarker != null)
                m_EndMarker.transform.position = m_EndPoint;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.SetPosition(0, m_StartPoint);
                m_LineRenderer.SetPosition(1, m_EndPoint);
            }
        }
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
            // if (point1 == Vector3.zero && point2 == Vector3.zero)
            // {
            //     Debug.Log("point1 or point2 is zero");
            //     if (m_StartMarker != null)
            //         DestroyImmediate(m_StartMarker);
            //     if (m_EndMarker != null)
            //         DestroyImmediate(m_EndMarker);
            //     if (m_LineRenderer != null)
            //         DestroyImmediate(m_LineRenderer.gameObject);
            // }
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
                    Debug.Log($"point1: {gs.transform.TransformPoint(point1)}");
                    m_MeasureStarted = true;
                }
                else
                {
                    point2 = GetSelectionCenterLocal();
                    Debug.Log($"point2: {gs.transform.TransformPoint(point2)}");
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
                UpdateMarkers();
                Handles.DrawLine(m_StartPoint, HandleUtility.GUIPointToWorldRay(evt.mousePosition).origin);
            }
            else if (!m_MeasureStarted && point1 != Vector3.zero && point2 != Vector3.zero)
            {
                // Draw a line between point1 and point2
                Handles.color = Color.green;
                m_EndPoint = gs.transform.TransformPoint(point2);
                Handles.DrawLine(gs.transform.TransformPoint(point1), m_EndPoint);
                UpdateMarkers();
            }
        }
    }
}