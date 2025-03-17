/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LITHO.UI
{

    /// <summary>
    /// Auto-generates buttons allowing different scenes to be opened
    /// </summary>
    [AddComponentMenu("LITHO/UI/Scene Select UI", -9396)]
    public class SceneSelectUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Button to spawn to represent each scene")]
        private GameObject _buttonPrefab = null;

        [SerializeField]
        [Tooltip("List of scenes to not create a button for (e.g. non-AR scenes)")]
        private List<string> _excludeScenes = new List<string>();

        private List<GameObject> _sceneSelectButtons = new List<GameObject>();


        void Awake()
        {
            if (_buttonPrefab == null)
            {
                Debug.LogWarning("Scene select button prefab is not set; " +
                                 this + " will not work as intended");
            }
        }

        private void Start()
        {
            // If there is more than one scene in this build
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                if (_buttonPrefab.GetComponentInChildren<Button>() != null)
                {
                    // Loop through all scene indices in this build
                    for (int s = 0; s < SceneManager.sceneCountInBuildSettings; s++)
                    {
                        // Create a button corresponding to the current scene
                        CreateButton(s);
                    }
                    SceneManager.activeSceneChanged += HandleActiveSceneChanged;
                }
                else
                {
                    Debug.LogWarning("Button prefab provided to " + this + "does not have a " +
                                     "Button component; cannot implement scene selection buttons");
                    // Delete this UI
                    Destroy(gameObject);
                }
            }
            else
            {
                // Delete this UI
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Text text;
            Button button;
            for (int b = 0; b < _sceneSelectButtons.Count; b++)
            {
                text = _sceneSelectButtons[b].GetComponentInChildren<Text>();
                button = _sceneSelectButtons[b].GetComponentInChildren<Button>();
                if (text != null && button != null)
                {
                    button.interactable = text.text != newScene.name;
                }
            }
        }


        private void CreateButton(int sceneIndex)
        {
            GameObject buttonObject = Instantiate(_buttonPrefab, transform);
            _sceneSelectButtons.Add(buttonObject);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(sceneIndex));

            Button button = buttonObject.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => GoToScene(sceneName));
            }

            Text buttonText = buttonObject.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = sceneName;
            }
            if (sceneIndex == SceneManager.GetActiveScene().buildIndex)
            {
                button.interactable = false;
            }
        }

        private void GoToScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

}
