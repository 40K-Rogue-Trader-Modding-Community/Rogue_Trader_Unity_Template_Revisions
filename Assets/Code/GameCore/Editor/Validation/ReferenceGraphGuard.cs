using System;
using Kingmaker.Editor.Validation;

#nullable enable

namespace Code.GameCore.Editor.Validation
{
	public class ReferenceGraphGuard : IDisposable
	{
		private readonly bool m_WasSuppressed;

		public ReferenceGraphGuard(bool isReferenceTrackingSuppressed)
		{
			m_WasSuppressed = ReferenceGraph.IsReferenceTrackingSuppressed;
			ReferenceGraph.IsReferenceTrackingSuppressed = isReferenceTrackingSuppressed;
		}
		public void Dispose()
		{
			ReferenceGraph.IsReferenceTrackingSuppressed = m_WasSuppressed;
		}
	}
}