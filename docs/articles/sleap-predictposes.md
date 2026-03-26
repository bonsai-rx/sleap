---
uid: sleap-predictposes
title: PredictPoses
---

[`PredictPoses`](xref:Bonsai.Sleap.PredictPoses) runs the [*topdown* model](https://nn.sleap.ai/latest/api/export/wrappers/topdown/#sleap_nn.export.wrappers.topdown). This model is used to find multiple instances in a full frame. This operator will output a [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection) object containing the collection of instances found in the image. Indexing a [`PoseCollection`](xref:Bonsai.Sleap.PoseCollection) will return a [`Pose`](xref:Bonsai.Sleap.Pose) where we can access the [`Centroid`](xref:Bonsai.Sleap.Centroid) for each detected instance along with the [`Pose`](xref:Bonsai.Sleap.Pose) containing information on all trained body parts.

The [`GetBodyPart`](xref:Bonsai.Sleap.GetBodyPart) operator can be used to access the data for a specific body part. By setting the [`Name`](xref:Bonsai.Sleap.GetBodyPart.Name) property to match the part name defined in the `export_metadata.json` file, the operator will filter the collection and send notifications for the selected [`BodyPart`](xref:Bonsai.Sleap.BodyPart) object and its inferred position ([`BodyPart.Position`](xref:Bonsai.Sleap.BodyPart.Position)).

:::workflow
![TopDownModel](~/workflows/TopDownModel.bonsai)
:::
