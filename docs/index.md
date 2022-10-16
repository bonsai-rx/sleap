![logo](~/images/sleap-Bonsai-icon.svg)

Bonsai.SLEAP is a [Bonsai](https://bonsai-rx.org/) interface for [SLEAP](https://sleap.ai/) allowing multi-animal, real-time, pose and identity estimation using pretrained network models stored in a [Protocol buffer (.pb) format](https://developers.google.com/protocol-buffers/).

Bonsai.SLEAP loads these .pb files using [TensorFlowSharp](https://github.com/migueldeicaza/TensorFlowSharp), a set of .NET bindings for TensorFlow allowing native inference using either the CPU or GPU. By using the .pb file and the corresponding configuration file (`training_config.json`), the `PredictFullModelPose` operator from Bonsai.SLEAP will push the live image data through the inference network and output a set of identified poses from which you can extract an object id and specific object part position. `Bonsai` can then leverage this data to drive online effectors or simply save it to an output file.

The Bonsai.SLEAP package came about following a fruitful discussion with the SLEAP team during the [Quantitative Approaches to Behaviour](http://cajal-training.org/on-site/qab2022).
