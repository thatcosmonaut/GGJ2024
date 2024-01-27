using System;
using System.Numerics;
using System.Drawing;

namespace ContentBuilderUI
{
	public static class UIColors
	{
		public static Vector4 RGB255(int r, int g, int b)
		{
			return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
		}

		public static Vector4 SamuraiGunn2Red = RGB255(254, 10, 10);
		public static Vector4 InkBlack = RGB255(0, 0, 0);
		public static Vector4 BgGrey = RGB255(110, 117, 125);
		public static Vector4 PromptGrey = RGB255(65, 81, 97);
		public static Vector4 MoneyYellow = RGB255(203, 141, 0);
		public static Vector4 OnlineBlue = RGB255(7, 47, 204);
		public static Vector4 PaperWhite = RGB255(237, 231, 211);
		public static Vector4 Transparent = new Vector4(0, 0, 0, 0);

		public static Vector4 RedBG = RGB255(24, 18, 18);
		public static Vector4 RedText = RGB255(231, 228, 228);

		public static Vector4 Text = RedText;
		public static Vector4 Disabled = BgGrey;
		public static Vector4 Background = RedBG;
		public static Vector4 Border = RedText;
		public static Vector4 Positive = RGB255(125, 225, 26);
		public static Vector4 Progress = MoneyYellow;
		public static Vector4 Negative = RGB255(230, 21, 21);
	}
}
