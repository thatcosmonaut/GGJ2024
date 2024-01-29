using System.Collections.Generic;

namespace RollAndCash.Data
{
	// We can't store strings in ECS because they are managed types!
	public static class TextStorage
	{
		static Dictionary<string, int> StringToID = new Dictionary<string, int>();
		static string[] IDToString = new string[256];
		static Stack<int> OpenIDs = new Stack<int>();
		static int NextID = 0;

		// TODO: is there some way we can reliably clear strings to free memory?

		public static string GetString(int id)
		{
			return IDToString[id];
		}

		public static int GetID(string text)
		{
			if (!StringToID.ContainsKey(text))
			{
				RegisterString(text);
			}

			return StringToID[text];
		}

		private static void RegisterString(string text)
		{
			if (OpenIDs.Count == 0)
			{
				if (NextID >= IDToString.Length)
				{
					System.Array.Resize(ref IDToString, IDToString.Length * 2);
				}
				StringToID[text] = NextID;
				IDToString[NextID] = text;
				NextID += 1;
			}
			else
			{
				StringToID[text] = OpenIDs.Pop();
			}
		}
	}
}
