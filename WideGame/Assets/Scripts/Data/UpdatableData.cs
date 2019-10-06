using UnityEngine;

public class UpdatableData : ScriptableObject {

    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

#if UNITY_EDITOR
    //also changed in compile time
    protected virtual void OnValidate () {
        if (autoUpdate) {
            UnityEditor.EditorApplication.update += NotifyOfUpdateValues;
        }
    }

    public void NotifyOfUpdateValues () {
        UnityEditor.EditorApplication.update -= NotifyOfUpdateValues;
        if (OnValuesUpdated != null) {
            OnValuesUpdated ();
        }
    }

#endif

}