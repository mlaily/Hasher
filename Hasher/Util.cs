using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hasher
{
	public static class Util
	{
		//we can't shift more than 63 bits with a long, so we would need a big int to support ZB and YB...
		static readonly string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB" };
		/// <summary>
		/// Supports any non negative value up to long.MaxValue (~8EB)
		/// </summary>
		public static string ToHumanReadableString(long length)
		{
			if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be negative!");
			long shiftResult = 0;
			long previousShiftResult = 0;
			for (int i = 1; i < units.Length; i++)
			{
				shiftResult = 1L << (i * 10);
				if (length < shiftResult || shiftResult == 64) // length < current unit value ? (or overflow, meaning we are in the EB range)
				{
					double value = ((double)length / previousShiftResult); //then we take the previous unit
					string format = value < 1000 ? "{0:#0.0}{1}" : "{0:#0}{1}";
					return string.Format(format, value, units[i - 1]);
				}
				previousShiftResult = shiftResult;
			}
			throw new ArgumentOutOfRangeException("length", "length is too big!"); //This is impossible
		}

		/// <summary>
		/// If the percent value is an invalid percentage (less than 0 or more than 100)
		/// the returned string is a moving star...
		/// </summary>
		public static string GetProgressBar(int percent, ref ProgressIndicatorState progressIndicator)
		{
			const int baseLength = 50;
			const int fullLength = 52;
			StringBuilder dots = new StringBuilder();
			if (percent < 0 || percent > 100)
			{
				if (progressIndicator == null)
				{
					progressIndicator = new ProgressIndicatorState();
				}
				dots.Append("[");
				for (int i = 0; i < progressIndicator.MovingIndicatorPosition; i++)
				{
					dots.Append(" ");
				}
				dots.Append('*');
				//take account of the star and the bracket
				for (int i = progressIndicator.MovingIndicatorPosition + 2; i < baseLength + 2; i++)
				{
					dots.Append(" ");
				}
				dots.Append("]");
				progressIndicator.MovingIndicatorPosition += progressIndicator.DirectionIsRight ? 1 : -1;
				if (progressIndicator.MovingIndicatorPosition >= baseLength || progressIndicator.MovingIndicatorPosition <= 0)
				{
					progressIndicator.DirectionIsRight = !progressIndicator.DirectionIsRight;
				}
				return dots.ToString();
			}
			else
			{
				int percentByTwo = (int)Math.Max(0, Math.Min(Math.Floor(percent / 2f), baseLength));
				dots.Append("[");
				for (int i = 1; i <= percentByTwo; i++)
				{
					dots.Append(".");
				}
				for (int i = percentByTwo; i < baseLength; i++)
				{
					dots.Append(" ");
				}
				dots.Append("]");
				string percentage = string.Format("{0}%", percent);
				int halves = (fullLength / 2) - percentage.Length / 2;
				int alignRight = percentage.Length % 2;
				return dots.ToString(0, halves) + percentage + dots.ToString(fullLength - halves + alignRight, halves - alignRight);
			}
		}

		public class ProgressIndicatorState
		{
			public int MovingIndicatorPosition { get; set; }
			public bool DirectionIsRight { get; set; }
			public ProgressIndicatorState()
			{
				MovingIndicatorPosition = 0;
				DirectionIsRight = true;
			}
		}
	}
}
