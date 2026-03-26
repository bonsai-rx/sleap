---
uid: sleap-predictposeidentities
title: PredictPoseIdentities
---

[`PredictPoseIdentities`](xref:Bonsai.Sleap.PredictPoseIdentities) runs the [*multi_class_topdown_combined* model](https://nn.sleap.ai/latest/api/export/wrappers/topdown_multiclass/#sleap_nn.export.wrappers.topdown_multiclass). This model combines the centroid detection model with a centered instance multiclass model. In addition to extracting pose information for each detected instance in the image, this model also returns the inferred identity of the object.

In addition to the properties of the [`Pose`](xref:Bonsai.Sleap.Pose) object, the extended [`PoseIdentity`](xref:Bonsai.Sleap.PoseIdentity) class adds [`Identity`](xref:Bonsai.Sleap.PoseIdentity.Identity) property that indicates the highest confidence identity. This will match one of the class names found in `export_metadata.json`. The [`IdentityScores`](xref:Bonsai.Sleap.PoseIdentity.IdentityScores) property indicates the confidence values for all class labels.

The operator [`GetMaximumConfidencePoseIdentity`](xref:Bonsai.Sleap.GetMaximumConfidencePoseIdentity) can be used to extract the [`PoseIdentity`](xref:Bonsai.Sleap.PoseIdentity) with the highest confidence from the input [`PoseIdentityCollection`](xref:Bonsai.Sleap.PoseIdentityCollection). By specifying a value in the optional [`Identity`](xref:Bonsai.Sleap.GetMaximumConfidencePoseIdentity.Identity) property, the operator will return the instance will the highest confidence for that particular class.

:::workflow
![MultiClassTopDownModel](~/workflows/MultiClassTopDownModel.bonsai)
:::
