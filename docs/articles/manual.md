How to use
==========

`Bonsai.SLEAP` currently implements the following SLEAP networks through the correspondent Bonsai operator:

 - `centroid`:
   - Input : full frame with potentially multiple objects
   - Output : collection of multiple detected centroids in the input image
   - Operator : `PredictCentroid`
 - `top-down-model`:
   - Input : full frame with potentially multiple objects
   - Output : collection of detected poses (centroid + body parts) from multiple objects in the image
   - Operator : `PredictPoses`
 - `top-down-id-model`:
   - Input : full frame with potentially multiple objects
   - Output : collection of detected poses (centroid + body parts) plus labeled identities from multiple objects in the image
   - Operator : `PredictLabeledPoses`
 - `single_instance`:
   - Input : croped instance with a single object in the input image
   - Output : returns a single pose (body parts)
   - Operator : `PredictSinglePose`

The general Bonsai workflow will thus be:

![Bonsai_Pipeline](~/images/sleap_operator.svg)

Additional information can be extracted by selecting the relevant structure fields.

![Bonsai_Pipeline_expanded](~/images/demo.gif)

In order to use the `Predict` operators, you will need to provide the `ModelFileName` to the exported .pb file folder containing your pretrained SLEAP model, along with the corresponding `PoseConfigFileName` to the `training_config.json` file.

If everything works out, you should see some indications in the Bonsai command line window about whether the GPU was successfully detected and enabled. The first frame will cold start the inference graph which may take a bit of time, but after that your poses should start streaming through!