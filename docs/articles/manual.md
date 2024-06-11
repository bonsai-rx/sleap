How to use
==========

`Bonsai.Sleap` currently implements real-time inference on four distinct SLEAP networks through their corresponding Bonsai `Predict` operators.

```mermaid
flowchart TD

id1("`**IplImage**`") --> id7(Multiple Instances)

id1 --> id8(Single Instance)

id7 -- centroid --> id3("`**PredictCentroids** 

Returns multiple: 
*Centroid*`")

id7 -- top-down-model --> id4("`**PredictPoses**

Returns multiple:
*Centroid*, *Pose*`")


id7 -- top-down-id-model --> id5("`**PredictPoseIdentities**

Returns multiple:
*Centroid*, *Pose*, *Identity*`")

id8 -- single_instance --> id2("`**PredictSinglePose**

Returns single:
*Pose*`")
```

In order to use the `Predict` operators, you will need to provide the `ModelFileName` to the exported .pb file folder containing your pre-trained SLEAP model, along with the corresponding `PoseConfigFileName` to the `training_config.json` file.

[!include[Introduction](~/articles/sleap-intro.md)]

Working examples for each of these operators can be found in the extended descriptions, which we cover below.

## PredictCentroids
[!include[PredictCentroids](~/articles/sleap-predictcentroids.md)]

## PredictPoses
[!include[PredictPoses](~/articles/sleap-predictposes.md)]

## PredictPoseIdentities
[!include[PredictPoseIdentities](~/articles/sleap-predictposeidentities.md)]

## PredictSinglePose
[!include[PredictSinglePose](~/articles/sleap-predictsinglepose.md)]