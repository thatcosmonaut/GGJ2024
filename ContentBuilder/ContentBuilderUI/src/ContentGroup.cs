using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ContentBuilderUI;

public class ContentGroup
{
	private ConcurrentQueue<TrackedDirectory> Directories = new ConcurrentQueue<TrackedDirectory>();

	public IEnumerator<TrackedDirectory> GetEnumerator() => Directories.GetEnumerator();

	public string Name;

	public BuildStatus BuildStatus
	{
		get
		{
			var allBuildStatus = BuildStatus.Complete;
			foreach (var directory in Directories)
			{
				allBuildStatus = ComposeBuildStatus(allBuildStatus, directory.BuildStatus);
			}
			return allBuildStatus;
		}
	}

	public ContentGroup(string name)
	{
		Name = name;
	}

	public void Add(TrackedDirectory directory)
	{
		Directories.Enqueue(directory);
	}

	public void Clear()
	{
		Directories.Clear();
	}

	private BuildStatus ComposeBuildStatus(BuildStatus current, BuildStatus composing)
	{
		if (current == BuildStatus.OutOfDate || composing == BuildStatus.OutOfDate)
		{
			return BuildStatus.OutOfDate;
		}
		else if (current == BuildStatus.InProgress)
		{
			return BuildStatus.InProgress;
		}
		else if (current == BuildStatus.Comparing)
		{
			return BuildStatus.Comparing;
		}
		else
		{
			return composing;
		}
	}
}
