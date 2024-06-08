import numpy as np
import cv2
import argparse
import os
from natsort import natsorted
from scipy.stats import ttest_rel
import matplotlib.pyplot as plt

def threshold_mask(mask_path, threshold, set_value=255):
    mask = cv2.imread(mask_path, cv2.IMREAD_GRAYSCALE)
    mask = cv2.threshold(mask, threshold, set_value, cv2.THRESH_BINARY)[1]
    mask = mask / 255
    return mask

def load_images(path_1, path_2, path_gt):
    gt_masks = natsorted([mask for mask in os.listdir(path_gt) if mask.endswith('.png')])
    idx = [name.split('.')[0] for name in gt_masks]
    gt = {}
    mask1 = {}
    mask2 = {}
    raw_img = {}
    for id , mask in zip(idx, gt_masks):
        # mask = threshold_mask(os.path.join(path_gt, mask), 30)
        mask = cv2.imread(os.path.join(path_gt, f'{id}.png'), cv2.IMREAD_GRAYSCALE) / 255
        if mask.sum() == 0:
            print(f"No contours found in {id}")
            continue
        # cv2.imwrite(f'./data/masks/mask_{id}.png', mask* 255)
        gt[id] = mask
        mask1[id] = cv2.imread(os.path.join(path_1, f'mask_{id}.png'), cv2.IMREAD_GRAYSCALE) / 255
        mask2[id] = threshold_mask(os.path.join(path_2, f'{id}.png'), 30)
        raw_img[id] = cv2.imread(os.path.join('./data/key_images_h', f'{id}.png'))
    return mask1, mask2, gt, raw_img

def calculate_iou(mask1, mask2):
    intersection = np.logical_and(mask1, mask2)
    union = np.logical_or(mask1, mask2)
    iou = np.sum(intersection) / np.sum(union)
    return iou

def draw_iou(mask1, mask2, gt, raw_img):
    for i, idx in enumerate(gt.keys()):
        m1 = mask1[idx]*255
        m2 = mask2[idx]*255
        g = gt[idx]*255
        m1 = cv2.cvtColor(m1.astype(np.uint8), cv2.COLOR_GRAY2RGB)
        m2 = cv2.cvtColor(m2.astype(np.uint8), cv2.COLOR_GRAY2RGB)
        g = cv2.cvtColor(g.astype(np.uint8), cv2.COLOR_GRAY2RGB)
        # Overlay image and gt
        overlay1 = cv2.addWeighted(m1, 0.5, raw_img[idx], 0.7, 0)
        overlay2 = cv2.addWeighted(m2, 0.5, raw_img[idx], 0.7, 0)
        overlay3 = cv2.addWeighted(g, 0.5, raw_img[idx], 0.7, 0)
        
        # # Show overlay using matplotlib
        # plt.figure(figsize=(16, 10))
        # plt.subplot(3, 1, 1)
        # plt.suptitle(f'Overlay Masks with IoU Scores {idx}', fontsize=20)
        # plt.imshow(overlay2, cmap='gray')
        # plt.title(f'Mask 1\nIoU: {cutie_iou[i]:.3f}', fontsize=15)
        # plt.axis('off')
        # plt.subplot(3, 1, 2)
        # plt.imshow(overlay1, cmap='gray')
        # plt.title(f'Mask 2\nIoU: {ar_iou[i]:.3f}', fontsize=15)

        # plt.axis('off')
        # plt.subplot(3, 1, 3)
        # plt.imshow(overlay3, cmap='gray')
        # plt.title('\ngt', fontsize=15)
        # plt.axis('off')
        # plt.show()

def calculate_prc_rc(mask, gt):
    
    # True Positives (TP)
    tp = np.sum((mask == 1) & (gt == 1))
    
    # False Positives (FP)
    fp = np.sum((mask == 1) & (gt == 0))
    
    # False Negatives (FN)
    fn = np.sum((mask == 0) & (gt == 1))
    
    # Precision: TP / (TP + FP)
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0
    
    # Recall: TP / (TP + FN)
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0
    print("Precision:", precision, "Recall:", recall)
    return precision, recall

# def plot_save_pr_curves(PRE, REC):
#     # Plotting the Precision-Recall curve
#     plt.figure(figsize=(8, 6))
#     plt.plot(REC, PRE, marker='o')
#     plt.xlabel('Recall')
#     plt.ylabel('Precision')
#     plt.title('Precision-Recall Curve')
#     plt.grid(True)
#     plt.show()
    
def plot_save_pr_curves(REC1, PRE1,REC2, PRE2):
    # Plotting the Precision-Recall curve
    plt.figure(figsize=(8, 6))
    plt.plot(REC1, PRE1, marker='o', label='Mask 1')
    plt.plot(REC2, PRE2, marker='o', label='Mask 2')
    plt.xlabel('Recall')
    plt.ylabel('Precision')
    plt.title('Precision-Recall Curve')
    plt.grid(True)
    plt.legend()
    plt.show()

if __name__ == "__main__":
    argparser = argparse.ArgumentParser(description='Calculate the Intersection over Union (IoU) score between two binary masks')
    argparser.add_argument('--root', type=str, help='Threshold value for binarizing the masks')
    args = argparser.parse_args()
    
    mask1_path = os.path.join(args.root, 'ar')
    mask2_path = os.path.join(args.root, 'cutie')
    gt_path = os.path.join(args.root, 'gt')
    mask1, mask2, gt, raw_img = load_images(mask1_path, mask2_path, gt_path)
    
    # Calculate IoU score for each image
    ar_iou = []
    cutie_iou = []
    
    for idx in gt.keys():
        mask1_iou = calculate_iou(mask1[idx], gt[idx])
        mask2_iou = calculate_iou(mask2[idx], gt[idx])
        ar_iou.append(mask1_iou)
        cutie_iou.append(mask2_iou)
    
    t_statistic, p_value = ttest_rel(ar_iou, cutie_iou)
    print(f"Paired t-test results:\nT-statistic: {t_statistic}\nP-value: {p_value}")
    # Interpretation
    alpha = 0.05  # significance level
    if p_value < alpha:
        print("The difference in IoU scores is statistically significant.")
    else:
        print("The difference in IoU scores is not statistically significant.")
              
    print("AR IoU score:", np.mean(ar_iou))
    print("Cutie IoU score:", np.mean(cutie_iou))
    draw_iou(mask1, mask2, gt, raw_img)
    # Plotting the distribution of IoU scores
    plt.figure(figsize=(8, 6))
    plt.hist(ar_iou, bins=30, alpha=0.5, label='AR')
    plt.hist(cutie_iou, bins=30, alpha=0.5, label='Cutie')
    plt.xlabel('IoU Score',fontsize=22)
    plt.ylabel('Frequency', fontsize=22)
    plt.xticks(fontsize=18)
    plt.yticks(fontsize=18)
    plt.title('Distribution of IoU Scores')
    plt.legend(fontsize=22)
    plt.show()
    # # Precision, Recall and F-measure
    # num_gt = len(gt) # number of ground truth files
    # num_rs_dir = 2 # number of method folders
    # gt2rs = np.zeros((num_gt,num_rs_dir)) # indicate if the mask of methods is correctly computed
    # PRE1 = []
    # REC1 = []
    # PRE2 = []
    # REC2 = []
    
    # for j in range(num_rs_dir):
    #     for idx in gt.keys():    
    #         if j == 0:
    #             pre, rec = calculate_prc_rc(mask1[idx], gt[idx])
    #             PRE1.append(pre)
    #             REC1.append(rec)
    #         else:
    #             pre, rec = calculate_prc_rc(mask2[idx], gt[idx])
    #             PRE2.append(pre)
    #             REC2.append(rec)
        
    # plot_save_pr_curves(REC1, PRE1,REC2, PRE2)
    