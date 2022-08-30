using Bonsai;
using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using OpenCV.Net;
using Bonsai.Vision;

[Combinator]
[Description("Centers the input image in a specified point and crops with the desired output size. Will fill borders if needed.")]

[WorkflowElementCategory(ElementCategory.Transform)]
public class CropCentered
{
    [Description("The size of the output Crop.")]
    public Size Size { get; set; }

    [Range(0, 255)]
    [Precision(0, 1)]
    [TypeConverter("Bonsai.Vision.BgraScalarConverter, Bonsai.Vision")]
    [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
    [Description("The color value to which all pixels in the border of the output image will be set to.")]
    public Scalar FillColor { get; set; }

    public IObservable<IplImage> Process(IObservable<Tuple<IplImage, Point2f>> source)
    {
        return source.Select(value =>
        {
            return CenterCropImage(value.Item1, value.Item2, Size, FillColor);

        });
    }

    public IObservable<IplImage> Process(IObservable<Tuple<IplImage,  ConnectedComponent>> source)
    {
        return source.Select(value =>
        {
            return CenterCropImage(value.Item1, value.Item2.Centroid, Size, FillColor);

        });

    }

    public IObservable<IplImage[]> Process(IObservable<Tuple<IplImage, ConnectedComponentCollection>> source)
    {
        return source.Select(input =>
        {
            var img = input.Item1;
            List<IplImage> outArr = new List<IplImage>();
            foreach (var _col in input.Item2) 
            {
                outArr.Add(CenterCropImage(img, _col.Centroid, Size, FillColor));
            }
            return outArr.ToArray();

        });

    }

    public IObservable<IplImage[]> Process(IObservable<Tuple<IplImage, Point2f[]>> source)
    {
        return source.Select(input =>
        {
            var img = input.Item1;
            List<IplImage> outArr = new List<IplImage>();
            for (int i = 0; i < input.Item2.Length; i++)
            {
                outArr.Add(CenterCropImage(img, input.Item2[i], Size, FillColor));
            }
            return outArr.ToArray();

        });

    }


    /// <summary>
    /// Crops the input image around a Center point. Fills the image if needed
    /// </summary>
    /// <param name="im_in"></param>
    /// <param name="Center"></param>
    /// <param name="Size"></param>
    /// <param name="FillColor"></param>
    /// <returns></returns>
    private IplImage CenterCropImage(IplImage im_in, Point2f Center, Size Size, Scalar FillColor)
    {
        //Check invalid arguments
        if (Center.X < 0 | Center.Y < 0 | Center.X > im_in.Width | Center.Y > im_in.Height)
        {
            throw new ArgumentException("Invalid value", "Center must be within IplImage size");
        }

        // Ideal output crop
        var rect = new Rect(
            (int)(Center.X - Size.Width / 2f),
            (int)(Center.Y - Size.Height / 2f),
            Size.Width,
            Size.Height);

        // Check if borders are needed
        if ((rect.X < 0) | ((rect.X + rect.Width) > im_in.Width) | (rect.Y < 0) | ((rect.Y + rect.Height) > im_in.Height))
        {

            var im_out = new IplImage(new Size(im_in.Size.Width + rect.Width * 2, im_in.Size.Height + rect.Height * 2),
                im_in.Depth, im_in.Channels);
            CV.CopyMakeBorder(im_in, im_out,
                new Point(im_out.Size.Width / 2 - im_in.Size.Width / 2, im_out.Size.Height / 2 - im_in.Size.Height / 2),
                IplBorder.Constant, FillColor);

            // Offset the rect to account for the newly added border
            rect.X += rect.Width;
            rect.Y += rect.Height;
            // Crop the image
            return im_out.GetSubRect(rect);
        }
        // If no borders are needed just default to a normal crop around center.
        else
        {
            return im_in.GetSubRect(rect);

        }
    }
}
