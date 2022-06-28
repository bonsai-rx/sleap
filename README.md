# Bonsai - SLEAP
![logo](Resources/sleap-Bonsai-icon.svg)

Bonsai.SLEAP is a [Bonsai](https://bonsai-rx.org/) interface for [SLEAP](https://sleap.ai/) allowing multi-animal, real-time, pose and identitiy estimation using pretrained network models stored in a [Protocol buffer (.pb) format](https://developers.google.com/protocol-buffers/).

Bonsai.SLEAP loads these .pb files using [TensorFlowSharp](https://github.com/migueldeicaza/TensorFlowSharp), a set of .NET bindings for TensorFlow allowing native inference using either the CPU or GPU. By using the .pb file and the corresponding configuration file (`training_config.json`), the `PredictFullModelPose` operator from Bonsai.SLEAP will push the live image data through the inference network and output a set of identified poses from which you can extract an object id and specific object part position. `Bonsai` can then leverage this data to drive online effectors of simply save it to an output file.

The Bonsai.SLEAP came about after a fruitful discussion during the [Quantitative Approaches to Behaviour](http://cajal-training.org/on-site/qab2022).

## How to install

Bonsai.SLEAP can be downloaded through the Bonsai package manager. In order to get visualizer support, you should download both the `Bonsai.SLEAP` and `Bonsai.SLEAP.Design` packages. However, in order to use it for either CPU or GPU inference, you need to pair it with a compiled native TensorFlow binary. You can find precompiled binaries for Windows 64-bit at https://www.tensorflow.org/install/lang_c.

To use GPU TensorFlow (highly recommended for live inference), you also need to install the `CUDA Toolkit` and the `cuDNN libraries`. The current SLEAP package was developed and tested with [CUDA v11.3](https://developer.nvidia.com/cuda-11.3.0-download-archive) and [cuDNN 8.2](https://developer.nvidia.com/cudnn). Additionally, make sure you have a CUDA [compatible GPU](https://docs.nvidia.com/deploy/cuda-compatibility/index.html#support-hardware) with the latest NVIDIA drivers.

After downloading the native TensorFlow binary and cuDNN, you can follow these steps to get the required native files into the `Extensions` folder of your local Bonsai install:

1. The easiest way to find your Bonsai install folder is to right-click on the Bonsai shortcut > Properties. The path to the folder will be shown in the "Start in" textbox;
2. Copy `tensorflow.dll` file from either the CPU or GPU [tensorflow release](https://www.tensorflow.org/install/lang_c#download_and_extract) to the `Extensions` folder;
3. If you are using TensorFlow GPU, make sure to add the `cuda/bin` folder of your cuDNN download to the `PATH` environment variable, or copy all DLL files to the `Extensions` folder.

## How to use

The core operator of Bonsai.SLEAP is the `PredictFullModelPose` node. You can place it after any image source, like so:

![Bonsai_Pipeline](Resources/bonsai-pipeline.svg)

You will also need to point the `ModelFileName` to the exported .pb file folder containing your pretrained SLEAP model, and the `PoseConfigFileName` to the `training_config.json` file describing the class identity and parts' labels.

If everything works out, you should see some indications in the Bonsai command line window about whether the GPU was successfully detected and enabled. The first frame will cold start the inference graph which may take a bit of time, but after that your poses should start streaming through!


## SLEAP installation

For all questions regarding installation of SLEAP, please check the official [docs](https://sleap.ai/).