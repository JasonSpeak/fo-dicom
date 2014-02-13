﻿using System;

using Dicom.Imaging.Render;

namespace Dicom.Imaging {
	/// <summary>
	/// Grayscale rendering options class
	/// </summary>
	public class GrayscaleRenderOptions {
		/// <summary>
		/// GrayscaleRenderOptions constructor using BitDepth values
		/// </summary>
		/// <param name="bits">Bit depth information</param>
		public GrayscaleRenderOptions(BitDepth bits) {
			BitDepth = bits;
			RescaleSlope = 1.0;
			RescaleIntercept = 0.0;
			VOILUTFunction = "LINEAR";
			WindowWidth = bits.MaximumValue - bits.MinimumValue;
			WindowCenter = (bits.MaximumValue + bits.MinimumValue) / 2.0;
			Monochrome1 = false;
			Invert = false;
		}

		/// <summary>
		/// BitDepth used to initialize the GrayscaleRenderOptions
		/// </summary>
		public BitDepth BitDepth {
			get;
			set;
		}
		/// <summary>
		/// Pixel data rescale slope
		/// </summary>
		public double RescaleSlope {
			get;
			set;
		}

		/// <summary>
		/// Pixel data resclae interception
		/// </summary>
		public double RescaleIntercept {
			get;
			set;
		}

		/// <summary>
		/// VOI LUT function (LINEAR or SEGMOID)
		/// </summary>
		public string VOILUTFunction {
			get;
			set;
		}

		/// <summary>
		/// Window width
		/// </summary>
		public double WindowWidth {
			get;
			set;
		}

		/// <summary>
		/// Window center
		/// </summary>
		public double WindowCenter {
			get;
			set;
		}

		/// <summary>
		/// Specify if this grey scale image is Monochrome1 or Monorchrome2, true means Monochrome1
		/// </summary>
		public bool Monochrome1 {
			get;
			set;
		}

		/// <summary>
		/// Set to true to render the output in inverted grey
		/// </summary>
		public bool Invert {
			get;
			set;
		}

		/// <summary>
		/// Create <see cref="GrayscaleRenderOptions"/>  from <paramref name="dataset"/> and populate the options properties with values:
		/// Bit Depth
		/// Rescale Slope
		/// Rescale Intercept
		/// Window Width
		/// Window Center
		/// </summary>
		/// <param name="dataset">Dataset to extract <see cref="GrayscaleRenderOptions"/> from</param>
		/// <returns>New grayscale render options instance</returns>
		public static GrayscaleRenderOptions FromDataset(DicomDataset dataset) {
			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			if (dataset.Contains(DicomTag.WindowWidth) && dataset.Get<double>(DicomTag.WindowWidth) != 0.0) {
				//If dataset contains WindowWidth and WindowCenter valid attributes used initially for the grayscale options
				return FromWindowLevel(dataset);
			} else if (dataset.Contains(DicomTag.SmallestImagePixelValue) && dataset.Contains(DicomTag.LargestImagePixelValue)) {
				//If dataset contains valid SmallesImagePixelValue and LargesImagePixelValue attributes, use range to calculate
				//WindowWidth and WindowCenter
				return FromImagePixelValueTags(dataset);
			} else {
				//If reached here, minimum and maximum pixel values calculated from pixels data to calculate
				//WindowWidth and WindowCenter
				return FromMinMax(dataset);
			}

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}

		public static GrayscaleRenderOptions FromWindowLevel(DicomDataset dataset) {
			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			options.WindowWidth = dataset.Get<double>(DicomTag.WindowWidth);
			options.WindowCenter = dataset.Get<double>(DicomTag.WindowCenter);

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}

		public static GrayscaleRenderOptions FromImagePixelValueTags(DicomDataset dataset) {
			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			int smallValue = dataset.Get<int>(DicomTag.SmallestImagePixelValue, 0);
			int largeValue = dataset.Get<int>(DicomTag.LargestImagePixelValue, 0);

			if (smallValue != 0 || largeValue != 0) {
				options.WindowWidth = Math.Abs(largeValue - smallValue);
				options.WindowCenter = (largeValue + smallValue) / 2.0;
			}

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}

		public static GrayscaleRenderOptions FromMinMax(DicomDataset dataset) {
			if (dataset.InternalTransferSyntax.IsEncapsulated)
				throw new ArgumentException("Min/Max pixel values can only be calculated for uncompressed images.", "dataset");

			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			int padding = dataset.Get<int>(DicomTag.PixelPaddingValue, 0, Int32.MinValue);

			var pixelData = DicomPixelData.Create(dataset);
			var pixels = PixelDataFactory.Create(pixelData, 0);
			var range = pixels.GetMinMax(padding);

			if (range.Minimum < bits.MinimumValue || range.Minimum == Double.MaxValue)
				range.Minimum = bits.MinimumValue;
			if (range.Maximum > bits.MaximumValue || range.Maximum == Double.MinValue)
				range.Maximum = bits.MaximumValue;

			options.WindowWidth = Math.Abs(range.Maximum - range.Minimum);
			options.WindowCenter = range.Minimum + (options.WindowWidth / 2.0);

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}

		public static GrayscaleRenderOptions FromBitRange(DicomDataset dataset) {
			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			options.WindowWidth = bits.MaximumValue - bits.MinimumValue;
			options.WindowCenter = bits.MinimumValue + (options.WindowWidth / 2.0);

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}

		public static GrayscaleRenderOptions FromHistogram(DicomDataset dataset, int percent = 90) {
			if (dataset.InternalTransferSyntax.IsEncapsulated)
				throw new ArgumentException("Histogram can only be calculated for uncompressed images.", "dataset");

			var bits = BitDepth.FromDataset(dataset);
			var options = new GrayscaleRenderOptions(bits);

			options.RescaleSlope = dataset.Get<double>(DicomTag.RescaleSlope, 1.0);
			options.RescaleIntercept = dataset.Get<double>(DicomTag.RescaleIntercept, 0.0);

			var pixelData = DicomPixelData.Create(dataset);
			var pixels = PixelDataFactory.Create(pixelData, 0);
			var histogram = pixels.GetHistogram(0);

			histogram.ApplyWindow(percent);

			options.WindowWidth = histogram.WindowEnd - histogram.WindowStart;
			options.WindowCenter = histogram.WindowStart + (options.WindowWidth / 2.0);

			options.VOILUTFunction = dataset.Get<string>(DicomTag.VOILUTFunction, "LINEAR");
			options.Monochrome1 = dataset.Get<PhotometricInterpretation>(DicomTag.PhotometricInterpretation) == PhotometricInterpretation.Monochrome1;

			return options;
		}
	}
}
