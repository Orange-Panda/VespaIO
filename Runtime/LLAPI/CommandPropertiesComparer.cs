using System.Collections.Generic;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Sort commands first by manual priority then by ordinal string (using the key value not name).
	/// </summary>
	public class CommandPropertiesComparer : IComparer<ICommandProperties>
	{
		public int Compare(ICommandProperties x, ICommandProperties y)
		{
			if (x == null)
			{
				return 0;
			}
			else if (y == null)
			{
				return -1;
			}
			else if (x.ManualPriority > y.ManualPriority)
			{
				return -1;
			}
			else if (x.ManualPriority < y.ManualPriority)
			{
				return 1;
			}
			else
			{
				return string.CompareOrdinal(x.Key, y.Key);
			}
		}
	}
}