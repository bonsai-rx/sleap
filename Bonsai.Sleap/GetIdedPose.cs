using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.Sleap

{
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Filters a collection of IdedPose objects based on a given id name")]

     public class GetIdedPose : Transform<IdedPoseCollection, IdedPoseCollection>
    {
        [Description("The name of the ided object.")]
        public string Name { get; set; }

        public override IObservable<IdedPoseCollection> Process(IObservable<IdedPoseCollection> source)
        {
            return source.Select(idedCol => {

                if (Name.Length == 0)
                {
                    return idedCol;
                }
                else
                {
                    return new IdedPoseCollection(idedCol.Where(x => x.IdName == Name).ToList());
                }

            });
        }
    }
}
