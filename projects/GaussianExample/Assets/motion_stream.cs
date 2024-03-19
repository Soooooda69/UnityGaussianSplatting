// SPDX-License-Identifier: MIT

using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class MainCameraController : MonoBehaviour
{
    private TcpClient client;
    private Thread receiveThread;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float vFOV;
    private int cameraImageWidth;
    private int cameraImageHeight;
    private void Start()
    {
        ConnectToServer();
        float hFOV = 46.7f; // Horizontal field of view in degrees
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
                if (poseData.Length == 7)
                {
                    float posX = float.Parse(poseData[0]);
                    float posY = float.Parse(poseData[1]);
                    float posZ = float.Parse(poseData[2]);
                    float rotX = float.Parse(poseData[3]);
                    float rotY = float.Parse(poseData[4]);
                    float rotZ = float.Parse(poseData[5]);
                    float rotW = float.Parse(poseData[6]);

                    cameraPosition = new Vector3(posX, -posY, posZ);
                    cameraRotation = new Quaternion(-rotX, rotY, -rotZ, rotW);
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
    }

    private void UpdateMainCameraPose()
    {
        if (Camera.main != null)
        {
            // Update the main camera's position and rotation
            Camera.main.transform.position = cameraPosition;
            Camera.main.transform.rotation = cameraRotation;
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