// SPDX-License-Identifier: MIT

using GaussianSplatting.Runtime;
using UnityEditor.EditorTools;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    abstract class GaussianTool : EditorTool
    {
        protected GaussianSplatRenderer GetRenderer()
        {
            var gs = target as GaussianSplatRenderer;
            if (!gs || !gs.HasValidAsset || !gs.HasValidRenderSetup)
                return null;
            return gs;
        }

        protected bool CanBeEdited()
        {
            var gs = GetRenderer();
            if (!gs)
                return false;
            return gs.asset.chunkData == null; // need to be lossless / non-chunked for editing
        }

        protected bool HasSelection()
        {
            var gs = GetRenderer();
            if (!gs)
                return false;
            return gs.editSelectedSplats > 0;
        }

        protected Vector3 GetSelectionCenterLocal()
        {
            var gs = GetRenderer();
            if (!gs || gs.editSelectedSplats == 0)
                return Vector3.zero;
            return gs.editSelectedBounds.center;
        }
        protected Rect FromToRect(Vector2 from, Vector2 to)
        {
            if (from.x > to.x)
                (from.x, to.x) = (to.x, from.x);
            if (from.y > to.y)
                (from.y, to.y) = (to.y, from.y);
            return new Rect(from.x, from.y, to.x - from.x, to.y - from.y);
        }
    }
}
