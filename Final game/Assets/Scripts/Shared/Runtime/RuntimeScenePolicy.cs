using UnityEngine;

[System.Serializable]
public class RuntimeScenePolicy
{
    public bool useExistingSceneObjects = true;
    public bool autoCreateMissingObjects = true;
    public bool overrideCameraTransform = false;
    public bool rebuildUiOnPlay = false;
    public bool preserveExistingImageOverrides = false;
    public string runtimeGeneratedRootName = "RuntimeGenerated";

    public Transform GetOrCreateRuntimeRoot(string ownerName)
    {
        string rootName = string.IsNullOrEmpty(runtimeGeneratedRootName) ? "RuntimeGenerated" : runtimeGeneratedRootName;
        GameObject root = GameObject.Find(rootName);
        if (root == null)
        {
            if (!autoCreateMissingObjects)
            {
                Debug.LogWarning(ownerName + ": Runtime generated root '" + rootName + "' is missing and auto creation is disabled.");
                return null;
            }

            root = new GameObject(rootName);
        }

        return root.transform;
    }

    public static RuntimeScenePolicy Defaults()
    {
        return new RuntimeScenePolicy();
    }
}
