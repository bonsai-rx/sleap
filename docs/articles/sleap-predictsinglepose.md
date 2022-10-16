---
uid: sleap-predictsinglepose
title: PredictSinglePose
---

Almost all SLEAP operators afford the detection of multiple instances for each incoming image. However, in certain cases we might be interested in only identifying a single object in the incoming image. This strategy offers multiple advantages, specifically in terms of performance. In Bonsai.SLEAP, this functionality is implemented using the [`PredictSinglePose`](xref:Bonsai.Sleap.PredictSinglePose) operator that implements a [*single_instance* network](https://sleap.ai/api/sleap.nn.config.model.html?highlight=centered%20instanc#sleap.nn.config.model.CenteredInstanceConfmapsHeadConfig). Since the centroid detection step is not performed by the network, the operator expects an already centered instance on which it will run the pose estimation. Moreover, the network will always return a single output per incoming frame, even if no valid instances are detected.

The following example workflow highlights how combining [basic computer-vision algorithm for image segmentation](https://circuitdigest.com/tutorial/image-segmentation-using-opencv#:~:text=Image%20segmentation%20is%20a%20process,the%20parts%20of%20an%20image.) for centroid detection, with the network-based pose estimation, results in >2-fold increases in performance relative to the previously introduced [`PredictPoses`](xref:Bonsai.Sleap.PredictPoses) operator. In this example, the first part of the workflow segments and detects the centroid positions (output of [`BinaryRegionAnalysis`](xref:Bonsai.Vision.BinaryRegionAnalysis)) of all available objects in the incoming frame, which are then combined with the original image to generate centered crops ([`CropCenter`](xref:Bonsai.Vision.CropCenter)). These images are then pushed through the network that will perform the pose estimation step of the process.

:::workflow
![SingleInstanceModel](~/workflows/SingleInstanceModel.bonsai)
:::

Finally, it is worth noting that [`PredictSinglePose`](xref:Bonsai.Sleap.PredictSinglePose) affords two input overloads. When receiving a single image it will output a corresponding [`Pose`](xref:Bonsai.Sleap.Pose). Since the operator skips the centroid-detection stage, it won't embed a [`Centroid`](xref:Bonsai.Sleap.Centroid) field in[`Pose`](xref:Bonsai.Sleap.Pose). Alternatively, a *batch* mode can be accessed by providing an array of images to the operator, instead returning [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection). This latter overload results in dramatic performance gains relative to single images.
