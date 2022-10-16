How to use
==========

`Bonsai.SLEAP` currently implements real-time inference on four distinct SLEAP networks through their corresponding Bonsai `Predict` operators.

![SleapOperators](~/images/SleapSchematic.svg)

In order to use the `Predict` operators, you will need to provide the `ModelFileName` to the exported .pb file folder containing your pre-trained SLEAP model, along with the corresponding `PoseConfigFileName` to the `training_config.json` file.

The simplest Bonsai workflow for running the complete model will thus be:

:::workflow
![PredictPoseIdentities](~/workflows/PredictPoseIdentities.bonsai)
:::

If everything works out, you should see some indication in the Bonsai command line window that the GPU was successfully detected and enabled. The first frame will cold start the inference graph and this may take a bit of time, but after that, your poses should start streaming through!

![Bonsai_Pipeline_expanded](~/images/demo.gif)

Working examples for each of these operators can be found in the extended description for each operator, which we cover below.

## PredictCentroids
[!include[PredictCentroids](~/articles/sleap-predictcentroids.md)]

## PredictPoses
[!include[PredictPoses](~/articles/sleap-predictposes.md)]

## PredictPoseIdentities
[!include[PredictPoseIdentities](~/articles/sleap-predictposeidentities.md)]

## PredictSinglePose
[!include[PredictSinglePose](~/articles/sleap-predictsinglepose.md)]