---
uid: sleap-intro
---

The simplest Bonsai workflow for running the complete SLEAP `top-down-id-model` is:

:::workflow
![PredictPoseIdentities](~/workflows/PredictPoseIdentities.bonsai)
:::

If everything works out, you should see some indication in the Bonsai command line window that the GPU was successfully detected and enabled. The first frame will cold start the inference graph and this may take a bit of time, but after that, your poses should start streaming through!

![Bonsai_Pipeline_expanded](~/images/demo.gif)
