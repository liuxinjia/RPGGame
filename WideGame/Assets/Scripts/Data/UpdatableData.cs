using UnityEngine;

[CreateAssetMenu (fileName = "UpdatableData", menuName = "ProcLand/UpdatableData", order = 0)]
public class UpdatableData : ScriptableObject {

    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

    //also changed in compile time
    protected virtual void OnValidate () {
        if (autoUpdate) {
            NotifyOfUpdateValues ();
        }
    }

    public void NotifyOfUpdateValues () {
        if (OnValuesUpdated != null) {
            OnValuesUpdated ();
        }
    }
}