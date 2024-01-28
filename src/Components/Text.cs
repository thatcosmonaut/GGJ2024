using GGJ2024.Content;
using MoonWorks.Graphics.Font;

namespace GGJ2024;

public struct Text
{
	public FontID FontID { get; }
	public int Size { get; }
	public int TextID { get; }
	public HorizontalAlignment HorizontalAlignment { get; }
	public VerticalAlignment VerticalAlignment { get; }

	public Text(
		FontID packID,
		int size,
		string text,
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
	) {
		FontID = packID;
		Size = size;
		TextID = Data.TextStorage.GetID(text);
		HorizontalAlignment = horizontalAlignment;
		VerticalAlignment = verticalAlignment;
	}

	public Text(
		FontID packID,
		int size,
		int textID,
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
	) {
		FontID = packID;
		Size = size;
		TextID = textID;
		HorizontalAlignment = horizontalAlignment;
		VerticalAlignment = verticalAlignment;
	}
}
