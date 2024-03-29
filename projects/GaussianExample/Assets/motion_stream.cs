// SPDX-License-Identifier: MIT

using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class MainCameraController : MonoBehaviour
{
    private TcpClient client;
    private Thread receiveThread;
    private int ID;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float vFOV;
    private int cameraImageWidth;
    private int cameraImageHeight;
    private void Start()
    {
        ConnectToServer();
        float hFOV = 46f; // Horizontal field of view in degrees
        cameraImageWidth = 541;
        cameraImageHeight = 541;
        float aspectRatio = (float)cameraImageWidth / (float)cameraImageHeight;
        vFOV = 2f * Mathf.Atan(Mathf.Tan(hFOV * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;

        int imageWidth = 541;
        int imageHeight = 541;
        
        Camera.main.fieldOfView = vFOV;
    }

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient("localhost", 8888);
            receiveThread = new Thread(new ThreadStart(ReceivePoses));
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("Error connecting to server: " + e.Message);
        }
    }

    private void ReceivePoses()
    {
        try
        {
            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Parse the received pose data
                string[] poseData = message.Split(' ');
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
                    // Debug.Log($"Received pose: ID={imgID}, position=({posX}, {posY}, {posZ}), rotation=({rotX}, {rotY}, {rotZ}, {rotW})");
                    cameraPosition = new Vector3(posX, -posY, posZ);
                    cameraRotation = new Quaternion(-rotX, rotY, -rotZ, rotW);
                    ID = imgID;
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error receiving poses: " + e.Message);
        }
    }

    private void Update()
    {
        UpdateMainCameraPose();
        CaptureAndSaveImage();
    }

    private void UpdateMainCameraPose()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            GameObject gaussianSplatsObject = GameObject.Find("GaussianSplats");
            gaussianSplatsObject.transform.rotation = Quaternion.Euler(-180, 0, 0);
            if (gaussianSplatsObject != null)
            {
            // Set the main camera's parent to the "GaussianSplats" object
            // mainCam.transform.parent = gaussianSplatsObject.transform;
            // Update the main camera's position and rotation
            mainCam.transform.position = cameraPosition;
            mainCam.transform.rotation = cameraRotation;
            }
        }
    }
    private void CaptureAndSaveImage()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            RenderTexture renderTexture = new RenderTexture(cameraImageWidth, cameraImageHeight, 24);
            mainCam.targetTexture = renderTexture;
            Texture2D screenshot = new Texture2D(cameraImageWidth, cameraImageHeight, TextureFormat.RGB24, false);
            mainCam.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, cameraImageWidth, cameraImageHeight), 0, 0);
            mainCam.targetTexture = null;
            RenderTexture.active = null;

            byte[] imageBytes = screenshot.EncodeToPNG();
            string imagePath = Path.Combine(Application.dataPath, "../../../test_data/key_images_masks_h", $"mask_{ID}.png");
            Debug.Log($"Saving image to: {imagePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
            File.WriteAllBytes(imagePath, imageBytes);

            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Image captured and saved: {imagePath}");
        }
    }

    private void OnDestroy()
    {
        if (receiveThread != null)
        {
            receiveThread.Abort();
            receiveThread = null;
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }
    }
}