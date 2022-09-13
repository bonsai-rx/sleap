using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.Sleap
{
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Filters a collection of labeled pose objects based on a given class name")]
    public class GetLabeledPose : Transform<LabeledPoseCollection, LabeledPoseCollection>
    {
        [Description("The class label used to filter the labeled pose collection.")]
        public string Label { get; set; }

        public override IObservable<LabeledPoseCollection> Process(IObservable<LabeledPoseCollection> source)
        {
            return source.Select(poses =>
            {
                var label = Label;
                return !string.IsNullOrEmpty(label)
                    ? new LabeledPoseCollection(poses.Where(x => x.Label == label).ToList())
                    : poses;
            });
        }
    }
}
