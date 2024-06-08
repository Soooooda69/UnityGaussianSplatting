using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
public class processing : MonoBehaviour
{
    private int cameraImageWidth;
    private int cameraImageHeight;
    private float hFOV;
    private int ID;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private struct PoseData
    {
        public int ID;
        public Vector3 position;
        public Quaternion rotation;
    };
    private List<PoseData> poseDataList = new List<PoseData>();
    // Start is called before the first frame update
    void Start()
    {
        // hFOV = 46f; // Horizontal field of view in degrees
        cameraImageWidth = 541;
        cameraImageHeight = 541;
        LoadPose();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            CaptureImages();
        }
    }
    
    private void LoadPose()
    {
        string filePath = "../../data/poses_pred.txt"; // Replace with the actual file path
        string[] lines = System.IO.File.ReadAllLines(filePath);
        
        foreach (string line in lines)
        {
            if (line.StartsWith("#"))
            {
                continue;
            }
            string[] poseData = line.Split(' ');
            PoseData poseInfo = new PoseData();
            // Process the values here
            if (poseData.Length == 8)
                {
                    int imgID = int.Parse(poseData[0]);
                    float posX = float.Parse(poseData[1]);
                    float posY = float.Parse(poseData[2]);
                    float posZ = float.Parse(poseData[3]);
                    float rotX = float.Parse(poseData[4]);
                    float rotY = float.Parse(poseData[5]);
                    float rotZ = float.Parse(poseData[6]);
                    float rotW = float.Parse(poseData[7]);
                    poseInfo.position = new Vector3(posX, -posY, posZ);
                    poseInfo.rotation = new Quaternion(-rotX, rotY, -rotZ, rotW);
                    poseInfo.ID = imgID;
                    poseDataList.Add(poseInfo);
                }       
        }
    }
    private void CaptureImages()
    {
        Camera[] cameras = Camera.allCameras;
        Debug.Log($"Number of cameras: {cameras}");
        Camera mainCam = cameras[1];
        foreach (PoseData poseInfo in poseDataList)
        {
            // Update camera pose
            transform.position = poseInfo.position;
            transform.rotation = poseInfo.rotation;
            // Save image
            RenderTexture renderTexture = new RenderTexture(cameraImageWidth, cameraImageHeight, 24);
            mainCam.targetTexture = renderTexture;
            Texture2D screenshot = new Texture2D(cameraImageWidth, cameraImageHeight, TextureFormat.RGB24, false);
            mainCam.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, cameraImageWidth, cameraImageHeight), 0, 0);
            mainCam.targetTexture = null;
            RenderTexture.active = null;

            byte[] imageBytes = screenshot.EncodeToPNG();
            string imagePath = Path.Combine("../../test_data/test", $"mask_{poseInfo.ID}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
            File.WriteAllBytes(imagePath, imageBytes);

            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Image captured and saved: {imagePath}");
        }
    }
}
