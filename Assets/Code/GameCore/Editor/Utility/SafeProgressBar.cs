using System;
using UnityEditor;

#nullable enable

namespace Kingmaker.Editor.Utility
{
	public class SafeProgressBar : IDisposable
	{
		private string m_Title;
		private float m_Total;
		private int m_Step;
		private int m_Progress;

		public SafeProgressBar(string title, int total, int step)
		{
			m_Title = title;
			InitLimits(total, step);
		}

		public SafeProgressBar(string title)
		{
			m_Title = title;
			InitLimits(1, 1);
		}

		public void InitLimits(int total, int step, string? title = null)
		{
			m_Title = title ?? m_Title;
			m_Total = total;
			m_Step = step;
			m_Progress = -1;
		}

		public void DisplayInfo(string info)
		{
			EditorUtility.DisplayProgressBar(m_Title, info, 0);
		}

		public void DisplayProgress(string? info)
		{
			m_Progress++;
			if (m_Progress % m_Step == 0)
			{
				EditorUtility.DisplayProgressBar(m_Title, info, m_Progress / m_Total);
			}
		}

		public void Dispose()
		{
			EditorUtility.ClearProgressBar();
		}
	}
}