using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    public const float maxViewDst = 450;
    public Transform viewer;
    public Material mapMaterial;

    static MapGenerator mapGenerator;
    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
    List<TerrainChunk> terrainChunkVisibleLastUpdated = new List<TerrainChunk> ();
    private void Start () {
        mapGenerator = FindObjectOfType<MapGenerator> ();

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt (maxViewDst / chunkSize);
    }

    private void Update () {
        viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
        UpdateVisibleChunks ();
    }

    public void UpdateVisibleChunks () {
        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);
        for (int i = 0; i < terrainChunkVisibleLastUpdated.Count; i++) {
            terrainChunkVisibleLastUpdated[i].SetVisible (false);
        }
        terrainChunkVisibleLastUpdated.Clear ();

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey (viewChunkCoord)) {
                    terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk ();
                    if (terrainChunkDictionary[viewChunkCoord].IsVisible ()) {
                        terrainChunkVisibleLastUpdated.Add (terrainChunkDictionary[viewChunkCoord]);
                    }
                } else {
                    terrainChunkDictionary.Add (viewChunkCoord, new TerrainChunk (viewChunkCoord, chunkSize, transform, mapMaterial));
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

        public TerrainChunk (Vector2 coord, int size, Transform parent, Material material) {
            position = coord * size;
            bounds = new Bounds (position, Vector3.one * size);
            Vector3 positionV3 = new Vector3 (position.x, 0, position.y);

            meshObject = new GameObject ("Terrain chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer> ();
            meshFilter = meshObject.AddComponent<MeshFilter> ();

            meshRenderer.material = material;
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible (false);

            mapGenerator.RequestMapData (OnMapDataReceived);
        }

        //for lod 
        void OnMapDataReceived (MapData mapData) {
            // mapGenerator.RequestMeshData (mapData,  OnMeshDataReceived);
        }

        void OnMeshDataReceived (MeshData meshData) {
            meshFilter.mesh = meshData.CreateMesh ();
        }

        public void UpdateTerrainChunk () {
            float viewerDstFromNearestEdge = bounds.SqrDistance (viewerPosition);
            bool visible = viewerDstFromNearestEdge <= maxViewDst * maxViewDst;
            SetVisible (visible);
        }

        public void SetVisible (bool visible) {
            meshObject.SetActive (visible);
        }

        public bool IsVisible () {
            return meshObject.activeSelf;
        }
    }

    class LODMesh{
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;

        public LODMesh(int lod){
            this.lod= lod;
        }

        void OnMeshDataReceived(MeshData meshData){
            mesh = meshData.CreateMesh();
            hasMesh = true;
        }

        public void RequestMesh(MapData mapData){
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,lod, OnMeshDataReceived);
        }
    }
}