using System.Collections.Generic;
using Kingmaker.Editor.EtudesViewer;
using UnityEngine;

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudeDrawerData
    {
        public Rect EtudeRect;
        public Rect EtudeButtonRect;
        public bool ShowChildren;
        public Vector2 LeftEnterPoint;
        public Vector2 LinkedStartPoint;
        public Vector2 RightExitPoint;
        public Dictionary<string, EtudeIdReferences> ChainStarts = new();
        public bool NeedToPaint;
        public int Depth;

        public readonly EtudeBackRefData StartData = new ();
        public readonly EtudeBackRefData CompleteData = new ();
        public readonly EtudeBackRefData CheckData = new ();
        public readonly EtudeBackRefData CutsceneData = new ();
        public readonly EtudeBackRefData UnstartData = new ();
        public readonly EtudeBackRefData SceneData = new ();
        public readonly EtudeBackRefData OtherData = new ();

        public Rect ConflictingGroupsLabelRect;
        public List<Rect> ConflictingGroupsRects = new List<Rect>();
    }
}