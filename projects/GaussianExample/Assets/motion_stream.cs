// SPDX-License-Identifier: MIT

using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MainCameraController : MonoBehaviour
{
    private TcpClient subClient;
    private TcpClient pubClient;
    private Thread receiveThread;
    private Thread publishThread;
    private int ID;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float vFOV;
    private int cameraImageWidth;
    private int cameraImageHeight;
    public Image displayImage;
    string imageDirectory = "../../data/key_images_h";
    private byte[] imageBytes;
    private void Start()
    {
        ConnectToServer();
        float hFOV = 48f; // Horizontal field of view in degrees
        cameraImageWidth = 541;
        cameraImageHeight = 541;
        float aspectRatio = (float)cameraImageWidth / (float)cameraImageHeight;
        vFOV = 2f * Mathf.Atan(Mathf.Tan(hFOV * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
        // int imageWidth = 541;
        // int imageHeight = 541;
        // Camera.main.fieldOfView = vFOV;     
    }

    private void ConnectToServer()
    {
        try
        {
            subClient = new TcpClient("localhost", 8888);
            // pubClient = new TcpClient("localhost", 8889);
            receiveThread = new Thread(new ThreadStart(ReceivePoses));
            receiveThread.Start();
            // publishThread = new Thread(() => PublishImageData(imageBytes));
            // publishThread = new Thread(new ThreadStart(PublishImageData));
            // publishThread.Start();
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
                NetworkStream stream = subClient.GetStream();
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

    //  private void PublishImageData()
    // {
    //     try
    //     {
    //         while (true)
    //         {
    //             NetworkStream stream = pubClient.GetStream();
            
    //             byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);
    //             stream.Write(lengthPrefix, 0, lengthPrefix.Length);
    //             Debug.Log("Image data length " + imageBytes.Length);
    //             // Send the actual image data
    //             if (imageBytes.Length == 0)
    //             {
    //                 Debug.Log("Image data length is 0");
    //                 continue;
    //             }
    //             stream.Write(imageBytes, 0, imageBytes.Length);
    //             Debug.Log("Image data sent successfully."+ DateTime.Now.ToString());
    //             // // Close the connection
    //             // stream.Close();
    //             // client.Close();
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.Log("Error publishing image data: " + e.Message);
    //     }
    // }

    private bool isCapturing = false;

    private void Update()
    {
        UpdateMainCameraPose();
        string IDString = ID.ToString();
        LoadImage(IDString);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isCapturing)
            {
                StopCaptureAndSaveImage();
            }
            else
            {
                StartCaptureAndSaveImage();
            }
        }
        
        if (isCapturing)
        {
            CaptureAndSaveImage();
        }
    }

    private void LoadImage(string imageID)
    {
        // Construct the image file path based on the img_ID
        string imageFilePath = Path.Combine(imageDirectory, imageID + ".png");
        // Check if the image file exists
        if (File.Exists(imageFilePath))
        {
            // Debug.Log("Loading image: " + imageFilePath);
            // Load the image file as a byte array
            byte[] imageBytes = File.ReadAllBytes(imageFilePath);

            // Create a texture from the image bytes
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            // Create a sprite from the texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

            // Assign the sprite to the UI Image
            displayImage.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("Image file not found: " + imageFilePath);
        }
    }

    private void StartCaptureAndSaveImage()
    {
        Debug.Log("Capture started."+ DateTime.Now.ToString());
        isCapturing = true;
        // Add code to start capturing and saving images
    }

    private void StopCaptureAndSaveImage()
    {
        Debug.Log("Capture stopped."+ DateTime.Now.ToString());
        isCapturing = false;
        // Add code to stop capturing and saving images
    }

    private void UpdateMainCameraPose()
    {
        // Camera[] cameras = Camera.allCameras;
        // Camera mainCam = Camera.main;
        // foreach (Camera mainCam in cameras)
        // {
        //     GameObject gaussianSplatsObject = GameObject.Find("GaussianSplats");
        //     gaussianSplatsObject.transform.rotation = Quaternion.Euler(-180, 0, 0);
        //     if (gaussianSplatsObject != null)
        //     {
        //     mainCam.transform.position = cameraPosition;
        //     mainCam.transform.rotation = cameraRotation;
        //     }
        // }

        // Camera mainCam = Camera.allCameras[1];
        // if (mainCam != null)
        Camera[] cameras = Camera.allCameras;
        foreach (Camera mainCam in cameras)
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
        Camera[] cameras = Camera.allCameras;
        Camera mainCam = cameras[1];
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

            imageBytes = screenshot.EncodeToPNG();
            string imagePath = Path.Combine(Application.dataPath, "../../../data/raw_masks", $"mask_{ID}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
            File.WriteAllBytes(imagePath, imageBytes);

            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Image captured and saved: {imagePath}");

            // Publish the image data to the server
            // Thread publishThread = new Thread(() => PublishImageData(imageBytes));
            // publishThread.Start();
        }
    }

    private void OnDestroy()
    {
        if (receiveThread != null)
        {
            receiveThread.Abort();
            receiveThread = null;
        }
        // if (publishThread != null)
        // {
        //     publishThread.Abort();
        //     publishThread = null;
        // }

        if (subClient != null)
        {
            subClient.Close();
            subClient = null;
        }
        // if (pubClient != null)
        // {
        //     pubClient.Close();
        //     pubClient = null;
        // }
    }
}