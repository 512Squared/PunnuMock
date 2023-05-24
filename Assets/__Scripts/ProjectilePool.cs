using System;
using System.Collections.Generic;
using UnityEngine;

namespace __Scripts
{
    public class ProjectilePool : SingletonMonobehaviour<ProjectilePool>
    {
        public GameObject projectilePrefab;
        [SerializeField] private Transform projectilePool;
        private Queue<GameObject> pool = new Queue<GameObject>();

        public GameObject Get()
        {
            if (pool.Count == 0) AddObjects(1);
            return pool.Dequeue();
        }

        public void Return(GameObject objectToReturn)
        {
            if (objectToReturn == null) return; // coroutine for Return() means delay creates nulls after SetProjectileType() is called)
            objectToReturn.SetActive(false);
            pool.Enqueue(objectToReturn);
        }

        private void AddObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject newObject = Instantiate(projectilePrefab, projectilePool);
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }
        /// <summary>
        /// This is purely for setup - allows Devs to easily experiment with different prefabs during game play
        /// </summary>
        /// <param name="newProjectilePrefab"></param>
        public void SetProjectileType(GameObject newProjectilePrefab)
        {
            projectilePrefab = newProjectilePrefab;
            foreach(GameObject projectile in pool) Destroy(projectile); 
            pool.Clear();
            projectilePool.Clear();
            AddObjects(10); 
        }
    }

}
