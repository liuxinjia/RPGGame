using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode { NoiseMap, ColourMap, Mesh }
	public DrawMode drawMode;

	public const int mapChunkSize = 241;
	[Range (0, 6)]
	public int editorPreviewLOD;
	public float noiseScale;

	public int octaves;
	[Range (0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;
	public int meshHeightMultipiler;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>> ();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>> ();

	public void DrawMapEditor () {
		MapData mapData = GenerateMapData ();

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colorMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (
				MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultipiler, meshHeightCurve, editorPreviewLOD),
				TextureGenerator.TextureFromColourMap (mapData.colorMap, mapChunkSize, mapChunkSize)
			);
		}
	}

	public void RequestMapData (Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (callback);
		};

		new Thread (threadStart).Start ();
	}
	public void MapDataThread (Action<MapData> callback) {
		//get the different map data in different thread
		MapData mapData = GenerateMapData ();
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}

	}

	public void RequestMeshData (MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	public void MeshDataThread (MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (
			mapData.heightMap, meshHeightMultipiler, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	private void Update () {
		if (mapDataThreadInfoQueue.Count > 0) {
			//overlook about that, it will update all the time
			// if you don't care the order
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	public MapData GenerateMapData () {
		float[, ] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight <= regions[i].height) {
						colorMap[y * mapChunkSize + x] = regions[i].colour;
						break;
					}
				}
			}
		}

		return new MapData (noiseMap, colorMap);
	}

	void OnValidate () {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
	}

	public struct MapThreadInfo<T> {
		//for thread safet
		//if we had created them , we should not change it
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}

}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

public struct MapData {
	public readonly float[, ] heightMap;
	public readonly Color[] colorMap;

	public MapData (float[, ] heightMap, Color[] colorMap) {
		this.heightMap = heightMap;
		this.colorMap = colorMap;
	}
}