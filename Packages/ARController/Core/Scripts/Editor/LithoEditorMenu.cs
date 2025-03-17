/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace LITHO.UnityEditor
{

    /// <summary>
    /// Provides menu options with links to documentation
    /// </summary>
    public static class LithoEditorMenu
    {
        private const string UPGRADE_SDK_LINK = "https://developer.litho.cc/page/download-sdk/4";

        private const string USING_LITHO_GUIDE_LINK
            = "https://documentation.litho.cc/UsingLitho.html";

        private const string DOCUMENTATION_LINK
            = "https://documentation.litho.cc/versions/v0.6.1/";

        [MenuItem("LITHO/Upgrade SDK", false, 1100)]
        private static void OpenUpgradeSDK()
        {
            Application.OpenURL(UPGRADE_SDK_LINK);
        }

        [MenuItem("LITHO/Help/Using Litho Guide", false, 1200)]
        private static void OpenUsingLithoGuide()
        {
            Application.OpenURL(USING_LITHO_GUIDE_LINK);
        }

        [MenuItem("LITHO/Help/Documentation", false, 1201)]
        private static void OpenDocumentation()
        {
            Application.OpenURL(DOCUMENTATION_LINK);
        }
    }

}
#endif
