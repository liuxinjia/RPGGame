# Reactor2@5733c4e

[Permalink](reactor2-5733c4e.md)

[Browse files](https://github.com/liuxinjia/RPGGame/tree/5733c4e1032303bb7dc089036686049082062bc4)

 Reactor2

* Loading branch information...

 [![@liuxinjia](https://avatars3.githubusercontent.com/u/24849085?s=60&v=4)](https://github.com/liuxinjia)

 1 parent [796ba1f](https://github.com/liuxinjia/RPGGame/commit/796ba1f98b120c63c68d3de3a3885905aee44eb5) commit 5733c4e1032303bb7dc089036686049082062bc4

|  | @@ -2,11 +2,11 @@ |  |
| :--- | :--- | :--- |
|  |  |  using System.Collections; |
|  |  |  using UnityEditor; |
|  |  |  |
|  |  |  \[CustomEditor \(typeof \(MapGenerator\)\)\] |
|  |  |  public class MapGeneratorEditor : Editor { |
|  |  |  \[CustomEditor \(typeof \(MapPreview\)\)\] |
|  |  |  public class MapPreviewEditor : Editor { |
|  |  |  |
|  |  |  public override void OnInspectorGUI\(\) { |
|  |  |  MapGenerator mapGen = \(MapGenerator\)target; |
|  |  |  MapPreview mapGen = \(MapPreview\)target; |
|  |  |  |
|  |  |  if \(DrawDefaultInspector \(\)\) { |
|  |  |  if \(mapGen.autoUpdate\) { |
|  |  |  |

 File renamed without changes.

 Large diffs are not rendered by default. ![](https://github.githubassets.com/images/spinners/octocat-spinner-128.gif)

##  0 comments on commit `5733c4e`

 Please [sign in](https://github.com/login?return_to=https%3A%2F%2Fgithub.com%2Fliuxinjia%2FRPGGame%2Fcommit%2F5733c4e1032303bb7dc089036686049082062bc4) to comment.

