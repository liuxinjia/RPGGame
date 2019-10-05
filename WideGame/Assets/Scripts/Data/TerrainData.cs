using UnityEngine;

[CreateAssetMenu (fileName = "TerrainData", menuName = "ProcLand/TerrainData", order = 0)]
public class TerrainData : UpdatableData {

    public float uniformScale = 2.5f;

    public bool useFlatShading;
    public bool useFalloff;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

}