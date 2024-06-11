---
uid: sleap-predictposeidentities
title: PredictPoseIdentities
---

[`PredictPoseIdentities`](xref:Bonsai.Sleap.PredictPoseIdentities) evaluates the full SLEAP model network. In addition to extracting pose information for each detected instance in the image, it also returns the inferred identity of the object, i.e. it performs inference on the [*top-down-id-model* network](https://sleap.ai/develop/api/sleap.nn.config.model.html#sleap.nn.config.model.MultiClassTopDownConfig).

In addition to the properties of the [`Pose`](xref:Bonsai.Sleap.Pose) object, the extended [`PoseIdentity`](xref:Bonsai.Sleap.PoseIdentity) class adds [`Identity`](xref:Bonsai.Sleap.PoseIdentity.Identity) property that indicates the highest confidence identity. This will match one of the class labels found in `training_config.json`. The [`IdentityScores`](xref:Bonsai.Sleap.PoseIdentity.IdentityScores) property indicates the confidence values for all class labels.

Since we are very often only interested in the instance with the highest identification confidence we have added the operator [`GetMaximumConfidencePoseIdentity`](xref:Bonsai.Sleap.GetMaximumConfidencePoseIdentity) which returns the [`PoseIdentity`](xref:Bonsai.Sleap.PoseIdentity) with the highest confidence from the input [`PoseIdentityCollection`](xref:Bonsai.Sleap.PoseIdentityCollection). Moreover, by specifying a value in the optional [`Identity`](xref:Bonsai.Sleap.GetMaximumConfidencePoseIdentity.Identity) property, the operator will return the instance will the highest confidence for that particular class.

:::workflow
![FullTopDownModel](~/workflows/FullTopDownModel.bonsai)
:::
