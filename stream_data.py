import socket
import time
import random
import cv2
import os
from natsort import natsorted

class StreamData:
    def __init__(self):
        self.host = 'localhost'
        self.port = 8888
        self.pose = "0 0 0 0 0 0 1"
        self.poses = []
        images_dir = 'test_data/key_images_h'
        self.images = natsorted([os.path.join(images_dir, img) for img in os.listdir(images_dir)])
        self.image_ids = [int(img.split('.png')[0]) for img in os.listdir(images_dir)]
        self.load_pose_data()
        self.start_server()
        
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
        with open('test_data/poses_pred.txt', 'r') as file:
            file.readline() # Skip the header
            for line in file:
                pose = line.strip().split(' ')
                pose = ' '.join(pose[0:])
                self.poses.append(pose)
        
    def start_server(self):
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.bind((self.host, self.port))
        server_socket.listen(1)
        print(f"Server started on {self.host}:{self.port}")

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

if __name__ == '__main__':
    camPose = StreamData()