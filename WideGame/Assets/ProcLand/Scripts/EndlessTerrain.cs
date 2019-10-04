using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    const float viewerMoveThresholdForChunkUpdate = 25.0f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDst;
    static float sqrMaxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

    void Start () {
        mapGenerator = FindObjectOfType<MapGenerator> ();

        chunkSize = MapGenerator.mapChunkSize - 1;
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunksVisibleInViewDst = Mathf.RoundToInt (maxViewDst / chunkSize);

        sqrMaxViewDst = maxViewDst * maxViewDst;
        for (int i = 0; i < detailLevels.Length; i++)
            detailLevels[i].SqrVisibleDstThreshold = detailLevels[i].visibleDstThreshold * detailLevels[i].visibleDstThreshold;

        UpdateVisibleChunks ();
    }

    void Update () {
        viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
        if ((viewerPosition - viewerPositionOld).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks ();
        }
    }

    void UpdateVisibleChunks () {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible (false);
        }
        terrainChunksVisibleLastUpdate.Clear ();

        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk ();
                } else {
                    terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lODMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk (Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds (position, Vector2.one * size);
            Vector3 positionV3 = new Vector3 (position.x, 0, position.y);

            meshObject = new GameObject ("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer> ();
            meshFilter = meshObject.AddComponent<MeshFilter> ();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible (false);

            lODMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lODMeshes[i] = new LODMesh (detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData (OnMapDataReceived);
        }

        void OnMapDataReceived (MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture texture = TextureGenerator.TextureFromColourMap (mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk ();
        }

        public void UpdateTerrainChunk () {
            if (mapDataReceived) {
        
                float viewerDstFromNearestEdge = bounds.SqrDistance (viewerPosition);
                bool visible = viewerDstFromNearestEdge <= sqrMaxViewDst;
                if (visible) {
                    int lodIndex = 0;

                    //becasue it's visible means it will less than the max detailLevelsLength
                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDstFromNearestEdge > detailLevels[i].SqrVisibleDstThreshold) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }
                    if (lodIndex != previousLODIndex) {
                        LODMesh lODMesh = lODMeshes[lodIndex];
                        if (lODMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lODMesh.mesh;
                        } else if (!lODMesh.hasRequestedMesh) {
                            lODMesh.RequestMesh (mapData);
                        }
                    }
                    terrainChunksVisibleLastUpdate.Add (this);
                }
                SetVisible (visible);
            }
        }

        public void SetVisible (bool visible) {
            meshObject.SetActive (visible);
        }

        public bool IsVisible () {
            return meshObject.activeSelf;
        }

    }

    public class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh (int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived (MeshData meshData) {
            mesh = meshData.CreateMesh ();
            hasMesh = true;
            updateCallback ();
        }

        public void RequestMesh (MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData (mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDstThreshold;
        [HideInInspector]
        public float SqrVisibleDstThreshold;

    }

}