---
uid: sleap-predictsinglepose
title: PredictSinglePose
---

[`PredictSinglePose`](xref:Bonsai.Sleap.PredictSinglePose) runs the [*single_instance* model](https://nn.sleap.ai/latest/api/export/wrappers/single_instance/). Most Bonsai.SLEAP operators support the detection of multiple instances for each incoming frame. However, there are advantages in both performance and flexibility of identifying a single object in the frame when alternative pre-processing methods for identifying cropped regions are available.

> [!NOTE]
> Since the centroid detection step is not performed by the network, the operator expects an already centered instance on which it will run the pose estimation. This operator will always return a single output per incoming frame, even if no valid instances are detected.

The following example workflow highlights how combining [basic computer-vision algorithm for image segmentation](https://bonsai-rx.org/docs/tutorials/acquisition.html#exercise-10-real-time-position-tracking) for centroid detection with SLEAP-NN pose estimation may result in >2-fold increases in performance relative to [`PredictPoses`](xref:Bonsai.Sleap.PredictPoses) operator. In this example, the first part of the workflow segments and detects the centroid positions (output of [`BinaryRegionAnalysis`](xref:Bonsai.Vision.BinaryRegionAnalysis)) of all available objects in the incoming frame, which are then combined with the original image to generate centered crops ([`CropCenter`](xref:Bonsai.Vision.CropCenter)). These images are then pushed through the network that will perform the pose estimation step of the process.

:::workflow
![SingleInstanceModel](~/workflows/SingleInstanceModel.bonsai)
:::

Finally, it is worth noting that [`PredictSinglePose`](xref:Bonsai.Sleap.PredictSinglePose) offers two input overloads. When a sequence of single images is provided, the operator will output a corresponding sequence of [`Pose`](xref:Bonsai.Sleap.Pose) objects. Since the operator skips the centroid-detection stage, it won't embed a [`Centroid`](xref:Bonsai.Sleap.Centroid) field in the returned [`Pose`](xref:Bonsai.Sleap.Pose).

Alternatively, a *batch* mode can be accessed by providing a sequence of batches (arrays) of images to the operator. In this case, the operator returns a sequence of [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection) objects, one for each input frame. This latter overload can result in dramatic gains in throughput relative to processing single images.
