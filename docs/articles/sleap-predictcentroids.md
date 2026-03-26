---
uid: sleap-predictcentroids
title: PredictCentroids
---

[`PredictCentroids`](xref:Bonsai.Sleap.PredictCentroids) runs the [*centroid* model](https://nn.sleap.ai/latest/api/export/wrappers/centroid/). This model is most commonly used to find a set of candidate centroids from a full-resolution image. For each frame, it will return a [`CentroidCollection`](xref:Bonsai.Sleap.CentroidCollection) which can be further indexed to access the individual instances.

As an example application, the output of this operator is also fully compatible with the [`CropCenter`](xref:Bonsai.Vision.CropCenter) transform node, which can be used to easily generate smaller crops centered on the detected centroid instance (i.e. [`Centroid`](xref:Bonsai.Sleap.Centroid))

:::workflow
![PredictCentroids](~/workflows/CentroidModel.bonsai)
:::
