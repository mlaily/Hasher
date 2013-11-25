﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hasher
{
	public static class Util
	{
		static readonly string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
		public static string HumanReadableLength(long length)
		{
			length = Math.Abs(length);
			for (int i = 1; i < units.Length; i++)
			{
				if (length < 1L << (i * 10)) // length < current unit value ?
				{
					double value = ((double)length / (1L << ((i - 1) * 10))); //then we take the previous unit
					string format = value < 1000 ? "{0:#0.0}" : "{0:#0}";
					return string.Format("{0:#0.0}" + units[i - 1], value);
				}
			}
			throw new ArgumentOutOfRangeException("length", "length is too big!");
		}

		private static int invalidProgressIndicator = 0;
		private static bool directionRight = true;
		/// <summary>
		/// If the percent value is an invalid percentage (less than 0 or more than 100)
		/// the returned string is a moving star...
		/// </summary>
		public static string GetProgressBar(int percent)
		{
			const int baseLength = 50;
			const int fullLength = 52;
			StringBuilder dots = new StringBuilder();
			if (percent < 0 || percent > 100)
			{
				dots.Append("[");
				for (int i = 0; i < invalidProgressIndicator; i++)
				{
					dots.Append(" ");
				}
				dots.Append('*');
				//take account of the star and the bracket
				for (int i = invalidProgressIndicator + 2; i < baseLength + 2; i++)
				{
					dots.Append(" ");
				}
				dots.Append("]");
				invalidProgressIndicator += directionRight ? 1 : -1;
				if (invalidProgressIndicator >= baseLength || invalidProgressIndicator <= 0)
				{
					directionRight = !directionRight;
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
	}
}
