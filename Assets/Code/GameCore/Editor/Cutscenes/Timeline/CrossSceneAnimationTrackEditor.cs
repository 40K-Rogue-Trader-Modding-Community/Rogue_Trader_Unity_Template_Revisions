using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.Timeline
{
    [CustomTimelineEditor(typeof(CrossSceneAnimationTrack))]
    public class CrossSceneAnimationTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.icon = EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
            options.trackColor = new Color(1, 0.5f, 0);
            return options;
        }
    }
}
