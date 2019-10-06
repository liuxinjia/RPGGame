using UnityEngine;

[CreateAssetMenu (fileName = "TextureData", menuName = "ProcLand/TextureData", order = 0)]
public class TextureData : UpdatableData {

    public Color[] baseColors;
    [Range (0, 1)]
    public float[] baseStartHeights;

    float saveMinHeight;
    float saveMaxHeight;

    public void ApplyToMaterial (Material material) {
        material.SetFloat ("baseColorCount", baseColors.Length);
        material.SetColorArray ("baseColors", baseColors);
        material.SetFloatArray ("baseStartHeights", baseStartHeights);

        UpdateMeshHeights (material, saveMinHeight, saveMaxHeight);
    }

    public void UpdateMeshHeights (Material material, float minHeight, float maxHeight) {
        saveMaxHeight = maxHeight;
        saveMinHeight = minHeight;

        material.SetFloat ("minHeight", minHeight);
        material.SetFloat ("maxHeight", maxHeight);
    }

}