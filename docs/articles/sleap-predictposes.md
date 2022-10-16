---
uid: sleap-predictposes
title: PredictPoses
---

[`PredictPoses`](xref:Bonsai.Sleap.PredictPoses) implements the [*top-down-model* network]. The usual input of this operation will be a sequence of full frames where multiple instances are expected to be found. This operator will output a [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection) with N number of instances found in the image. Indexing a [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection) will return a [`Pose`](xref:Bonsai.Sleap.Pose) where we can access the [`Centroid`](xref:Bonsai.Sleap.Centroid) of each detected instance along with the [`Pose`](xref:Bonsai.Sleap.Pose) containing information on all trained body parts.

To access the data of a specific body part we use the [`GetBodyPart`](xref:Bonsai.Sleap.GetBodyPart). We set [`Name`](xref:Bonsai.Sleap.GetBodyPart.Name) to match the part name defined in the `training_config.json` file. From that moment, the operator will always emit the selected [`BodyPart`](xref:Bonsai.Sleap.BodyPart) object and its inferred position ([`BodyPart.Position`](xref:Bonsai.Sleap.BodyPart.Position)).

:::workflow
![TopDownNoIDModel](~/workflows/TopDownNoIDModel.bonsai)
:::
