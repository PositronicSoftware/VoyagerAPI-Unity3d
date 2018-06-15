using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnvironmentSpawner))]
[CanEditMultipleObjects]
public class EnvironmentSpawnerEditor : Editor {

	public override void OnInspectorGUI () {
		EnvironmentSpawner spawner = (EnvironmentSpawner)target;

		DrawDefaultInspector();

		EditorGUILayout.Separator();

		if (GUILayout.Button("Spawn Asteroids")) {
			spawner.SpawnAsteroids();
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Spawn Rings")) {
			spawner.SpawnRings();
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Remove Asteroids")) {
			spawner.RemoveAsteroids();
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Remove Rings")) {
			spawner.RemoveRings();
		}
	}
}