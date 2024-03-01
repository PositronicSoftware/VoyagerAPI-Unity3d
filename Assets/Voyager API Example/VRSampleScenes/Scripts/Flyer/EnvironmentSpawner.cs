using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRStandardAssets.Common;
using VRStandardAssets.Utils;
using UnityEngine.Playables;
using VRStandardAssets.Flyer;

// This script handles the spawning and some of the
// interactions of Rings and Asteroids with the flyer.
[ExecuteInEditMode]
public class EnvironmentSpawner : MonoBehaviour
{
	public BezierSpline spline;
	public PlayableDirector playableDirector;
	public bool spawnOnStart = false;

	[SerializeField] private float m_AsteroidSpawnFrequency = 1f;       // The time between asteroids spawning in seconds.
	[SerializeField] private float m_RingSpawnFrequency = 0.5f;         // The time between rings spawning in seconds.
	// Unused
	//[SerializeField] private int m_InitialAsteroidCount = 100;          // The number of asteroids present at the start.
	[SerializeField] private float m_AsteroidSpawnZoneRadius = 120f;    // The radius of the sphere in which the asteroids spawn.
	// Unused
	//[SerializeField] private float m_RingSpawnZoneRadius = 50f;         // The radius of the sphere in which the rings spawn.
	[SerializeField] private float m_SpawnZoneDistance = 500f;          // The distance from the camera of the spawn spheres.
	[SerializeField] private GameObject m_AsteroidObject;               // The the asteroids.
	[SerializeField] private GameObject m_RingObject;                   // The the rings.
	[SerializeField] private ObjectPool m_AsteroidExplosionObjectPool;  // The object pool that stores the expolosions made when asteroids are hit.
	[SerializeField] private ObjectPool m_AsteroidObjectPool;           // The object pool that stores the asteroids.
	[SerializeField] private ObjectPool m_RingObjectPool;               // The object pool that stores the rings.

	private float asteroidTime = 0f;
	private float ringTime = 0f;

	[SerializeField] private List<GameObject> asteroids = new List<GameObject>();
	[SerializeField] private List<GameObject> rings = new List<GameObject>();

	public void Start()
	{
		// Spawn all the starting asteroids.
		/*for (int i = 0; i < m_InitialAsteroidCount; i++)
		{
			SpawnAsteroid();
		}*/

		if (spawnOnStart) {
			SpawnRings();

			SpawnAsteroids();
		}
	}

	public void SpawnRings() {
		while (ringTime < (float)playableDirector.duration) {
			SpawnRing();
		}
	}

	public void SpawnAsteroids() {
		while (asteroidTime < (float)playableDirector.duration) {
			SpawnAsteroid();
		}
	}

	public void RemoveAsteroids() {
		foreach(GameObject asteroid in asteroids) {
			if (asteroid != null) {
				DestroyImmediate(asteroid);
			}
		}

		asteroids = new List<GameObject>();

		asteroidTime = 0f;
	}

	public void RemoveRings() {
		foreach (GameObject ring in rings) {
			if (ring != null) {
				DestroyImmediate(ring);
			}
		}

		rings = new List<GameObject>();
		ringTime = 0f;
	}

	private void SpawnAsteroid ()
	{
		// Get an asteroid from the object pool.
		GameObject asteroidGameObject = GameObject.Instantiate(m_AsteroidObject);

		Vector3 position = spline.GetPoint(Mathf.Clamp((asteroidTime / (float)playableDirector.duration), 0f, (float)playableDirector.duration));

		// Generate a position at a distance forward from the camera within a random sphere and put the asteroid at that position.
		Vector3 asteroidPosition = position * m_SpawnZoneDistance + Random.insideUnitSphere * m_AsteroidSpawnZoneRadius;
		asteroidGameObject.transform.position = asteroidPosition;

		// Get the asteroid component and add it to the collection.
		Asteroid asteroid = asteroidGameObject.GetComponent<Asteroid>();

		// Subscribe to the asteroids events.
		asteroid.OnAsteroidRemovalDistance += HandleAsteroidRemoval;
		asteroid.OnAsteroidHit += HandleAsteroidHit;

		asteroidTime += m_AsteroidSpawnFrequency;

		asteroidGameObject.transform.parent = m_AsteroidObjectPool.transform;

		asteroids.Add(asteroidGameObject);
	}


	private void SpawnRing()
	{
		// Get a ring from the object pool.
		GameObject ringGameObject = GameObject.Instantiate(m_RingObject);

		ringGameObject.transform.position = spline.GetPoint(Mathf.Clamp((ringTime / (float)playableDirector.duration), 0f, (float)playableDirector.duration));
		ringGameObject.transform.rotation = Quaternion.LookRotation(spline.GetDirection(Mathf.Clamp((ringTime / (float)playableDirector.duration), 0f, (float)playableDirector.duration)));
		ringTime += m_RingSpawnFrequency;

		ringGameObject.transform.parent = m_RingObjectPool.transform;

		rings.Add(ringGameObject);
	}


	private void HandleAsteroidRemoval(Asteroid asteroid)
	{
		// Only one of HandleAsteroidRemoval and HandleAsteroidHit should be called so unsubscribe both.
		asteroid.OnAsteroidRemovalDistance -= HandleAsteroidRemoval;
		asteroid.OnAsteroidHit -= HandleAsteroidHit;

		// Remove the asteroid from the collection.
		// m_Asteroids.Remove(asteroid);

		// Return the asteroid to its object pool.
		// m_AsteroidObjectPool.ReturnGameObjectToPool (asteroid.gameObject);
	}


	private void HandleAsteroidHit(Asteroid asteroid)
	{
		// Remove the asteroid when it's hit.
		HandleAsteroidRemoval (asteroid);

		// Get an explosion from the object pool and put it at the asteroids position.
		GameObject explosion = m_AsteroidExplosionObjectPool.GetGameObjectFromPool ();
		explosion.transform.position = asteroid.transform.position;

		// Get the asteroid explosion component and restart it.
		AsteroidExplosion asteroidExplosion = explosion.GetComponent<AsteroidExplosion>();
		asteroidExplosion.Restart();

		// Subscribe to the asteroid explosion's event.
		asteroidExplosion.OnExplosionEnded += HandleExplosionEnded;
	}


	private void HandleExplosionEnded(AsteroidExplosion explosion)
	{
		// Now the explosion has finished unsubscribe from the event.
		explosion.OnExplosionEnded -= HandleExplosionEnded;

		// Return the explosion to its object pool.
		m_AsteroidExplosionObjectPool.ReturnGameObjectToPool (explosion.gameObject);
	}


	private void HandleRingRemove(Ring ring)
	{
		// Now the ring has been removed, unsubscribe from the event.
		ring.OnRingRemove -= HandleRingRemove;

		// Remove the ring from it's collection.
		// m_Rings.Remove(ring);

		// Return the ring to its object pool.
		// m_RingObjectPool.ReturnGameObjectToPool(ring.gameObject);
	}
}