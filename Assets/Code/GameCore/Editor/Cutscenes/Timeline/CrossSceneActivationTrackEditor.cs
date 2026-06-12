using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.Timeline
{
    [CustomTimelineEditor(typeof(CrossSceneActivationTrack))]
    public class CrossSceneActivationTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            var style = EditorStyles.FromUSS("Icon-Activation");
            if (style != null)
            {
                options.icon = style.normal.background;
                options.trackColor = new Color(1, 0.5f, 0);
            }
            return options;
        }
    }
}