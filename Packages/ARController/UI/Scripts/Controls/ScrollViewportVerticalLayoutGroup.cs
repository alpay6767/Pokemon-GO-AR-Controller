/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.UI;

namespace LITHO.UI
{

    /// <summary>
    /// Acts as a VerticalLayoutGroup, but constrains the lower bound to be contained by a given
    /// container object
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("LITHO/UI/Scroll Viewport Vertical Layout Group", -9498)]
    public class ScrollViewportVerticalLayoutGroup : VerticalLayoutGroup
    {
        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();

            RectTransform containerRect
                = transform.parent.parent.parent.parent.GetComponent<RectTransform>();

            float top = 0f, bottom = 0f;

            Transform sizer = transform;
            LayoutGroup sizerLayoutGroup;
            while (sizer != null)
            {
                sizerLayoutGroup = sizer?.GetComponent<LayoutGroup>();
                // Account for padding at the current point in the hierarchy
                bottom += sizerLayoutGroup?.padding.bottom ?? 0f;

                if (sizer == containerRect.transform)
                {
                    break;
                }
                // Move up the hierarchy
                sizer = sizer.parent;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            top += corners[1].y * LithoUI.BASELINE_SCREEN_WIDTH / Screen.width;
            containerRect.GetWorldCorners(corners);
            bottom += corners[0].y * LithoUI.BASELINE_SCREEN_WIDTH / Screen.width;
            float maxHeight = Mathf.Min(top - bottom, preferredHeight);

            SetLayoutInputForAxis(maxHeight, maxHeight, 0, 1);
        }
    }
}
