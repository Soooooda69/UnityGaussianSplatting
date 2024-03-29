import cv2
import numpy as np
import os
from natsort import natsorted

dir = 'test_data\key_images_masks_h'
for img in natsorted(os.listdir(dir)): 
    # Load the image
    print(img)
    image = cv2.imread(os.path.join(dir, img))

    # Convert the image from BGR to HSV color space
    hsv_image = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)

    # Define lower and upper boundaries for green color in HSV
    lower_green = np.array([40, 50, 50])
    upper_green = np.array([80, 255, 255])

    # Create a binary mask
    mask = cv2.inRange(hsv_image, lower_green, upper_green)

    # Apply morphological operations to remove noise and fill gaps
    kernel = np.ones((5, 5), np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)

    # Find contours in the binary mask
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    # Select the largest contour as the desired green region
    largest_contour = max(contours, key=cv2.contourArea)

    # Create a black background for the segmentation mask
    segmentation_mask = np.zeros(image.shape[:2], dtype=np.uint8)

    # Draw the filled contour on the segmentation mask
    cv2.drawContours(segmentation_mask, [largest_contour], 0, (255, 255, 255), cv2.FILLED)

    # Create a copy of the segmentation mask for the curved contour
    curved_mask = segmentation_mask.copy()

    # Apply Gaussian blur to smooth the edges
    curved_mask = cv2.GaussianBlur(curved_mask, (15, 15), 0)

    # Threshold the blurred mask to create a binary mask
    _, curved_mask = cv2.threshold(curved_mask, 127, 255, cv2.THRESH_BINARY)

    # Find contours in the curved mask
    curved_contours, _ = cv2.findContours(curved_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    # Draw the curved contour on the segmentation mask
    cv2.polylines(segmentation_mask, curved_contours, True, (255, 255, 255), 2)

    # Display the original image and the segmentation mask
    cv2.imshow('Original Image', image)
    cv2.imshow('Segmentation Mask', segmentation_mask)
    cv2.waitKey(0)
    cv2.destroyAllWindows()
    # break