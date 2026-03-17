using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs multi-instance centroid detection for each
    /// image in the sequence using a SLEAP model.
    /// </summary>
    /// <seealso cref="PredictPoses"/>
    /// <seealso cref="PredictPoseIdentities"/>
    /// <seealso cref="PredictSinglePose"/>
    /// <seealso cref="GetBodyPart"/>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs multi-instance centroid detection for each image in the sequence using a SLEAP model.")]
    public class PredictCentroids : Transform<IplImage, CentroidCollection>
    {
        /// <summary>
        /// Gets or sets a value specifying the path to the exported ONNX
        /// file containing the pretrained SLEAP model.
        /// </summary>
        [FileNameFilter("ONNX Files(*.onnx)|*.onnx")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("Specifies the path to the exported ONNX file containing the pretrained SLEAP model.")]
        public string ModelFileName { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the path to the configuration JSON file
        /// containing training metadata.
        /// </summary>
        [FileNameFilter("Config Files(*.json)|*.json|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("Specifies the path to the configuration JSON file containing training metadata.")]
        public string TrainingConfig { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the confidence threshold used to discard centroid
        /// predictions. If no value is specified, all estimated centroid positions are returned.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the confidence threshold used to discard centroid predictions. If no value is specified, all estimated centroid positions are returned.")]
        public float? CentroidMinConfidence { get; set; }

        /// <summary>
        /// Gets or sets the backend execution provider used to perform inference.
        /// </summary> <summary>
        [Description("The backend execution provider used to perform inference.")]
        public Provider Provider { get; set; } = Provider.Cpu;

        private IObservable<CentroidCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage tmpImage = null;
                Size currentImageSize = default;
                DenseTensor<byte> inputBuffer = null;

                var sessionOptions = new SessionOptions
                {
                    EnableProfiling = true,
                    ProfileOutputPathPrefix = "onnx_profile",
                };

                switch (Provider)
                {
                    case Provider.Cuda:
                        {
                            var cudaOptions = new OrtCUDAProviderOptions();
                            sessionOptions.AppendExecutionProvider_CUDA(cudaOptions);
                            break;
                        }

                    case Provider.TensorRT:
                        {
                            var cudaOptions = new OrtCUDAProviderOptions();
                            sessionOptions.AppendExecutionProvider_CUDA(cudaOptions);
                            sessionOptions.AppendExecutionProvider_Tensorrt();
                            break;
                        }
                }

                var session = new InferenceSession(ModelFileName, sessionOptions);
                var inputName = session.InputMetadata.Keys.First();
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);

                if (config.ModelType != ModelType.Centroid)
                {
                    session?.Dispose();
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.Centroid)} model type but found {config.ModelType}.");
                }

                return source.Select(input =>
                {
                    var imageSize = input[0].Size;
                    var batchSize = input.Length;
                    var centroidThreshold = CentroidMinConfidence ?? 0;

                    if (inputBuffer == null || currentImageSize != imageSize)
                    {
                        ReadOnlySpan<int> inputSize = stackalloc int[] { batchSize, 1, imageSize.Height, imageSize.Width };
                        inputBuffer = TensorHelper.CreateInputBuffer(inputSize);
                        currentImageSize = imageSize;
                    }

                    var frames = input.Select(frame => TensorHelper.EnsureGrayscale(frame, ref tmpImage)).ToArray();

                    TensorHelper.UpdateInputBuffer(inputBuffer, imageSize, frames);
                    var onnxInputs = new [] { NamedOnnxValue.CreateFromTensor(inputName, inputBuffer) };

                    var centroidCollection = new CentroidCollection(input[0]);

                    using var onnxOutputs = session.Run(onnxInputs);
                    var outputs = onnxOutputs.ToList();

                    var centroidTensor = outputs[0].AsTensor<float>();
                    var confTensor = outputs[1].AsTensor<float>();
                    var validTensor = outputs[2].AsTensor<bool>();

                    var nInstances = confTensor.Dimensions[1];
                    if (nInstances == 0)
                        return centroidCollection;

                    for (int i = 0; i < nInstances; i++)
                    {
                        if (validTensor[0, i] && confTensor[0, i] >= centroidThreshold)
                        {
                            centroidCollection.Add(new Centroid(frames[0])
                            {
                                Name = config.AnchorName,
                                Position = new Point2f(
                                    centroidTensor[0, i, 0],
                                    centroidTensor[0, i, 1]),
                                Confidence = confTensor[0, i]
                            });
                        }
                    }
                    return centroidCollection;
                });
            });
        }

        /// <summary>
        /// Performs multi-instance centroid detection for each image in an observable
        /// sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">The sequence of images from which to extract the centroids.</param>
        /// <returns>
        /// A sequence of <see cref="CentroidCollection"/> objects representing the
        /// centroids extracted from each image in the <paramref name="source"/> sequence.
        /// </returns>
        public override IObservable<CentroidCollection> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame }));
        }
    }
}
