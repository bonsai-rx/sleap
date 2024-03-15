## Bonsai - SLEAP installation

Bonsai.SLEAP can be downloaded through the Bonsai package manager. In order to get visualizer support, you should download both the `Bonsai.SLEAP` and `Bonsai.SLEAP.Design` packages. However, in order to use it for either CPU or GPU inference, you need to pair it with a compiled native TensorFlow binary. You can find precompiled binaries for Windows 64-bit at https://www.tensorflow.org/install/lang_c.

To use GPU TensorFlow (highly recommended for live inference), you also need to install the `CUDA Toolkit` and the `cuDNN libraries`. The current SLEAP package was developed and tested with [CUDA v11.3](https://developer.nvidia.com/cuda-11.3.0-download-archive) and [cuDNN 8.2](https://developer.nvidia.com/cudnn). Additionally, make sure you have a CUDA [compatible GPU](https://docs.nvidia.com/deploy/cuda-compatibility/index.html#support-hardware) with the latest NVIDIA drivers.

After downloading the native TensorFlow binary and cuDNN, you can follow these steps to get the required native files into the `Extensions` folder of your local Bonsai install:

1. The easiest way to find your Bonsai install folder is to right-click on the Bonsai shortcut > Properties. The path to the folder will be shown in the "Start in" textbox;
2. Copy `tensorflow.dll` file from either the CPU or GPU [tensorflow release](https://www.tensorflow.org/install/lang_c#download_and_extract) to the `Extensions` folder;
3. If you are using TensorFlow GPU, make sure to add the `cuda/bin` folder of your cuDNN download to the `PATH` environment variable, or copy all DLL files to the `Extensions` folder.

## SLEAP installation

For all questions regarding installation of SLEAP, please check the official [docs](https://sleap.ai/).

> [!WARNING]
> At this time (3/15/24) `Bonsai.SLEAP` is only compatible with pb model files from SLEAP v1.2.9 and below. More information and status updates on a potential bug-fix can be found on this [link](https://github.com/talmolab/sleap/issues/1542)

