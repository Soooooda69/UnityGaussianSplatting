import socket
import time
import random
import cv2
import os
from natsort import natsorted
import struct
import threading
from PIL import Image
import io
import signal

class StreamData:
    def __init__(self):
        self.host = 'localhost'
        self.pubPort = 8888
        self.subPort = 8889
        self.pose = "0 0 0 0 0 0 1"
        self.poses = []
        images_dir = 'data/key_images_h'
        self.images = natsorted([os.path.join(images_dir, img) for img in os.listdir(images_dir)])
        self.image_ids = [int(img.split('.png')[0]) for img in os.listdir(images_dir)]
        self.load_pose_data()
        # self.start_server()
        self.stopThreads = False
        self.start_threading()
        
    def generate_pose_data(self):
        # Generate random pose data for demonstration purposes
        pos_x = random.uniform(-10, 10)
        pos_y = random.uniform(-10, 10)
        pos_z = random.uniform(-10, 10)
        rot_x = random.uniform(-1, 1)
        rot_y = random.uniform(-1, 1)
        rot_z = random.uniform(-1, 1)
        rot_w = random.uniform(-1, 1)
        return f"{pos_x} {pos_y} {pos_z} {rot_x} {rot_y} {rot_z} {rot_w}"

    def load_pose_data(self):
        # Load pose data from a file or other source
        with open('data/poses_pred.txt', 'r') as file:
            file.readline() # Skip the header
            for line in file:
                pose = line.strip().split(' ')
                pose = ' '.join(pose[0:])
                self.poses.append(pose)
        
    def start_server(self):
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.bind((self.host, self.pubPort))
        server_socket.listen(1)
        print(f"Server started on {self.host}:{self.pubPort}")
        
        while True:
            client_socket, address = server_socket.accept()
            print(f"Connected to client: {address}")

            while True:                
                for image, pose in zip(self.images, self.poses):
                    client_socket.send(pose.encode('ascii'))
                    print(f"Sent pose data: {pose}")
                    img = cv2.imread(image)
                    cv2.imshow('image', img)
                    cv2.waitKey(0)
                    # breakpoint()
                    time.sleep(0.1)  # Adjust the delay as needed
    
    def start_threading(self):
        publish_thread = threading.Thread(target=self.publish_data)
        subscribe_thread = threading.Thread(target=self.subscribe_data)

        publish_thread.start()
        # subscribe_thread.start()

        publish_thread.join()
        # subscribe_thread.join()
    
    # def signal_handler(sig, frame):
    #     global running
    #     print('Ctrl+C pressed. Exiting gracefully...')
    #     running = False
    #  # Register the signal handler for Ctrl+C
    # signal.signal(signal.SIGINT, signal_handler)
    
    def publish_data(self):
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.bind((self.host, self.pubPort))
        server_socket.listen(1)
        print(f"Server started on {self.host}:{self.pubPort}")
        
        while True:
            if self.stopThreads:
                break
            try:
                client_socket, address = server_socket.accept()
                print(f"Connected to client: {address}")
                while True: 
                    for image, pose in zip(self.images, self.poses):

                        client_socket.send(pose.encode('ascii'))
                        print(f"Sent pose data: {pose}")
                        img = cv2.imread(image)
                        cv2.imshow('image', img)
                        cv2.waitKey(0)
                        time.sleep(0.1)  # Adjust the delay as needed
            except Exception as e:
                print(f"Failed to send data: {e}")
                self.stopThreads = True
                break
                
        # while True:
        #     try:
        #         # Establish TCP connection to Unity
        #         with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        #             sock.connect((self.host, self.pubPort))

        #             for image, pose in zip(self.images, self.poses):
        #                 sock.send(pose.encode('ascii'))
        #                 print(f"Sent pose data: {pose}")
        #                 img = cv2.imread(image)
        #                 cv2.imshow('image', img)
        #                 cv2.waitKey(0)
        #                 # breakpoint()
        #                 time.sleep(0.1)  # Adjust the delay as needed

        #     except Exception as e:
        #         print(f"Failed to send data: {e}")
        #         break
    
    
    def subscribe_data(self):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_sock:
            server_sock.bind((self.host, self.subPort))
            server_sock.listen(1)
            print(f"Listening for incoming connections on {self.host}:{self.subPort}")

            while True:
                if self.stopThreads:
                    break
                connection, client_address = server_sock.accept()
                print(f"Connection from {client_address}")

                try:
                    # Read the length of the incoming data
                    length_prefix = connection.recv(4)
                    if len(length_prefix) < 4:
                        break

                    data_length = struct.unpack('!I', length_prefix)[0]
                    # print(f"Expecting to receive {data_length} bytes of image data")

                    # Read the image data
                    image_data = bytearray()
                    while len(image_data) < data_length:
                        packet = connection.recv(4096)
                        if not packet:
                            break
                        image_data.extend(packet)

                    if len(image_data) == 0:
                        print(f"Waiting for image data...")
                        continue

                    # Process the received image data
                    image = Image.open(io.BytesIO(image_data))
                    # image.show()  # Display the image
                    image.save('received_image.png')  # Save the image
                    print("Image received and saved.")
                except Exception as e:
                    print(f"Error receiving data: {e}")
                    self.stopThreads = True
                    break
                
if __name__ == '__main__':
    camPose = StreamData()
   
